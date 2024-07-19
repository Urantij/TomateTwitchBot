using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using TwitchLib.Api;
using TwitchLib.Api.Auth;

namespace TomateTwitchBot.Twitch;

public class TwitchApiConfig
{
    [Required] public required string ClientId { get; init; }
    [Required] public required string Secret { get; init; }
    [Required] public required string RefreshToken { get; init; }
}

public class GreatApi
{
    private readonly TwitchAPI _api;

    private readonly string _refreshToken;

    public GreatApi(IOptions<TwitchApiConfig> options, ILoggerFactory loggerFactory)
    {
        _api = new TwitchAPI(loggerFactory);
        _api.Settings.ClientId = options.Value.ClientId;
        _api.Settings.Secret = options.Value.Secret;

        _refreshToken = options.Value.RefreshToken;
    }

    public async Task<TwitchAPI> GetApiAsync()
    {
        RefreshResponse auth = await _api.Auth.RefreshAuthTokenAsync(_refreshToken, _api.Settings.Secret);

        _api.Settings.AccessToken = auth.AccessToken;

        return _api;
    }
}