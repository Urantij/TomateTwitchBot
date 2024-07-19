using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using TwitchSimpleLib.Chat;
using TwitchSimpleLib.Chat.Messages;

namespace TomateTwitchBot.Twitch;

public class TwitchChatConfig
{
    [Required] public required string Name { get; init; }
    [Required] public required string Id { get; init; }
    [Required] public required string Token { get; init; }
}

public class ChatBot : IHostedService
{
    private readonly ILogger<ChatBot> _logger;
    private readonly TwitchChatClient _client;
    public ChatAutoChannel Channel { get; init; }

    public ChatBot(IOptions<TwitchChatConfig> options, IOptions<TargetConfig> target, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ChatBot>();

        _client = new TwitchChatClient(true, new TwitchChatClientOpts()
        {
            Username = options.Value.Name,
            OauthToken = options.Value.Token
        }, loggerFactory);
        Channel = _client.AddAutoJoinChannel(target.Value.Name);

        _client.AuthFailed += ClientOnAuthFailed;
        _client.AuthFinished += ClientOnAuthFinished;
        _client.MessageProcessingException += ClientOnMessageProcessingException;
        _client.ConnectionClosed += ClientOnConnectionClosed;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _client.ConnectAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Close();

        return Task.CompletedTask;
    }

    private void ClientOnAuthFailed(object? sender, EventArgs e)
    {
        _logger.LogCritical("Не удалось аутентифицироваться!");
    }

    private void ClientOnAuthFinished(object? sender, TwitchGlobalUserStateMessage? e)
    {
        _logger.LogInformation("Прошли аутентификацию...");
    }

    private void ClientOnMessageProcessingException((Exception exception, string message) obj)
    {
        _logger.LogError(obj.exception, "Ошибка при обработке сообщения {message}", obj.message);
    }

    private void ClientOnConnectionClosed(Exception? obj)
    {
        _logger.LogInformation(obj, "Соединение закрыто.");
    }
}