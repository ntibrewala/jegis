using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GIS.Framework.Helpers
{
    public static class ApiRunner
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static string PostInvoice(string jsonPayload, string authToken, string gstin, string userName, string password, string aspId, string targetUrl)
        {
            LoggerHelper.Log("Entered ApiRunner.PostInvoice");
            LoggerHelper.Log($"Starting API call to CharteredInfo POST endpoint: {targetUrl}...");
            var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);

            // CharteredInfo E-Invoice API Headers
            request.Headers.Add("aspid", aspId);
            request.Headers.Add("password", password);
            request.Headers.Add("ip-usr", "127.0.0.1"); // Mandatory for NIC
            request.Headers.Add("Gstin", gstin);
            request.Headers.Add("user_name", userName);
            request.Headers.Add("AuthToken", authToken);

            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                LoggerHelper.Log($"API Response StatusCode: {(int)response.StatusCode}");
                LoggerHelper.Log($"API Response Body: {responseBody}");

                LoggerHelper.Log("Exited ApiRunner.PostInvoice");
                return responseBody;
            }
            catch (Exception ex)
            {
                LoggerHelper.Log($"Critical Error during API call: {ex.Message}");
                LoggerHelper.Log("Exited ApiRunner.PostInvoice (with Exception)");
                throw;
            }
        }

        public static string PostCancelInvoice(string jsonPayload, string authToken, string gstin, string userName, string password, string aspId, string targetUrl)
        {
            LoggerHelper.Log("Entered ApiRunner.PostCancelInvoice");
            LoggerHelper.Log($"Starting API call to CharteredInfo Cancel endpoint: {targetUrl}...");
            var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);

            // CharteredInfo E-Invoice API Headers
            request.Headers.Add("aspid", aspId);
            request.Headers.Add("password", password);
            request.Headers.Add("ip-usr", "127.0.0.1"); // Mandatory for NIC
            request.Headers.Add("Gstin", gstin);
            request.Headers.Add("user_name", userName);
            request.Headers.Add("AuthToken", authToken);

            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                LoggerHelper.Log($"API Response StatusCode: {(int)response.StatusCode}");
                LoggerHelper.Log($"API Response Body: {responseBody}");

                LoggerHelper.Log("Exited ApiRunner.PostCancelInvoice");
                return responseBody;
            }
            catch (Exception ex)
            {
                LoggerHelper.Log($"Critical Error during Cancel API call: {ex.Message}");
                LoggerHelper.Log("Exited ApiRunner.PostCancelInvoice (with Exception)");
                throw;
            }
        }
    }
}