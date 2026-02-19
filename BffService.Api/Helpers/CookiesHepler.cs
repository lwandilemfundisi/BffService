using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace BffService.Api.Helpers
{
    public static class CookiesHepler
    {
        public static async Task<bool> IsTokenActive(HttpClient httpClient, string accessToken)
        {
            var introspectionRequest = new HttpRequestMessage(HttpMethod.Post, "https://localhost:8443/realms/OnlineTicketSalesRealm/protocol/openid-connect/token/introspect");
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes("OnlineTicketSalesBff:nluXYrk1ECM08fYYq9HOY1TBPPUaGXME"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            introspectionRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "token", accessToken }
            });
            var response = await httpClient.SendAsync(introspectionRequest);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }
            var content = await response.Content.ReadAsStringAsync();
            var tokenInfo = System.Text.Json.JsonDocument.Parse(content);
            return tokenInfo.RootElement.GetProperty("active").GetBoolean();

        }
    }
}
