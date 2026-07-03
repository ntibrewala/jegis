using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GIS.Framework.Helpers
{
    /// <summary>
    /// Generalized HTTP Helper to manage HttpClient instances and basic JSON requests.
    /// Does NOT know anything about specific APIs (like CharteredInfo or SAP).
    /// </summary>
    internal static class HttpHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Sends a generic GET request with custom headers and returns a deserialized object.
        /// </summary>
        public static async Task<T> GetAsync<T>(string url, Dictionary<string, string> headers)
        {
            LoggerHelper.Log($"Preparing generic GET request to {url}");
            
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }
                
                request.Headers.Add("Accept", "application/json");

                try
                {
                    LoggerHelper.Log($"Sending request...");
                    using (var response = await _httpClient.SendAsync(request))
                    {
                        LoggerHelper.Log($"Received response with StatusCode: {(int)response.StatusCode}");
                        string json = await response.Content.ReadAsStringAsync();
                        LoggerHelper.Log($"Response body: {json}");
                        
                        if (!response.IsSuccessStatusCode)
                        {
                            LoggerHelper.Log($"Warning: API returned non-success status code {(int)response.StatusCode}. Attempting to parse error JSON...");
                        }
                        
                        return JsonConvert.DeserializeObject<T>(json);
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError($"HttpHelper.GetAsync to {url}", ex);
                    throw;
                }
            }
        }
    }
}
