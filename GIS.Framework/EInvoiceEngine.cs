using GIS.Framework.Interfaces;
using GIS.Framework.Models;
using System;

namespace GIS.Framework
{
    public static class EInvoiceEngine
    {
        public static Tuple<string, string> ProcessEInvoice(IDatabaseHelper dbHelper, string objType, string docEntry)
        {
            try
            {
                Helpers.LoggerHelper.Log($"--- STARTING STANDALONE E-INVOICE PROCESS FOR DOC: {docEntry} ---");

                // PRE-VALIDATION: Check if an active E-Invoice already exists
                string docType = (objType == "13") ? "13" : "14";
                string checkQuery = $"SELECT 1 FROM \"GIS_EI_ORES\" WHERE \"ObjType\"='{docType}' AND \"DocEntry\"={docEntry} AND IFNULL(\"IsCancel\", '') != 'Y'";
                var dtCheck = dbHelper.ExecuteQuery(checkQuery);
                if (dtCheck != null && dtCheck.Rows.Count > 0)
                {
                    throw new Exception("An active E-Invoice (IRN) already exists for this document! Cancel it first before generating a new one.");
                }

                // Step 1: Fetch Credentials for this specific document
                Helpers.LoggerHelper.Log("Fetching API Credentials...");
                var credentials = Helpers.CredentialManager.GetCredentialsForDocument(dbHelper, objType, docEntry);

                if (credentials == null) throw new Exception("Could not resolve credentials for this document.");

                // Step 2: Check Token Cache / Fetch New Token
                Helpers.LoggerHelper.Log("Fetching Auth Token...");
                var tokenResponse = Helpers.AuthHelper.GetCharteredAuthTokenSync(dbHelper, objType, docEntry);

                if (tokenResponse.Data == null || string.IsNullOrEmpty(tokenResponse.Data.AuthToken)) throw new Exception("Failed to acquire valid AuthToken. Process aborted.");

                // Step 3: Generate the Standalone JSON Payload & Extract Target URL
                Helpers.LoggerHelper.Log("Generating Standalone JSON Payload...");
                var payloadTuple = Helpers.PayloadGenerator.GenerateEInvoicePayload(dbHelper, objType, docEntry);
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

                // Step 5: Database Write-Back
                // DEFERRED: DB Write-Back will now be handled by UIEventHandler after showing Success Bar

                Helpers.LoggerHelper.Log($"--- COMPLETED STANDALONE E-INVOICE PROCESS FOR DOC: {docEntry} ---");
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
