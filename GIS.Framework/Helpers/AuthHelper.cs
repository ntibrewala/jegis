using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GIS.Framework.Configuration;
using GIS.Framework.Models;
using GIS.Framework.Interfaces;

namespace GIS.Framework.Helpers
{
    /// <summary>
    /// Orchestrates the authentication flow by binding credentials to the HttpHelper.
    /// </summary>
    public static class AuthHelper
    {
        public static bool IsTokenExpiredError(string apiResponse)
        {
            if (string.IsNullOrEmpty(apiResponse)) return false;
            return apiResponse.IndexOf("GSP752", StringComparison.OrdinalIgnoreCase) >= 0
                || apiResponse.IndexOf("AuthToken not found or expired", StringComparison.OrdinalIgnoreCase) >= 0
                || apiResponse.IndexOf("Invalid Token", StringComparison.OrdinalIgnoreCase) >= 0
                || apiResponse.IndexOf("Token Expired", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static async Task<TokenResponse> GetCharteredAuthTokenAsync(IDatabaseHelper dbHelper, string objType, string docEntry, bool forceRefresh = false)
        {
            LoggerHelper.Log("Entered AuthHelper.GetCharteredAuthTokenAsync");
            LoggerHelper.Log($"Starting CharteredAuthToken fetch process for ObjType: {objType}, DocEntry: {docEntry}, ForceRefresh: {forceRefresh}...");
            
            // 1. Fetch Credentials & Check Cache
            var credentials = CredentialManager.GetCredentialsForDocument(dbHelper, objType, docEntry);

            if (forceRefresh)
            {
                LoggerHelper.Log($"ForceRefresh requested for GSTIN {credentials.GSTIN}. Evicting cached token from TEC_AUTHTOKEN...");
                CredentialManager.ClearCachedToken(dbHelper, credentials.GSTIN);
                credentials.ExistingAuthToken = null;
                credentials.ExistingTokenExpiry = null;
            }
            
            // Trust TEC_COMBINED_TOKEN_GET (which already validates TokenExpiry > CURRENT_TIMESTAMP on the HANA DB clock).
            // Do NOT prematurely invalidate using PC clock AddMinutes(5), because calling /auth before true NIC expiry
            // causes Chartered Info to return the same dying token.
            bool isTokenValid = !forceRefresh && !string.IsNullOrEmpty(credentials.ExistingAuthToken);

            if (isTokenValid)
            {
                LoggerHelper.Log($"Found valid, unexpired AuthToken in database cache for GSTIN {credentials.GSTIN}. Skipping HTTP API call.");
                LoggerHelper.Log("Exited AuthHelper.GetCharteredAuthTokenAsync");
                return new TokenResponse 
                { 
                    Status = 1, 
                    Data = new TokenData 
                    { 
                        AuthToken = credentials.ExistingAuthToken,
                        TokenExpiry = credentials.ExistingTokenExpiry?.ToString("yyyy-MM-dd HH:mm:ss")
                    } 
                };
            }

            // Fetch dynamic AUTH_URL from database (@GIS_EI_OAPI or fallback to @GIS_EW_OAPI)
            string authUrl = "";
            string urlQuery = "SELECT MAX(\"U_Url\") FROM \"@GIS_EI_OAPI\" WHERE \"U_Action\" = 'GenerateKey'";
            var dtUrl = dbHelper.ExecuteQuery(urlQuery);
            if (dtUrl != null && dtUrl.Rows.Count > 0 && dtUrl.Rows[0][0] != DBNull.Value)
            {
                authUrl = dtUrl.Rows[0][0].ToString();
            }
            if (string.IsNullOrEmpty(authUrl))
            {
                var dtEwUrl = dbHelper.ExecuteQuery("SELECT MAX(\"U_Url\") FROM \"@GIS_EW_OAPI\" WHERE \"U_Action\" = 'GenerateKey'");
                if (dtEwUrl != null && dtEwUrl.Rows.Count > 0 && dtEwUrl.Rows[0][0] != DBNull.Value)
                {
                    authUrl = dtEwUrl.Rows[0][0].ToString();
                }
            }
            if (string.IsNullOrEmpty(authUrl))
            {
                authUrl = "https://einvapi.charteredinfo.com/v1.03/dec/auth?action=ACCESSTOKEN"; // fallback
            }

            // 2. Map DB Credentials to HTTP Headers (support both v1.04 eivital and v1.03 EWB ACCESSTOKEN endpoints)
            var headers = new Dictionary<string, string>
            {
                { "aspid", credentials.AspId },
                { "password", credentials.AspPassword },
                { "Gstin", credentials.GSTIN },
                { "user_name", credentials.UserName },       // Required by v1.04 E-Invoice Auth
                { "UserName", credentials.UserName },        // Required by v1.03 E-Way Bill Auth
                { "eInvPwd", credentials.ApiPassword },      // Required by v1.04 E-Invoice Auth
                { "EwbPwd", credentials.ApiPassword }        // Required by v1.03 E-Way Bill Auth
            };

            if (forceRefresh)
            {
                headers["ForceRefreshAccessToken"] = "true";
            }
            
            LoggerHelper.Log($"No valid token in cache. Making live HTTP call to {authUrl}...");
            var tokenResponse = await HttpHelper.GetAsync<TokenResponse>(authUrl, headers);
            
            bool isSuccess = tokenResponse.Status == 1 || tokenResponse.status == "1";
            string newToken = tokenResponse.Data?.AuthToken ?? tokenResponse.authtoken;
            string apiExpiry = tokenResponse.Data?.TokenExpiry ?? tokenResponse.TokenExpiry;

            if (isSuccess && !string.IsNullOrEmpty(newToken))
            {
                LoggerHelper.Log($"Successfully fetched new AuthToken from CharteredInfo API.");
                
                // 3. Cache the Token in SAP DB (uses API TokenExpiry if provided, or HANA CURRENT_TIMESTAMP + 5h 50m)
                try {
                    CredentialManager.SaveTokenToCache(
                        dbHelper, 
                        credentials.GSTIN, 
                        credentials.ClientId ?? "", 
                        credentials.UserName, 
                        newToken, 
                        apiExpiry
                    );
                    LoggerHelper.Log($"Successfully cached new AuthToken for GSTIN {credentials.GSTIN}.");
                } catch (Exception ex) {
                    LoggerHelper.Log($"Warning: Failed to cache AuthToken to TEC_AUTHTOKEN. Error: {ex.Message}");
                }
                
                // Populate Data for downstream consumers if flat format
                if (tokenResponse.Data == null)
                {
                    tokenResponse.Data = new TokenData { AuthToken = newToken, TokenExpiry = apiExpiry };
                }
                
                LoggerHelper.Log("Exited AuthHelper.GetCharteredAuthTokenAsync");
                return tokenResponse;
            }
            else
            {
                LoggerHelper.Log("Exited AuthHelper.GetCharteredAuthTokenAsync (with Exception)");
                throw new Exception($"Auth API Error: {tokenResponse.error?.message ?? tokenResponse.ErrorDetails ?? "Unknown Error or Missing Token"}");
            }
        }

        public static TokenResponse GetCharteredAuthTokenSync(IDatabaseHelper dbHelper, string objType, string docEntry, bool forceRefresh = false)
        {
            return GetCharteredAuthTokenAsync(dbHelper, objType, docEntry, forceRefresh).GetAwaiter().GetResult();
        }
    }
}
