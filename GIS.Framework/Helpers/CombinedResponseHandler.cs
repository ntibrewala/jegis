using GIS.Framework.Interfaces;
using System;

namespace GIS.Framework.Helpers
{
    internal class CombinedResponseHandler : IResponseHandler
    {
        public void Handle(IDatabaseHelper dbHelper, string objType, string docEntry, string rawJsonRequest, string rawJsonResponse)
        {
            LoggerHelper.Log("Executing CombinedResponseHandler logic...");
            LoggerHelper.Log($"Request Payload: {rawJsonRequest}");
            LoggerHelper.Log($"API Response: {rawJsonResponse}");

            try
            {
                string safeRequest = rawJsonRequest?.Replace("'", "''") ?? "";
                string safeResponse = rawJsonResponse?.Replace("'", "''") ?? "";
                string sDocTypeLog = (objType == "13") ? "IN" : (objType == "14" ? "CM" : objType);
                string sDocType = (objType == "13") ? "13" : (objType == "14" ? "14" : "Unknown");

                // 1. Insert into TEC_EI_LOG (Legacy Logging)
                string logQuery = $"INSERT INTO \"TEC_EI_LOG\" VALUES('{docEntry}', '{sDocTypeLog}', '{safeRequest}', '{safeResponse}', CURRENT_TIMESTAMP, 'Addon')";
                dbHelper.ExecuteNonQuery(logQuery);
                LoggerHelper.Log("Combined Payload successfully written to TEC_EI_LOG.");

                // 2. Parse the response and update SAP directly
                if (rawJsonResponse != null)
                {
                    try
                    {
                        var jObj = Newtonsoft.Json.Linq.JObject.Parse(rawJsonResponse);
                        string status = jObj["Status"]?.ToString() ?? "";
                        
                        bool isSuccess = status == "1" || status.ToLower() == "true" || rawJsonResponse.Contains("\"status_cd\":\"1\"");
                        bool isDuplicate = false;
                        
                        string irn = "", ackNo = "", ackDt = "", signedInvoice = "", signedQRCode = "", ewbNo = "", ewbValidTill = "", ewbDt = "";

                        // Check for DUPIRN (Duplicate IRN)
                        if (!isSuccess && jObj["InfoDtls"] != null && jObj["InfoDtls"].HasValues)
                        {
                            string infCd = jObj["InfoDtls"][0]["InfCd"]?.ToString() ?? "";
                            if (infCd == "DUPIRN")
                            {
                                isDuplicate = true;
                                isSuccess = true;
                                var desc = jObj["InfoDtls"][0]["Desc"];
                                irn = desc?["Irn"]?.ToString() ?? "";
                                ackNo = desc?["AckNo"]?.ToString() ?? "";
                                ackDt = desc?["AckDt"]?.ToString() ?? "";
                                LoggerHelper.Log("Duplicate IRN detected. Treating as SUCCESS and extracting previous IRN details.");
                            }
                        }

                        if (isSuccess && !isDuplicate)
                        {
                            var dataToken = jObj["Data"];
                            if (dataToken != null && dataToken.Type == Newtonsoft.Json.Linq.JTokenType.String)
                            {
                                // The new API returns the Data object as a serialized JSON string
                                var nestedObj = Newtonsoft.Json.Linq.JObject.Parse(dataToken.ToString());
                                irn = nestedObj["Irn"]?.ToString() ?? "";
                                ackNo = nestedObj["AckNo"]?.ToString() ?? "";
                                ackDt = nestedObj["AckDt"]?.ToString() ?? "";
                                signedInvoice = nestedObj["SignedInvoice"]?.ToString() ?? "";
                                signedQRCode = nestedObj["SignedQRCode"]?.ToString() ?? "";
                                ewbNo = jObj["EwbNo"]?.ToString() ?? nestedObj["EwbNo"]?.ToString() ?? "";
                                ewbValidTill = jObj["EwbValidTill"]?.ToString() ?? nestedObj["EwbValidTill"]?.ToString() ?? "";
                                ewbDt = jObj["EwbDt"]?.ToString() ?? nestedObj["EwbDt"]?.ToString() ?? "";
                            }
                            else if (dataToken != null && dataToken.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                            {
                                irn = dataToken["Irn"]?.ToString() ?? "";
                                ackNo = dataToken["AckNo"]?.ToString() ?? "";
                                ackDt = dataToken["AckDt"]?.ToString() ?? "";
                                signedInvoice = dataToken["SignedInvoice"]?.ToString() ?? "";
                                signedQRCode = dataToken["SignedQRCode"]?.ToString() ?? "";
                                ewbNo = dataToken["EwbNo"]?.ToString() ?? "";
                                ewbValidTill = dataToken["EwbValidTill"]?.ToString() ?? "";
                                ewbDt = dataToken["EwbDt"]?.ToString() ?? "";
                            }
                        }

                        if (isSuccess && !string.IsNullOrEmpty(irn))
                        {
                            string tableName = (objType == "13") ? "OINV" : (objType == "14" ? "ORIN" : "OWTR");

                            // Fetch Distance from InfoDtls if present
                            string distance = "";
                            try
                            {
                                var infoDtls = jObj["InfoDtls"] as Newtonsoft.Json.Linq.JArray;
                                if (infoDtls != null)
                                {
                                    foreach (var info in infoDtls)
                                    {
                                        if (info["InfCd"]?.ToString() == "EWBPPD")
                                        {
                                            string desc = info["Desc"]?.ToString() ?? "";
                                            var match = System.Text.RegularExpressions.Regex.Match(desc, @"\d+");
                                            if (match.Success) distance = match.Value;
                                        }
                                    }
                                }
                            }
                            catch { }

                            // Update SAP Document - ONLY use columns that exist on OINV
                            string updateQuery = $"UPDATE \"{tableName}\" SET \"Comments\"='SUCCESS', \"U_IRN\"='{irn}'";
                            if (!string.IsNullOrEmpty(ewbNo)) updateQuery += $", \"U_ewayBNo\"='{ewbNo}'";
                            if (!string.IsNullOrEmpty(ewbValidTill)) updateQuery += $", \"U_EwayVal\"='{ewbValidTill}'";
                            if (!string.IsNullOrEmpty(ewbDt)) updateQuery += $", \"U_ewayBDa\"='{ewbDt}'";
                            if (!string.IsNullOrEmpty(distance)) updateQuery += $", \"U_TotalDist\"='{distance}'";
                            updateQuery += $" WHERE \"DocEntry\"={docEntry}";

                            dbHelper.ExecuteNonQuery(updateQuery);
                            LoggerHelper.Log($"Successfully updated U_IRN and details on {tableName} {docEntry}");

                            // Upsert GIS_EI_ORES (to prevent Error 305 duplicate rows without deleting)
                            string checkExist = $"SELECT 1 FROM \"GIS_EI_ORES\" WHERE \"DocEntry\"={docEntry} AND \"ObjType\"='{sDocType}'";
                            var dt = dbHelper.ExecuteQuery(checkExist);
                            if (dt != null && dt.Rows.Count > 0)
                            {
                                string updateOres = $"UPDATE \"GIS_EI_ORES\" SET \"ResponseMessage\"='SUCCESS', \"Status\"='ACT', \"AckNo\"='{ackNo}', \"AckDt\"='{ackDt}', \"Irn\"='{irn}', \"EncryptedSignedInvoice\"='{signedInvoice}', \"EncryptedSignedQRCode\"='{signedQRCode}', \"IsCancel\"='', \"ResponseCode\"='1', \"RandomNo\"='', \"QRCodeImage\"=''";
                                if (!string.IsNullOrEmpty(ewbNo)) updateOres += $", \"EwbNo\"='{ewbNo}'";
                                if (!string.IsNullOrEmpty(ewbDt)) updateOres += $", \"EwbDt\"='{ewbDt}'";
                                if (!string.IsNullOrEmpty(ewbValidTill)) updateOres += $", \"EwbValidTill\"='{ewbValidTill}'";
                                updateOres += $" WHERE \"DocEntry\"={docEntry} AND \"ObjType\"='{sDocType}'";
                                dbHelper.ExecuteNonQuery(updateOres);
                                LoggerHelper.Log($"Successfully updated existing record in GIS_EI_ORES for DocEntry {docEntry}.");
                            }
                            else
                            {
                                string insertOres = $"INSERT INTO \"GIS_EI_ORES\" (\"DocEntry\", \"ObjType\", \"ResponseMessage\", \"Status\", \"AckNo\", \"AckDt\", \"Irn\", \"EncryptedSignedInvoice\", \"EncryptedSignedQRCode\", \"IsCancel\", \"ResponseCode\", \"RandomNo\", \"QRCodeImage\", \"EwbNo\", \"EwbDt\", \"EwbValidTill\") VALUES ('{docEntry}', '{sDocType}', 'SUCCESS', 'ACT', '{ackNo}', '{ackDt}', '{irn}', '{signedInvoice}', '{signedQRCode}', '', '1', '', '', '{ewbNo}', '{ewbDt}', '{ewbValidTill}')";
                                dbHelper.ExecuteNonQuery(insertOres);
                                LoggerHelper.Log($"Successfully inserted record into GIS_EI_ORES for DocEntry {docEntry}.");
                            }

                            // Call SAP DI API to automatically generate and bind the native QR code
                            if (!string.IsNullOrEmpty(signedQRCode))
                            {
                                LoggerHelper.Log("Binding native SAP QR Code via DI API...");
                                dbHelper.UpdateDocumentQRCode(objType, docEntry, signedQRCode);
                            }
                        }
                        else
                        {
                            LoggerHelper.Log("API returned an error. Logged to TEC_EI_LOG. Will NOT update OINV or GIS_EI_ORES to preserve historical successes.");
                            
                            // Check if Duplicate IRN logic could go here, but omitted to prevent overwriting.
                        }
                    }
                    catch (Exception parseEx)
                    {
                        LoggerHelper.Log("Failed to parse API response: " + parseEx.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Log($"CRITICAL ERROR in CombinedResponseHandler: {ex.Message}");
                throw;
            }
        }
    }
}