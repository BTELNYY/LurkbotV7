using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Discord.Rest;
using LurkbotV7.Managers;

namespace LurkbotV7;

public class Program
{
    public const string Version = "7.0.0";

    static BotConfig _config;

    public const string ConfigPath = "./config.yml";

    public static DiscordSocketClient _client;

    public static InteractionService _interaction;

    public static BotConfig Config
    {
        get
        {
            return _config;
        }
        set
        {
            _config = value;
        }
    }

    public static void SaveConfig()
    {
        var builder = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        string text = builder.Serialize(Config);
        if (File.Exists(ConfigPath))
        {
            File.Delete(ConfigPath);
        }
        File.WriteAllText(ConfigPath, text);
    }

    public static void LoadConfig()
    {
        var builder = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        if (!File.Exists(ConfigPath))
        {
            Log.Warning("Config File is missing, creating a new one.");
            Config = new BotConfig();
            SaveConfig();
            return;
        }
        string text = File.ReadAllText(ConfigPath);
        BotConfig config = (BotConfig)builder.Deserialize<BotConfig>(text);
        if(config == null)
        {
            Log.Error("Failure to load config: config is null.");
            Config = new BotConfig();
            SaveConfig();
        }
        else
        {
            Config = config;
        }
    }

    public static async Task Main(string[] args)
    {
        await new Program().MainAsync(args);
    }

    public async Task MainAsync(string[] args)
    {
        Config = new BotConfig();
        Directory.CreateDirectory(Config.LogPath);
        LoadConfig();
        //Ensure the proper directory is loaded.
        Directory.CreateDirectory(Config.LogPath);
        Log.Info("Starting Lurkbot v" + Version);
        SLManager.Init();
        DiscordSocketConfig config = new()
        {
            GatewayIntents = GatewayIntents.All,
            MessageCacheSize = 50,
        };
        _client = new(config);
        InteractionServiceConfig interactionServiceConfig = new()
        { 
            //TODO, fill in.
        };
        _interaction = new InteractionService(_client, interactionServiceConfig);
        _client.Log += ClientLog;
        _client.Ready += ClientReady;
        await _client.LoginAsync(TokenType.Bot, Config.BotToken);
        await Task.Delay(-1);
    }

    private Task ClientReady()
    {

        return Task.CompletedTask;
    }

    private Task ClientLog(LogMessage arg)
    {
        Log.Message((LogLevel)arg.Severity, arg.Message);
        return Task.CompletedTask;
    }
}