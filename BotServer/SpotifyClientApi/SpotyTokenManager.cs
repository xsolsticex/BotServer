using SpotifyAPI.Web.Http;

namespace BotServer.SpotifyClientApi
{
    public class SpotyTokenManager
    {
        private HttpClient _client;

        public SpotyTokenManager(HttpClient client)
        {
            _client = client;

        }


        public async Task<string> GenerateUserToken(string auth_code)
        {
            var param = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = auth_code,
                ["redirect_uri"] = "http://127.0.0.1:8000/auth/spotify",
                ["content-type"] = "application/x-www-form-urlencoded"
            };

            using var content = new FormUrlEncodedContent(param);

            var response = await _client.PostAsync(_client.BaseAddress, content);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
