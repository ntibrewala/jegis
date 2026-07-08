using GIS.Framework.Interfaces;
using GIS.Framework.Models;
using System;

namespace GIS.Framework
{
    public static class CombinedEngine
    {
        public static string ProcessCombined(IDatabaseHelper dbHelper, string objType, string docEntry)
        {
            try
            {
                Helpers.LoggerHelper.Log("Entered CombinedEngine.ProcessCombined");
                Helpers.LoggerHelper.Log($"--- STARTING COMBINED PROCESS FOR DOC: {docEntry} ---");

                // DIAGNOSTIC: If file logging silently failed, write the error to the database so we can see it
                if (!string.IsNullOrEmpty(Helpers.LoggerHelper.LastError))
                {
                    try
                    {
                        string safeError = Helpers.LoggerHelper.LastError.Replace("'", "''");
                        dbHelper.ExecuteNonQuery($"INSERT INTO \"TEC_EI_LOG\" VALUES('{docEntry}', 'DIAG', 'LOG_ERROR_DIAGNOSTIC', '{safeError}', CURRENT_TIMESTAMP, 'Addon')");
                    }
                    catch { }
                }

                // PRE-VALIDATION: Check E-Invoice & E-Way Bill existence
                string docType = (objType == "13") ? "13" : "14";
                string tableName = (objType == "13") ? "OINV" : "ORIN";

                // Check E-Invoice (eiExists)
                bool eiExists = false;
                string checkEiQuery = $"SELECT 1 FROM \"GIS_EI_ORES\" WHERE \"ObjType\"='{docType}' AND \"DocEntry\"={docEntry} AND IFNULL(\"IsCancel\", '') != 'Y'";
                var dtEi = dbHelper.ExecuteQuery(checkEiQuery);
                if (dtEi != null && dtEi.Rows.Count > 0) eiExists = true;

                // Check E-Way Bill (ewbExists)
                bool ewbExists = false;
                string checkEwbQuery = $"SELECT \"U_ewayBNo\" FROM \"{tableName}\" WHERE \"DocEntry\"={docEntry}";
                var dtEwb = dbHelper.ExecuteQuery(checkEwbQuery);
                if (dtEwb != null && dtEwb.Rows.Count > 0)
                {
                    string ewbNo = dtEwb.Rows[0]["U_ewayBNo"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(ewbNo) && !ewbNo.Contains("Cancelled")) ewbExists = true;
                }

                // TRUTH TABLE
                if (eiExists && ewbExists)
                    throw new Exception("Both E-Invoice and E-Way Bill already exist for this document!");
                if (eiExists && !ewbExists)
                    throw new Exception("An active E-Invoice already exists! Please use Standalone E-Way Bill instead.");
                if (!eiExists && ewbExists)
                    throw new Exception("An active E-Way Bill already exists! Please use Standalone E-Invoice instead.");

                // Step 1: Fetch Credentials for this specific document
                Helpers.LoggerHelper.Log("Fetching API Credentials...");
                var credentials = Helpers.CredentialManager.GetCredentialsForDocument(dbHelper, objType, docEntry);

                if (credentials == null) throw new Exception("Could not resolve credentials for this document.");

                // Step 2: Check Token Cache / Fetch New Token
                Helpers.LoggerHelper.Log("Fetching Auth Token...");
                var tokenResponse = Helpers.AuthHelper.GetCharteredAuthTokenSync(dbHelper, objType, docEntry);

                if (tokenResponse.Data == null || string.IsNullOrEmpty(tokenResponse.Data.AuthToken)) throw new Exception("Failed to acquire valid AuthToken. Process aborted.");

                // Step 3: Generate the Combined JSON Payload & Extract Target URL
                Helpers.LoggerHelper.Log("Generating JSON Payload...");
                var payloadTuple = Helpers.PayloadGenerator.GenerateCombinedPayload(dbHelper, objType, docEntry);
                string jsonPayload = payloadTuple.Item1;
                string targetUrl = payloadTuple.Item2;

                // Step 4: Run the API
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

                // Step 5: Write the response to SAP (OINV/ORIN) and log to TEC_EI_LOG
                Helpers.LoggerHelper.Log("Executing Database Write-Back...");
                Helpers.ResponseHandler.RouteResponse("combined", dbHelper, objType, docEntry, jsonPayload, apiResponse);

                Helpers.LoggerHelper.Log($"--- COMPLETED COMBINED PROCESS FOR DOC: {docEntry} ---");
                Helpers.LoggerHelper.Log("Exited CombinedEngine.ProcessCombined");
                return apiResponse;
            }
            catch (Exception ex)
            {
                Helpers.LoggerHelper.Log($"CRITICAL ENGINE FAILURE: {ex.Message}");
                Helpers.LoggerHelper.Log("Exited CombinedEngine.ProcessCombined (with Exception)");
                throw;
            }
        }
    }
}