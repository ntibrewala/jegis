using GIS.Framework.Interfaces;
using GIS.Framework.Models;
using System;

namespace GIS.Framework
{
    public static class EWayBillCancelEngine
    {
        public static Tuple<string, string> ProcessCancel(IDatabaseHelper dbHelper, string objType, string docEntry)
        {
            try
            {
                Helpers.LoggerHelper.Log($"--- STARTING STANDALONE E-WAY BILL CANCEL PROCESS FOR DOC: {docEntry} (ObjType: {objType}) ---");

                // PRE-VALIDATION: Check if an E-Way Bill exists to cancel
                string tableName = (objType == "13") ? "OINV" : (objType == "14") ? "ORIN" : "OWTR";
                string checkQuery = $"SELECT \"U_ewayBNo\" FROM \"{tableName}\" WHERE \"DocEntry\"={docEntry}";
                var dtCheck = dbHelper.ExecuteQuery(checkQuery);
                if (dtCheck == null || dtCheck.Rows.Count == 0 || string.IsNullOrEmpty(dtCheck.Rows[0]["U_ewayBNo"]?.ToString()))
                {
                    throw new Exception("No E-Way Bill found to cancel for this document!");
                }

                string ewbNo = dtCheck.Rows[0]["U_ewayBNo"].ToString();
                if (ewbNo.Contains("Cancelled"))
                {
                    throw new Exception("The E-Way Bill for this document is already cancelled!");
                }

                // Step 1: Fetch Credentials for this specific document
                Helpers.LoggerHelper.Log("Fetching API Credentials...");
                var credentials = Helpers.CredentialManager.GetCredentialsForDocument(dbHelper, objType, docEntry);

                if (credentials == null) throw new Exception("Could not resolve credentials for this document.");

                // Step 2: Check Token Cache / Fetch New Token
                Helpers.LoggerHelper.Log("Fetching Auth Token...");
                var tokenResponse = Helpers.AuthHelper.GetCharteredAuthTokenSync(dbHelper, objType, docEntry);

                if (tokenResponse.Data == null || string.IsNullOrEmpty(tokenResponse.Data.AuthToken)) throw new Exception("Failed to acquire valid AuthToken. Process aborted.");

                // Step 3: Generate the Standalone Cancel JSON Payload & Extract Target URL
                Helpers.LoggerHelper.Log("Generating E-Way Bill Cancel JSON Payload...");
                var payloadTuple = Helpers.PayloadGenerator.GenerateEWayBillCancelPayload(dbHelper, objType, docEntry);
                string jsonPayload = payloadTuple.Item1;
                string targetUrl = payloadTuple.Item2;

                // Step 4: Execute API POST
                Helpers.LoggerHelper.Log("Executing API POST...");
                string apiResponse = Helpers.ApiRunner.PostInvoice(
                    jsonPayload,
                    tokenResponse.Data.AuthToken,
                    credentials.GSTIN,
                    credentials.UserName,
                    credentials.AspPassword,
                    credentials.AspId,
                    targetUrl
                );

                // Auto-heal on GSP752 / expired AuthToken: clear cache, fetch fresh token, and retry once
                if (Helpers.AuthHelper.IsTokenExpiredError(apiResponse))
                {
                    Helpers.LoggerHelper.Log($"Token rejected (GSP752) for GSTIN {credentials.GSTIN}. Evicting cache and auto-retrying with fresh token...");
                    var freshTokenResponse = Helpers.AuthHelper.GetCharteredAuthTokenSync(dbHelper, objType, docEntry, forceRefresh: true);
                    if (freshTokenResponse?.Data != null && !string.IsNullOrEmpty(freshTokenResponse.Data.AuthToken))
                    {
                        apiResponse = Helpers.ApiRunner.PostInvoice(
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

                // Step 5: Database Write-Back
                // DEFERRED: DB Write-Back will now be handled by UIEventHandler after showing Success Bar

                Helpers.LoggerHelper.Log($"--- COMPLETED STANDALONE E-WAY BILL CANCEL PROCESS FOR DOC: {docEntry} ---");
                return new Tuple<string, string>(jsonPayload, apiResponse);
            }
            catch (Exception ex)
            {
                Helpers.LoggerHelper.Log($"CRITICAL ENGINE FAILURE: {ex.Message}");
                throw;
            }
        }
    }
}
