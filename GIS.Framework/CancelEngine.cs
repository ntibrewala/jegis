using System;
using GIS.Framework.Helpers;
using GIS.Framework.Interfaces;
using GIS.Framework.Models.EInvoice;
using Newtonsoft.Json;

namespace GIS.Framework
{
    public class CancelEngine
    {
        public static Tuple<string, string> ProcessCancel(IDatabaseHelper dbHelper, string objType, string docEntry)
        {
            try
            {
                LoggerHelper.Log("Entered CancelEngine.ProcessCancel");
                LoggerHelper.Log($"--- STARTING CANCEL PROCESS FOR DOC: {docEntry} ---");

                // Step 1: Check if the document was previously generated and get its IRN
                string irn = string.Empty;
                string query = $"SELECT \"Irn\", \"IsCancel\" FROM \"GIS_EI_ORES\" WHERE \"ObjType\" = '{objType}' AND \"DocEntry\" = {docEntry}";
                var dt = dbHelper.ExecuteQuery(query);
                
                if (dt == null || dt.Rows.Count == 0)
                {
                    throw new Exception("Document has not been generated on the portal. No IRN found to cancel.");
                }

                string isCancel = dt.Rows[0]["IsCancel"]?.ToString() ?? "";
                if (isCancel.ToUpper() == "Y")
                {
                    throw new Exception("This Document is already cancelled. Cannot cancel again.");
                }

                irn = dt.Rows[0]["Irn"]?.ToString() ?? "";
                if (string.IsNullOrEmpty(irn))
                {
                    throw new Exception("IRN is empty in the database. Cannot cancel.");
                }

                // Step 2: Fetch Credentials and Token
                LoggerHelper.Log("Fetching API Credentials...");
                var credentials = CredentialManager.GetCredentialsForDocument(dbHelper, objType, docEntry);
                if (credentials == null) throw new Exception("Could not resolve credentials for this document.");
                
                var tokenResponse = AuthHelper.GetCharteredAuthTokenSync(dbHelper, objType, docEntry);
                if (tokenResponse.Data == null || string.IsNullOrEmpty(tokenResponse.Data.AuthToken)) throw new Exception("Failed to acquire valid AuthToken. Process aborted.");

                // Target URL
                string targetUrl = "https://einvapi.charteredinfo.com/eicore/dec/v1.03/Invoice/Cancel";
                try
                {
                    string urlQuery = "SELECT \"U_Url\" FROM \"@GIS_EI_OAPI\" WHERE \"U_Action\"='CancelEInvoice'";
                    var urlDt = dbHelper.ExecuteQuery(urlQuery);
                    if (urlDt != null && urlDt.Rows.Count > 0 && urlDt.Rows[0]["U_Url"] != DBNull.Value)
                    {
                        string dbUrl = urlDt.Rows[0]["U_Url"].ToString().Trim();
                        if (!string.IsNullOrEmpty(dbUrl)) targetUrl = dbUrl;
                    }
                }
                catch
                {
                    LoggerHelper.Log("Could not fetch CancelEInvoice URL from @GIS_EI_OAPI, using fallback.");
                }

                // Step 3: Create Cancel Payload
                var cancelPayload = new ReqPlCancelIRN
                {
                    Irn = irn,
                    CnlRsn = "1",
                    CnlRem = "Cancelled via SAP"
                };
                string jsonPayload = JsonConvert.SerializeObject(cancelPayload);

                // Step 4: Run the API
                LoggerHelper.Log("Executing Cancel API POST...");
                throw new Exception($"DEBUG: URL={targetUrl} | GSTIN={credentials.GSTIN} | TokenLength={tokenResponse.Data.AuthToken.Length}");
                string apiResponse = ApiRunner.PostCancelInvoice(
                    jsonPayload,
                    tokenResponse.Data.AuthToken,
                    credentials.GSTIN,
                    credentials.UserName,
                    credentials.AspPassword,
                    credentials.AspId,
                    targetUrl
                );

                // Auto-heal on GSP752 / expired AuthToken: clear cache, fetch fresh token, and retry once
                if (AuthHelper.IsTokenExpiredError(apiResponse))
                {
                    LoggerHelper.Log($"Token rejected (GSP752) for GSTIN {credentials.GSTIN}. Evicting cache and auto-retrying with fresh token...");
                    var freshTokenResponse = AuthHelper.GetCharteredAuthTokenSync(dbHelper, objType, docEntry, forceRefresh: true);
                    if (freshTokenResponse?.Data != null && !string.IsNullOrEmpty(freshTokenResponse.Data.AuthToken))
                    {
                        apiResponse = ApiRunner.PostCancelInvoice(
                            jsonPayload,
                            freshTokenResponse.Data.AuthToken,
                            credentials.GSTIN,
                            credentials.UserName,
                            credentials.AspPassword,
                            credentials.AspId,
                            targetUrl
                        );
                    }
                }

                // Step 5: Write the response to SAP (GIS_EI_ORES and OINV)
                // DEFERRED: DB Write-Back will now be handled by UIEventHandler after showing Success Bar

                LoggerHelper.Log($"--- COMPLETED CANCEL PROCESS FOR DOC: {docEntry} ---");
                LoggerHelper.Log("Exited CancelEngine.ProcessCancel");
                return new Tuple<string, string>(jsonPayload, apiResponse);
            }
            catch (Exception ex)
            {
                LoggerHelper.Log($"CRITICAL CANCEL ENGINE FAILURE: {ex.Message}");
                LoggerHelper.Log("Exited CancelEngine.ProcessCancel (with Exception)");
                throw;
            }
        }
    }
}
