using System;
using Newtonsoft.Json.Linq;
using GIS.Framework.Interfaces;

namespace GIS.Framework.Helpers
{
    public static class CancelResponseHandler
    {
        public static void RouteCancelResponse(IDatabaseHelper dbHelper, string objType, string docEntry, string requestPayload, string rawJsonResponse)
        {
            try
            {
                // Always log the raw cancellation response to TEC_EI_LOG
                string safeRequest = requestPayload.Replace("'", "''");
                string safeResponse = rawJsonResponse.Replace("'", "''");
                string sDocType = objType;
                string sDocTypeLog = (objType == "13") ? "IN" : (objType == "14") ? "CM" : objType;
                string logQuery = $"INSERT INTO \"TEC_EI_LOG\" VALUES('{docEntry}', '{sDocTypeLog}', '{safeRequest}', '{safeResponse}', CURRENT_TIMESTAMP, 'Cancel')";
                dbHelper.ExecuteNonQuery(logQuery);

                var jObj = JObject.Parse(rawJsonResponse);
                string status = jObj["Status"]?.ToString() ?? "";
                
                if (status == "1" || status.ToLower() == "true")
                {
                    // Success!
                    var dataToken = jObj["Data"];
                    string cancelDate = "";
                    if (dataToken != null && dataToken.Type == JTokenType.String)
                    {
                        var nestedObj = JObject.Parse(dataToken.ToString());
                        cancelDate = nestedObj["CancelDate"]?.ToString() ?? "";
                    }
                    else if (dataToken != null)
                    {
                        cancelDate = dataToken["CancelDate"]?.ToString() ?? "";
                    }

                    // Format Cancel Date
                    string formattedCancelDate = cancelDate;
                    if (DateTime.TryParse(cancelDate, out DateTime cDate))
                    {
                        formattedCancelDate = cDate.ToString("yyyyMMdd");
                    }
                    else
                    {
                        formattedCancelDate = DateTime.Now.ToString("yyyyMMdd");
                    }

                    // 1. Update GIS_EI_ORES to reflect cancellation
                    string updateOres = $"UPDATE \"GIS_EI_ORES\" SET \"Status\"='CNL', \"IsCancel\"='Y', \"ResponseMessage\"='CANCELLED' WHERE \"DocEntry\"={docEntry} AND \"ObjType\"='{sDocType}'";
                    dbHelper.ExecuteNonQuery(updateOres);

                    // 2. Update SAP Document (OINV / ORIN)
                    string docTable = (objType == "13") ? "OINV" : (objType == "14") ? "ORIN" : "";
                    if (!string.IsNullOrEmpty(docTable))
                    {
                        // Prefix U_IRN and U_ewayBNo with 'Cancelled '
                        string updateSapDoc = $"UPDATE \"{docTable}\" SET \"U_CanclDte\" = '{formattedCancelDate}', " +
                                              $"\"U_IRN\" = CASE WHEN \"U_IRN\" NOT LIKE 'Cancelled %' THEN 'Cancelled ' || IFNULL(\"U_IRN\",'') ELSE \"U_IRN\" END, " +
                                              $"\"U_ewayBNo\" = CASE WHEN \"U_ewayBNo\" IS NOT NULL AND \"U_ewayBNo\" <> '' AND \"U_ewayBNo\" NOT LIKE 'Cancelled %' THEN 'Cancelled ' || \"U_ewayBNo\" ELSE \"U_ewayBNo\" END " +
                                              $"WHERE \"DocEntry\" = {docEntry}";
                        dbHelper.ExecuteNonQuery(updateSapDoc);
                    }

                    LoggerHelper.Log($"Successfully cancelled IRN for DocEntry {docEntry}.");
                }
                else
                {
                    LoggerHelper.Log($"API returned an error during cancellation. Did not update SAP tables.");
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Log($"Failed to process Cancel Response: {ex.Message}");
                throw;
            }
        }
    }
}
