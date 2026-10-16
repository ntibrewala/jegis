using System;
using System.Data;
using GIS.Framework.Interfaces;
using GIS.Framework.Models;

namespace GIS.Framework.Helpers
{
    public static class CredentialManager
    {
        public static ApiCredentials GetCredentialsForDocument(IDatabaseHelper dbHelper, string objType, string docEntry)
        {
            string query = $"CALL \"TEC_COMBINED_TOKEN_GET\" ('{objType}', '{docEntry}')";
            
            DataTable result = dbHelper.ExecuteQuery(query);
            
            if (result == null || result.Rows.Count == 0)
            {
                throw new Exception($"Could not find valid credentials for ObjType {objType}, DocEntry {docEntry}");
            }
            
            var row = result.Rows[0];
            
            var creds = new ApiCredentials
            {
                GSTIN = row["GSTIN"]?.ToString(),
                AspId = row["AspId"]?.ToString(),
                AspPassword = row["AspPassword"]?.ToString(),
                UserName = row["UserName"]?.ToString(),
                ApiPassword = row["ApiPassword"]?.ToString(),
                ClientId = row["ClientId"]?.ToString(),
                ClientSecret = row["ClientSecret"]?.ToString()
            };

            // Safely parse the token information if it exists in the cache
            if (row.Table.Columns.Contains("AuthToken") && row["AuthToken"] != DBNull.Value && !string.IsNullOrEmpty(row["AuthToken"].ToString()))
            {
                creds.ExistingAuthToken = row["AuthToken"].ToString();
                
                if (row["TokenExpiry"] != DBNull.Value)
                {
                    if (DateTime.TryParse(row["TokenExpiry"].ToString(), out DateTime expiry))
                    {
                        creds.ExistingTokenExpiry = expiry;
                    }
                }
            }

            return creds;
        }

        public static void ClearCachedToken(IDatabaseHelper dbHelper, string gstin)
        {
            if (string.IsNullOrEmpty(gstin)) return;
            string deleteQuery = $"DELETE FROM \"TEC_AUTHTOKEN\" WHERE \"GSTIN\" = '{gstin}'";
            dbHelper.ExecuteNonQuery(deleteQuery);
        }

        public static void SaveTokenToCache(IDatabaseHelper dbHelper, string gstin, string clientId, string userName, string authToken, string tokenExpiryString)
        {
            // Prevent extending expiry if Chartered Info returned the exact same old token without an explicit new expiry
            try
            {
                var dtExisting = dbHelper.ExecuteQuery($"SELECT \"AuthToken\" FROM \"TEC_AUTHTOKEN\" WHERE \"GSTIN\" = '{gstin}'");
                if (dtExisting != null && dtExisting.Rows.Count > 0)
                {
                    string prevToken = dtExisting.Rows[0]["AuthToken"]?.ToString() ?? "";
                    if (prevToken == authToken && string.IsNullOrEmpty(tokenExpiryString))
                    {
                        LoggerHelper.Log($"Chartered Info returned existing token for GSTIN {gstin}; preserving original expiry instead of extending +6h.");
                        return;
                    }
                }
            }
            catch { }

            // Use API-provided expiry if available; otherwise use HANA server clock (+5h 50m = 21000s) to avoid PC vs DB clock skew
            string expirySql = string.IsNullOrEmpty(tokenExpiryString)
                ? "ADD_SECONDS(CURRENT_TIMESTAMP, 21000)"
                : $"'{tokenExpiryString}'";
            
            // Delete old token
            string deleteQuery = $"DELETE FROM \"TEC_AUTHTOKEN\" WHERE \"GSTIN\" = '{gstin}'";
            dbHelper.ExecuteNonQuery(deleteQuery);
            
            // Insert new token
            string insertQuery = $@"
                INSERT INTO ""TEC_AUTHTOKEN"" (""GSTIN"", ""ClientId"", ""UserName"", ""Type"", ""AuthToken"", ""TokenExpiry"", ""CreateDate"")
                VALUES ('{gstin}', '{clientId}', '{userName}', 'E-Invoice', '{authToken}', {expirySql}, CURRENT_TIMESTAMP)";
                
            dbHelper.ExecuteNonQuery(insertQuery);
        }
    }
}
