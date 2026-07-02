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
            string whsQuery = "";
            if (objType == "13" || objType == "Invoice") whsQuery = $"SELECT MAX(\"WhsCode\") FROM \"INV1\" WHERE \"DocEntry\" = {docEntry}";
            else if (objType == "14" || objType == "CreditMemo") whsQuery = $"SELECT MAX(\"WhsCode\") FROM \"RIN1\" WHERE \"DocEntry\" = {docEntry}";
            else if (objType == "67" || objType == "Transfer") whsQuery = $"SELECT MAX(\"WhsCode\") FROM \"WTR1\" WHERE \"DocEntry\" = {docEntry}";

            string gstin = null;
            if (!string.IsNullOrEmpty(whsQuery))
            {
                DataTable dtWhs = dbHelper.ExecuteQuery(whsQuery);
                if (dtWhs != null && dtWhs.Rows.Count > 0 && dtWhs.Rows[0][0] != DBNull.Value)
                {
                    string whsCode = dtWhs.Rows[0][0].ToString();
                    string locQuery = $"SELECT MAX(\"Location\") FROM \"OWHS\" WHERE \"WhsCode\" = '{whsCode}'";
                    DataTable dtLoc = dbHelper.ExecuteQuery(locQuery);
                    
                    if (dtLoc != null && dtLoc.Rows.Count > 0 && dtLoc.Rows[0][0] != DBNull.Value)
                    {
                        string locCode = dtLoc.Rows[0][0].ToString();
                        string stateQuery = $"SELECT MAX(T1.\"GSTCode\") FROM \"OLCT\" T0 INNER JOIN \"OCST\" T1 ON T0.\"State\" = T1.\"Code\" AND T1.\"Country\" = 'IN' WHERE T0.\"Code\" = {locCode}";
                        DataTable dtState = dbHelper.ExecuteQuery(stateQuery);
                        
                        if (dtState != null && dtState.Rows.Count > 0 && dtState.Rows[0][0] != DBNull.Value)
                        {
                            string stateCode = dtState.Rows[0][0].ToString();
                            string gstinQuery = $"SELECT MAX(\"U_GSTIN\") FROM \"@GIS_EI_OGST\" WHERE \"Name\" = '{stateCode}'";
                            DataTable dtGstin = dbHelper.ExecuteQuery(gstinQuery);
                            
                            if (dtGstin != null && dtGstin.Rows.Count > 0 && dtGstin.Rows[0][0] != DBNull.Value)
                            {
                                gstin = dtGstin.Rows[0][0].ToString();
                            }
                        }
                    }
                }
            }

            string finalQuery = "";
            if (string.IsNullOrEmpty(gstin))
            {
                finalQuery = "SELECT TOP 1 A.\"U_GSTIN\", A.\"U_AspID\", A.\"U_AspPwd\", A.\"U_UserName\", A.\"U_Password\", A.\"U_CltID\", A.\"U_CltSecrt\", T.\"AuthToken\", T.\"TokenExpiry\" FROM \"@GIS_EI_OGST\" A LEFT JOIN \"TEC_AUTHTOKEN\" T ON A.\"U_GSTIN\" = T.\"GSTIN\" AND T.\"Type\" = 'E-Invoice' AND T.\"TokenExpiry\" > CURRENT_TIMESTAMP";
            }
            else
            {
                finalQuery = $"SELECT TOP 1 A.\"U_GSTIN\", A.\"U_AspID\", A.\"U_AspPwd\", A.\"U_UserName\", A.\"U_Password\", A.\"U_CltID\", A.\"U_CltSecrt\", T.\"AuthToken\", T.\"TokenExpiry\" FROM \"@GIS_EI_OGST\" A LEFT JOIN \"TEC_AUTHTOKEN\" T ON A.\"U_GSTIN\" = T.\"GSTIN\" AND T.\"Type\" = 'E-Invoice' AND T.\"TokenExpiry\" > CURRENT_TIMESTAMP WHERE A.\"U_GSTIN\" = '{gstin}'";
            }
            
            DataTable result = dbHelper.ExecuteQuery(finalQuery);
            
            if (result == null || result.Rows.Count == 0)
            {
                throw new Exception($"Could not find valid credentials for ObjType {objType}, DocEntry {docEntry}");
            }
            
            var row = result.Rows[0];
            
            var creds = new ApiCredentials
            {
                GSTIN = row["U_GSTIN"]?.ToString(),
                AspId = row["U_AspID"]?.ToString(),
                AspPassword = row["U_AspPwd"]?.ToString(),
                UserName = row["U_UserName"]?.ToString(),
                ApiPassword = row["U_Password"]?.ToString(),
                ClientId = row["U_CltID"]?.ToString(),
                ClientSecret = row["U_CltSecrt"]?.ToString()
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
