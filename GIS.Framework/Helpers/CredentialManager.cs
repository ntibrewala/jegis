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

        public static void SaveTokenToCache(IDatabaseHelper dbHelper, string gstin, string clientId, string userName, string authToken, string tokenExpiryString)
        {
            // If the table is User Defined, make sure it matches the exact schema
            // We use standard UPSERT logic (or delete/insert) to cache the token for this GSTIN
            string safeExpiry = string.IsNullOrEmpty(tokenExpiryString) ? "NULL" : $"'{tokenExpiryString}'";
            
            // Delete old token
            string deleteQuery = $"DELETE FROM \"TEC_AUTHTOKEN\" WHERE \"GSTIN\" = '{gstin}' AND \"Type\" = 'E-Invoice'";
            dbHelper.ExecuteNonQuery(deleteQuery);
            
            // Insert new token
            string insertQuery = $@"
                INSERT INTO ""TEC_AUTHTOKEN"" (""GSTIN"", ""ClientId"", ""UserName"", ""Type"", ""AuthToken"", ""TokenExpiry"", ""CreateDate"")
                VALUES ('{gstin}', '{clientId}', '{userName}', 'E-Invoice', '{authToken}', {safeExpiry}, CURRENT_TIMESTAMP)";
                
            dbHelper.ExecuteNonQuery(insertQuery);
        }
    }
}
