using Microsoft.EntityFrameworkCore;
using TomateTwitchBot.Data;
using TomateTwitchBot.Twitch;

namespace TomateTwitchBot;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.TimestampFormat = "[HH:mm:ss] ";
            options.UseUtcTimestamp = true;
        });

        builder.Services.AddDbContextFactory<MyContext>(c => c.UseSqlite("Data Source=./db.sqlite"));

        builder.Services.AddOptions<TwitchApiConfig>()
            .Bind(builder.Configuration.GetSection("TwitchApi"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<TwitchChatConfig>()
            .Bind(builder.Configuration.GetSection("TwitchChat"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<TargetConfig>()
            .Bind(builder.Configuration.GetSection("Target"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<GreatApi>();
        builder.Services.AddSingleton<ChatBot>();
        builder.Services.AddHostedService<ChatBot>(c => c.GetRequiredService<ChatBot>());

        builder.Services.AddSingleton<Worker>();
        builder.Services.AddHostedService<Worker>(c => c.GetRequiredService<Worker>());
        
        var host = builder.Build();

        using (IServiceScope scope = host.Services.CreateScope())
        {
            using var context = scope.ServiceProvider.GetRequiredService<MyContext>();
            context.Database.Migrate();
        }
        host.Run();
    }
}