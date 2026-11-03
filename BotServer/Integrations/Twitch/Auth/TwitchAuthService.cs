using BotServer.Integrations.Twitch.Auth.Models;
using BotServer.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using TwitchLib.Api;
using TwitchLib.Api.Auth;

namespace BotServer.Integrations.Twitch.Auth
{
    /// <summary>
    /// Servicio especializado en la gestión de autenticación, tokens y OAuth para Twitch.
    /// </summary>
    public class TwitchAuthService
    {
        private readonly TwitchAPI _api;
        private readonly IServiceScopeFactory _service;
        private string redirectUri = "https://botserver-qccm.onrender.com/confirm";

        public TwitchAuthService(TwitchAPI api, IServiceScopeFactory service)
        {
            _api = api;
            _service = service;
        }

        public string GenerateScopesUrl()
        {
            List<string> scopes = new()
            {
                "chat:edit",
                "moderator:manage:banned_users",
                "chat:read",
                "channel:manage:vips",
                "channel:manage:moderators",
                "channel:manage:polls",
                "moderator:manage:shoutouts",
                "user:manage:whispers",
                "clips:edit",
                "channel:manage:broadcast",
                "moderator:manage:chat_messages",
                "channel:read:subscriptions",
                "moderator:read:followers",
                "user:bot",
                "user:read:chat",
                "user:write:chat"
            };

            return string.Join("%20", scopes);
        }

        public async Task<UserToken?> GetTokenWithCode(string code)
        {
            AuthCodeResponse token = await _api.Auth.GetAccessTokenFromCodeAsync(
                code, 
                clientId: _api.Settings.ClientId, 
                clientSecret: _api.Settings.Secret, 
                redirectUri: redirectUri
            );

            if (token is not null)
            {
                return new UserToken { AccessToken = token.AccessToken, RefreshToken = token.RefreshToken };
            }
            return null;
        }

        public async Task<ValidateAccessTokenResponse?> ValidateToken(string token)
        {
            var isValid = await _api.Auth.ValidateAccessTokenAsync(accessToken: token);
            return isValid;
        }

        public async Task<string> GetAppToken()
        {
            var client = new TwitchAPI();
            client.Settings.ClientId = _api.Settings.ClientId;
            client.Settings.Secret = _api.Settings.Secret;

            var appToken = await client.Auth.GetAccessTokenAsync();
            return appToken;
        }

        public async Task<UserToken> RefreshToken(string refreshToken)
        {
            RefreshResponse token = await _api.Auth.RefreshAuthTokenAsync(
                refreshToken, 
                _api.Settings.Secret, 
                _api.Settings.ClientId
            );

            return new UserToken { AccessToken = token.AccessToken, RefreshToken = token.RefreshToken };
        }

        public async Task<UserToken> GetValidToken(UserToken token)
        {
            var isValid = await ValidateToken(token.AccessToken);

            if (isValid == null)
            {
                var refreshedToken = await RefreshToken(token.RefreshToken);

                token.AccessToken = refreshedToken.AccessToken;
                token.RefreshToken = refreshedToken.RefreshToken;

                if (!string.IsNullOrEmpty(token.Username))
                {
                    using var scope = _service.CreateScope();
                    var usersService = scope.ServiceProvider.GetRequiredService<UsersService>();
                    await usersService.UpdateUser(token);
                }

                isValid = await ValidateToken(token.AccessToken);
            }

            if (isValid == null)
            {
                throw new Exception("El token no es válido y no se pudo renovar.");
            }

            _api.Settings.AccessToken = token.AccessToken;
            token.Username = isValid.Login;
            token.UserId = isValid.UserId;

            return token;
        }

        public async Task<UserToken> GetValidToken(string username)
        {
            using var scope = _service.CreateScope();
            var tokenService = scope.ServiceProvider.GetRequiredService<TokensService>();
            var token = await tokenService.GetAccessToken(username);

            if (token == null)
                throw new Exception($"No existe un token para el usuario '{username}'.");

            var isValid = await ValidateToken(token.AccessToken);

            if (isValid == null)
            {
                var refreshedToken = await RefreshToken(token.RefreshToken);

                token.AccessToken = refreshedToken.AccessToken;
                token.RefreshToken = refreshedToken.RefreshToken;

                await tokenService.SaveData();

                isValid = await ValidateToken(token.AccessToken);
            }

            _api.Settings.AccessToken = token.AccessToken;
            token.Username = isValid.Login;
            token.UserId = isValid.UserId;

            return token;
        }

        public async Task GetAuthorizationUrl(bool local = false)
        {
            var scopes = GenerateScopesUrl();
            if (local)
            {
                redirectUri = "http://localhost:8000/confirm";
            }
            Console.WriteLine($"Auth URL : https://id.twitch.tv/oauth2/authorize?client_id={_api.Settings.ClientId}&redirect_uri={redirectUri}&scope={scopes}&response_type=code&force_verify=true");
            await Task.Delay(8000);
        }
    }
}
