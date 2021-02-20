using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TwitchBot
{
    public class MyApplication
    {
        private IHttpClientFactory _httpFactory { get; set; }
        public MyApplication(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        public async Task<string> Run(string url, string apiKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                url);

            request.Headers.Add("X-Riot-Token", apiKey);

            var client = _httpFactory.CreateClient();
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return $"StatusCode: {response.StatusCode}";
            }
        }
    }
}
