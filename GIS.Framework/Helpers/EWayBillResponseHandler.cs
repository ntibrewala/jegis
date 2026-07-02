using GIS.Framework.Interfaces;
using Newtonsoft.Json.Linq;
using System;

namespace GIS.Framework.Helpers
{
    public class EWayBillResponseHandler : IResponseHandler
    {
        public void Handle(IDatabaseHelper dbHelper, string objType, string docEntry, string payload, string apiResponse)
        {
            LoggerHelper.Log($"Handling API Response for E-Way Bill Generation (DocEntry: {docEntry})...");

            string tableName = (objType == "13") ? "OINV" : (objType == "14") ? "ORIN" : "OWTR";
            
            LoggerHelper.Log($"Request Payload: {payload}");
            LoggerHelper.Log($"API Response: {apiResponse}");
            
            // 1. Log the RAW request/response into TEC_EW_LOG
            string safePayload = payload.Replace("'", "''");
            string safeResponse = apiResponse.Replace("'", "''");
            string logQuery = $"INSERT INTO \"TEC_EW_LOG\" (\"DocEntry\", \"ObjType\", \"Request\", \"Response\", \"CreateDate\", \"UserSign\") VALUES('{docEntry}', '{objType}', '{safePayload}', '{safeResponse}', CURRENT_TIMESTAMP, 'System')";
            try {
                dbHelper.ExecuteNonQuery(logQuery);
            } catch (Exception ex) {
                LoggerHelper.Log($"Failed to write to TEC_EW_LOG: {ex.Message}");
            }

            try
            {
                JObject jObj = JObject.Parse(apiResponse);
                
                // Check if it's an error response
                if (jObj["error"] != null || jObj["status_cd"]?.ToString() == "0")
                {
                    string errorMsg = jObj["error"]?["message"]?.ToString() ?? "Unknown API Error";
                    LoggerHelper.Log($"API returned error: {errorMsg}");
                    return;
                }

                // E-Way API returns ewayBillNo directly in the root object on success
                string ewbNo = jObj["ewayBillNo"]?.ToString() ?? "";
                
                string rawValidUpto = jObj["validUpto"]?.ToString() ?? "";
                string validUpto = rawValidUpto;
                if (DateTime.TryParseExact(rawValidUpto, "dd/MM/yyyy hh:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dtValidExact))
                    validUpto = dtValidExact.ToString("yyyy-MM-dd HH:mm:ss");
                else if (DateTime.TryParse(rawValidUpto, out DateTime dtValid))
                    validUpto = dtValid.ToString("yyyy-MM-dd HH:mm:ss");
                    
                string rawEwbDate = jObj["ewayBillDate"]?.ToString() ?? "";
                string ewbDate = rawEwbDate;
                if (DateTime.TryParseExact(rawEwbDate, "dd/MM/yyyy hh:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dtEwbExact))
                    ewbDate = dtEwbExact.ToString("yyyy-MM-dd HH:mm:ss");
                else if (DateTime.TryParse(rawEwbDate, out DateTime dtEwb))
                    ewbDate = dtEwb.ToString("yyyy-MM-dd HH:mm:ss");
                
                string alert = jObj["alert"]?.ToString() ?? "";
                string distance = "";
                if (!string.IsNullOrEmpty(alert))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(alert, @"\d+");
                    if (match.Success)
                    {
                        distance = match.Value;
                    }
                }
                
                if (!string.IsNullOrEmpty(ewbNo))
                {
                    // 2. Update SAP Document (OINV / ORIN / OWTR)
                    string updateDocQ = $"UPDATE \"{tableName}\" SET \"U_ewayBNo\" = '{ewbNo}', \"U_EwayVal\" = '{validUpto}', \"U_ewayBDa\" = '{ewbDate}'";
                    if (!string.IsNullOrEmpty(distance))
                    {
                        updateDocQ += $", \"U_TotalDist\" = '{distance}'";
                    }
                    updateDocQ += $" WHERE \"DocEntry\" = {docEntry}";
                    
                    dbHelper.ExecuteNonQuery(updateDocQ);
                    LoggerHelper.Log($"Successfully updated SAP Document {tableName} with E-Way Bill Number: {ewbNo} and Distance: {distance}");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Log($"Error while parsing JSON response for E-Way Bill: {ex.Message}");
            }
        }
    }

    public class EWayBillCancelResponseHandler : IResponseHandler
    {
        public void Handle(IDatabaseHelper dbHelper, string objType, string docEntry, string payload, string apiResponse)
        {
            LoggerHelper.Log($"Handling API Response for E-Way Bill Cancel (DocEntry: {docEntry})...");

            string tableName = (objType == "13") ? "OINV" : (objType == "14") ? "ORIN" : "OWTR";
            
            LoggerHelper.Log($"Cancel Request Payload: {payload}");
            LoggerHelper.Log($"Cancel API Response: {apiResponse}");
            
            // 1. Log the RAW request/response into TEC_EW_LOG
            string safePayload = payload.Replace("'", "''");
            string safeResponse = apiResponse.Replace("'", "''");
            string logQuery = $"INSERT INTO \"TEC_EW_LOG\" (\"DocEntry\", \"ObjType\", \"Request\", \"Response\", \"CreateDate\", \"UserSign\") VALUES('{docEntry}', '{objType}', '{safePayload}', '{safeResponse}', CURRENT_TIMESTAMP, 'System')";
            try {
                dbHelper.ExecuteNonQuery(logQuery);
            } catch (Exception ex) {
                LoggerHelper.Log($"Failed to write to TEC_EW_LOG: {ex.Message}");
            }

            try
            {
                JObject jObj = JObject.Parse(apiResponse);
                
                // Check if it's an error response
                if (jObj["error"] != null || jObj["status_cd"]?.ToString() == "0")
                {
                    string errorMsg = jObj["error"]?["message"]?.ToString() ?? "Unknown API Error";
                    LoggerHelper.Log($"API returned error: {errorMsg}");
                    return;
                }

                // If ewayBillNo exists in the root, cancellation succeeded
                if (jObj["ewayBillNo"] != null)
                {
                    // Fetch existing EWay Bill number so we can append "- Cancelled"
                    string query = $"SELECT \"U_ewayBNo\" FROM \"{tableName}\" WHERE \"DocEntry\"={docEntry}";
                    var dt = dbHelper.ExecuteQuery(query);
                    string ewbNo = "";
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        ewbNo = dt.Rows[0]["U_ewayBNo"]?.ToString() ?? "";
                    }

                    if (!string.IsNullOrEmpty(ewbNo) && !ewbNo.Contains("Cancelled"))
                    {
                        ewbNo = ewbNo + " - Cancelled";
                    }

                    // 2. Update SAP Document (OINV / ORIN / OWTR)
                    string updateDocQ = $"UPDATE \"{tableName}\" SET \"U_ewayBNo\" = '{ewbNo}', \"U_CanclDte\" = CURRENT_DATE WHERE \"DocEntry\" = {docEntry}";
                    dbHelper.ExecuteNonQuery(updateDocQ);
                    LoggerHelper.Log($"Successfully updated SAP Document {tableName} for E-Way Bill Cancel!");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Log($"Error while parsing JSON response for E-Way Bill Cancel: {ex.Message}");
            }
        }
    }
}
