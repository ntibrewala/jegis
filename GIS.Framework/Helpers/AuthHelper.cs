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
            string urlQuery = "SELECT MAX(\"U_Url\") FROM \"@GIS_EW_OAPI\" WHERE \"U_Action\" = 'GenerateKey'";
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
                { "UserName", credentials.UserName },       // Strictly 'UserName'
                { "eInvPwd", credentials.ApiPassword },     // Required for E-Invoice token
                { "EwbPwd", credentials.ApiPassword }       // Required for E-Way Bill token in v1.03
            };
            
            LoggerHelper.Log($"No valid token in cache. Making live HTTP call to {authUrl}...");
            var tokenResponse = await HttpHelper.GetAsync<TokenResponse>(authUrl, headers);
            
            if (tokenResponse.Status == 1 && tokenResponse.Data != null)
            {
                LoggerHelper.Log("Live fetch successful. Saving new token back to database cache...");
                CredentialManager.SaveTokenToCache(
                    dbHelper, 
                    credentials.GSTIN, 
                    tokenResponse.Data.ClientId, 
                    tokenResponse.Data.UserName, 
                    tokenResponse.Data.AuthToken, 
                    tokenResponse.Data.TokenExpiry
                );
            }

            return tokenResponse;
        }

        public static TokenResponse GetCharteredAuthTokenSync(IDatabaseHelper dbHelper, string objType, string docEntry)
        {
            return GetCharteredAuthTokenAsync(dbHelper, objType, docEntry).GetAwaiter().GetResult();
        }
    }
}
