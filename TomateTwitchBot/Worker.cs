using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TomateTwitchBot.Data;
using TomateTwitchBot.Data.Models;
using TomateTwitchBot.Twitch;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchSimpleLib.Chat.Messages;

namespace TomateTwitchBot;

public partial class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;
    private readonly ChatBot _chatBot;
    private readonly GreatApi _greatApi;
    private readonly IDbContextFactory<MyContext> _factory;
    private readonly TargetConfig _config;
    private readonly TwitchChatConfig _chatConfig;

    private readonly TimeoutStorage _timeout = new();
    private readonly ChatCache _cache = new();

    private readonly Regex _nickRegex = MyRegex();

    public Worker(ChatBot chatBot, GreatApi greatApi, IDbContextFactory<MyContext> factory,
        IOptions<TargetConfig> options,
        IOptions<TwitchChatConfig> optionsChat, ILogger<Worker> logger)
    {
        _logger = logger;
        _chatBot = chatBot;
        _greatApi = greatApi;
        _factory = factory;
        _config = options.Value;
        _chatConfig = optionsChat.Value;

        chatBot.Channel.PrivateMessageReceived += ChannelOnPrivateMessageReceived;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1));
                }
                catch
                {
                    return;
                }

                _timeout.Clear();
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void ChannelOnPrivateMessageReceived(object? sender, TwitchPrivateMessage e)
    {
        _cache.Add(e.username, e.displayName, e.userId);

        if (e.customRewardId != _config.RewardId)
            return;

        _logger.LogDebug("{who}: {text}", e.username, e.text);

        string? userProvidedVictimName = ResolveName(e.text);

        Task.Run(async () =>
        {
            try
            {
                if (userProvidedVictimName == null)
                {
                    await HandleNoNameAsync(e);
                    return;
                }

                ChatCache.Info? info = _cache.Resolve(userProvidedVictimName);

                string? victimId;
                string? victimUsername;
                string? victimFancyName;

                TwitchAPI? api = null;
                if (info != null)
                {
                    victimId = info.Id;
                    victimUsername = info.Username;
                    victimFancyName = info.DisplayName ?? info.Username;
                }
                else
                {
                    api = await _greatApi.GetApiAsync();

                    GetUsersResponse userResponse =
                        await api.Helix.Users.GetUsersAsync(logins: [userProvidedVictimName]);

                    if (userResponse.Users.Length == 0)
                    {
                        await HandleNoNameAsync(e);
                        return;
                    }

                    victimId = userResponse.Users[0].Id;
                    victimUsername = userResponse.Users[0].Login;
                    victimFancyName = userResponse.Users[0].DisplayName ?? userResponse.Users[0].Login;
                }

                api ??= await _greatApi.GetApiAsync();

                (int answer, double roll) = Roll();

                int time = (_timeout.GetExistingTime(victimId) ?? 0) + answer;

                BanUserResponse banResponse;
                try
                {
                    banResponse = await api.Helix.Moderation.BanUserAsync(_config.Id, _chatConfig.Id,
                        new BanUserRequest()
                        {
                            UserId = victimId,
                            Duration = time,
                            Reason = $"Не повезло, не повезло. {e.displayName ?? e.username}"
                        });
                }
                catch (BadRequestException)
                {
                    await HandleBadBanAsync(e, answer, roll);
                    return;
                }

                if (banResponse.Data.Length == 0)
                {
                    await HandleBadBanAsync(e, answer, roll);
                    return;
                }

                _timeout.AddTime(victimId, answer);

                await HandleKillAsync(e, victimFancyName, answer, time, roll);

                await WriteToDbAsync(e.text, e.userId, e.username, victimId, victimUsername, roll);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при попытке выполнить команду.");

                try
                {
                    await HandleUnhandled(e);
                }
                catch
                {
                }
            }
        });
    }

    private async Task WriteToDbAsync(string text, string killerId, string killerUsername, string victimId,
        string? victimUsername,
        double roll)
    {
        await using MyContext context = await _factory.CreateDbContextAsync();

        // Неуклюже много запросов, но мне пока впадлу думать.

        int? killerDbId = await context.Users.Where(u => u.TwitchId == killerId).Select(u => (int?)u.Id)
            .FirstOrDefaultAsync();
        int? victimDbId = await context.Users.Where(u => u.TwitchId == victimId).Select(u => (int?)u.Id)
            .FirstOrDefaultAsync();

        if (killerDbId == null)
        {
            UserDb killerUserDb = new()
            {
                TwitchId = killerId,
                LastSeenUsername = killerUsername
            };
            context.Users.Add(killerUserDb);
            await context.SaveChangesAsync();
            killerDbId = killerUserDb.Id;
        }

        if (victimDbId == null)
        {
            UserDb victimUserDb = new()
            {
                TwitchId = victimId,
                LastSeenUsername = victimUsername
            };
            context.Users.Add(victimUserDb);
            await context.SaveChangesAsync();
            victimDbId = victimUserDb.Id;
        }

        TimeoutDb timeoutDb = new(text, roll, killerDbId.Value, victimDbId.Value, DateTimeOffset.UtcNow);
        context.Timeouts.Add(timeoutDb);
        await context.SaveChangesAsync();
    }

    private Task HandleKillAsync(TwitchPrivateMessage e, string target, int answer, int totalTime, double roll)
    {
        string comment = roll switch
        {
            < 0.01 => $"Ой, дружище... @{target} посидит, но немного.",
            < 0.2 => $"Типа кинул таймач в @{target}, но тут не уверен.",
            < 0.5 => $"Отправил в таймаут @{target}.",
            < 0.8 => $"Зарядил таймаут в @{target}.",
            _ => $"В ГОЛОВУ @{target}"
        };

        if (answer == totalTime)
        {
            return _chatBot.Channel.SendMessageAsync(
                $"{comment} +{answer} ({(int)Math.Round(roll * 100)}%)", e.id);
        }

        return _chatBot.Channel.SendMessageAsync(
            $"{comment} +{answer}={totalTime} ({(int)Math.Round(roll * 100)}%)", e.id);
    }

    private Task HandleBadBanAsync(TwitchPrivateMessage e, int answer, double roll)
    {
        return _chatBot.Channel.SendMessageAsync(
            $"Не на того ты батон крошишь. {answer} ({(int)Math.Round(roll * 100)}%)", e.id);
    }

    private Task HandleNoNameAsync(TwitchPrivateMessage e)
    {
        (int answer, double roll) = Roll();

        return _chatBot.Channel.SendMessageAsync(
            $"Дэээээ, дружище. Бывает. Но ты наролял {answer} ({(int)Math.Round(roll * 100)}%)", e.id);
    }

    private Task HandleUnhandled(TwitchPrivateMessage e)
    {
        return _chatBot.Channel.SendMessageAsync(
            $"Всё капитально сломалось, увырге.", e.id);
    }

    private string? ResolveName(string text)
    {
        string[] split = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (split.Length == 0)
            return null;

        if (split.Length == 1)
            return ClearName(split[0]);

        string? result = split.FirstOrDefault(s => s.StartsWith('@'));
        if (result != null)
            return ClearName(result);

        return (from nick in split
            select _nickRegex.Match(nick)
            into match
            where match.Success
            select ClearName(match.Groups["name"].Value)).FirstOrDefault();
    }

    private static string ClearName(string name)
    {
        return name.TrimStart('@').TrimEnd(',').TrimEnd(':');
    }

    private (int answer, double roll) Roll()
    {
        double roll = Random.Shared.NextDouble();

        int question = _config.MaxTimeoutTime - _config.MinTimeoutTime;

        int answer = (int)Math.Round(question * roll);

        return (answer, roll);
    }

    [GeneratedRegex(@"(^|\s)(?<name>[a-zA-Z0-9]+[a-zA-Z0-9_]*)", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}