using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Discord.Rest;
using LurkbotV7.Managers;
using System.Reflection;
using LurkbotV7.Attributes;
using Microsoft.Extensions.DependencyInjection;
using LurkbotV7.Modules;
using Microsoft.Extensions.DependencyInjection.Extensions;
using LurkbotV7.Config;

namespace LurkbotV7;

public class Program
{
    public const string Version = "7.0.0";

    static BotConfig _config;

    public const string ConfigPath = "./config.yml";

    public static DiscordSocketClient _client;

    public static InteractionService _interaction;

    public static IServiceProvider _services;

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
        Directory.CreateDirectory(ConfigurationManager.CONFIGPATH);
        Log.Info(@$"

  mmmm    m                    m      ""                         m                    #      mmmmm           m
 #""   "" mm#mm   mmm    m mm  mm#mm  mmm    m mm    mmmm         #      m   m   m mm  #   m  #    #  mmm   mm#mm
 ""#mmm    #    ""   #   #""  ""   #      #    #""  #  #"" ""#         #      #   #   #""  "" # m""   #mmmm"" #"" ""#    #
     ""#   #    m""""""#   #       #      #    #   #  #   #         #      #   #   #     #""#    #    # #   #    #
 ""mmm#""   ""mm  ""mm""#   #       ""mm  mm#mm  #   #  ""#m""#         #mmmmm ""mm""#   #     #  ""m  #mmmm"" ""#m#""    ""mm
                                                   m  #
                                                    """"
Version: {Version}
");
        ConfigurationManager.Init();
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
        _services = ConfigureServices();
        _interaction = new InteractionService(_client, interactionServiceConfig);
        _client.Log += ClientLog;
        _client.Ready += ClientReady;
        _client.SlashCommandExecuted += async (x) =>
        {
            Log.Debug($"Command execute. Command: {x.CommandName}");
            var ctx = new SocketInteractionContext(_client, x);
            await _interaction.ExecuteCommandAsync(ctx, _services);
        };
        await _client.StartAsync();
        await _client.LoginAsync(TokenType.Bot, Config.BotToken);
        await Task.Delay(-1);
    }

    private static IServiceProvider ConfigureServices()
    {
        var map = new ServiceCollection();

        //Cheap hack to avoid some retards service checker for fucking properties. 
        //If you have a fucking attribute to ignore the check, USE IT YOU FUCKING DUMBASS!
        //NEVER, FUCKING NEVER TOUCH C# AGAIN!
        foreach (Type t in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(ModuleConfiguration))))
        {
            Log.Info($"Add Dependency: {t.Name}");
            map.AddSingleton(t);
        }

        return map.BuildServiceProvider();
    }


    private async Task ClientReady()
    {
        //foreach (var cmd in await _client.GetGlobalApplicationCommandsAsync())
        //{
        //    Log.Info($"Delete: {cmd.Name}");
        //    await cmd.DeleteAsync();
        //}
        try
        {
            List<Type> targets = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetCustomAttribute(typeof(ModuleAttribute)) != null).ToList();
            foreach (Type t in targets)
            {
                Log.Info($"Register Module: {t.Name}");
                await _interaction.AddModuleAsync(t, _services);
            }
            await _interaction.RegisterCommandsGloballyAsync();
        }
        catch(Exception ex)
        {
            Log.Error(ex.ToString()); 
        }
    }

    private Task ClientLog(LogMessage arg)
    {
        Log.Message((LogLevel)arg.Severity, arg.Message);
        return Task.CompletedTask;
    }
}