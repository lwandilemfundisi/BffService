using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace BffService.Api.Helpers
{
    public static class CookiesHepler
    {
        public static async Task<bool> IsTokenActive(string accessToken)
        {
            var introspectionClient = new HttpClient();
            var introspectionRequest = new HttpRequestMessage(HttpMethod.Post, "https://localhost:8443/realms/OnlineTicketSalesRealm/protocol/openid-connect/token/introspect");
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes("OnlineTicketSalesBff:nluXYrk1ECM08fYYq9HOY1TBPPUaGXME"));
            introspectionClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            introspectionRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "token", accessToken }
            });
            var response = await introspectionClient.SendAsync(introspectionRequest);
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
