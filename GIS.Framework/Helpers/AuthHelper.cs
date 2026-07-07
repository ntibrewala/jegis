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
        public static async Task<TokenResponse> GetCharteredAuthTokenAsync(IDatabaseHelper dbHelper, string objType, string docEntry)
        {
            LoggerHelper.Log("Entered AuthHelper.GetCharteredAuthTokenAsync");
            LoggerHelper.Log($"Starting CharteredAuthToken fetch process for ObjType: {objType}, DocEntry: {docEntry}...");
            
            // 1. Fetch Credentials & Check Cache
            var credentials = CredentialManager.GetCredentialsForDocument(dbHelper, objType, docEntry);
            
            // Check if token exists and is valid for at least the next 5 minutes (buffer)
            bool isTokenValid = false;
            if (!string.IsNullOrEmpty(credentials.ExistingAuthToken) && credentials.ExistingTokenExpiry.HasValue)
            {
                if (credentials.ExistingTokenExpiry.Value > DateTime.Now.AddMinutes(5))
                {
                    isTokenValid = true;
                }
                else
                {
                    LoggerHelper.Log($"Token exists but expires soon ({credentials.ExistingTokenExpiry.Value}). Fetching new token...");
                }
            }

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

            // Fetch dynamic AUTH_URL from database
            string authUrl = "";
            string urlQuery = "SELECT MAX(\"U_Url\") FROM \"@GIS_EI_OAPI\" WHERE \"U_Action\" = 'GenerateKey'";
            var dtUrl = dbHelper.ExecuteQuery(urlQuery);
            if (dtUrl != null && dtUrl.Rows.Count > 0 && dtUrl.Rows[0][0] != DBNull.Value)
            {
                authUrl = dtUrl.Rows[0][0].ToString();
            }
            if (string.IsNullOrEmpty(authUrl))
            {
                authUrl = "https://gstsandbox.charteredinfo.com/eivital/dec/v1.04/auth"; // fallback
            }

            // 2. Map DB Credentials to HTTP Headers
            // The Production Chartered Info API v1.03 is strict about header casing and requires EwbPwd for combined generation
            var headers = new Dictionary<string, string>
            {
                { "aspid", credentials.AspId },
                { "password", credentials.AspPassword },
                { "Gstin", credentials.GSTIN },             // Strictly 'Gstin'
                { "user_name", credentials.UserName },       // Strictly 'user_name' in v1.04
                { "eInvPwd", credentials.ApiPassword }      // Required for E-Invoice token
            };
            
            LoggerHelper.Log($"No valid token in cache. Making live HTTP call to {authUrl}...");
            var tokenResponse = await HttpHelper.GetAsync<TokenResponse>(authUrl, headers);
            
            bool isSuccess = tokenResponse.Status == 1 || tokenResponse.status == "1";
            string newToken = tokenResponse.Data?.AuthToken ?? tokenResponse.authtoken;

            if (isSuccess && !string.IsNullOrEmpty(newToken))
            {
                LoggerHelper.Log($"Successfully fetched new AuthToken from CharteredInfo API.");
                
                // 3. Cache the Token in SAP DB
                string calculatedExpiry = DateTime.Now.AddHours(6).ToString("yyyy-MM-dd HH:mm:ss");
                try {
                    CredentialManager.SaveTokenToCache(
                        dbHelper, 
                        credentials.GSTIN, 
                        credentials.ClientId ?? "", 
                        credentials.UserName, 
                        newToken, 
                        calculatedExpiry
                    );
                    LoggerHelper.Log($"Successfully cached new AuthToken for GSTIN {credentials.GSTIN}.");
                } catch (Exception ex) {
                    LoggerHelper.Log($"Warning: Failed to cache AuthToken to TEC_AUTHTOKEN. Error: {ex.Message}");
                }
                
                // Populate Data for downstream consumers if flat format
                if (tokenResponse.Data == null)
                {
                    tokenResponse.Data = new TokenData { AuthToken = newToken, TokenExpiry = calculatedExpiry };
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

        public static TokenResponse GetCharteredAuthTokenSync(IDatabaseHelper dbHelper, string objType, string docEntry)
        {
            return GetCharteredAuthTokenAsync(dbHelper, objType, docEntry).GetAwaiter().GetResult();
        }
    }
}
