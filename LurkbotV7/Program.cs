﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LurkbotV7.Attributes;
using LurkbotV7.Config;
using LurkbotV7.Managers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LurkbotV7;

public class Program
{
    public const string Version = "7.0.0";

    static BotConfig _config;

    public static string ConfigPath
    {
        get
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "config.yml");
        }
    }

    public static DiscordSocketClient Client;

    public static InteractionService InteractionServices;

    public static IServiceProvider Services;

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
        ISerializer builder = new SerializerBuilder()
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
        IDeserializer builder = new DeserializerBuilder()
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
        BotConfig config = builder.Deserialize<BotConfig>(text);
        if (config == null)
        {
            Log.Error("Failure to load config: config is null.");
            Config = new BotConfig();
        }
        else
        {
            Config = config;
        }
        SaveConfig();
    }

    public static async Task Main(string[] args)
    {
        await new Program().MainAsync(args);
    }

    public static bool DestoryCommands = false;

    public async Task MainAsync(string[] args)
    {
        Config = new BotConfig();
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), Config.LogPath));
        LoadConfig();
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
        if (args.Contains("--delete-commands"))
        {
            DestoryCommands = true;
        }
        ConfigurationManager.Init();
        DiscordSocketConfig config = new()
        {
            GatewayIntents = GatewayIntents.All,
            MessageCacheSize = 50,
#if DEBUG
            LogLevel = LogSeverity.Debug,
#else
            LogLevel = LogSeverity.Info,
#endif
        };
        Client = new(config);
        InteractionServiceConfig interactionServiceConfig = new()
        {
            //TODO, fill in.
        };
        Client.Log += ClientLog;
        Client.Ready += ClientReady;
        Services = ConfigureServices();
        InteractionServices = new InteractionService(Client, interactionServiceConfig);
        Client.SlashCommandExecuted += async (x) =>
        {
            SocketInteractionContext ctx = new(Client, x);
            await InvokeCommand(ctx, Services);
        };
        Client.MessageCommandExecuted += async (x) =>
        {
            SocketInteractionContext ctx = new(Client, x);
            await InvokeCommand(ctx, Services);
        };
        Client.ModalSubmitted += async (x) =>
        {
            SocketInteractionContext ctx = new(Client, x);
            await InvokeCommand(ctx, Services);
        };
        await Client.StartAsync();
        await Client.LoginAsync(TokenType.Bot, Config.BotToken);
        await Task.Delay(-1);
    }

    private async Task InvokeCommand(SocketInteractionContext context, IServiceProvider serviceProvider)
    {
        try
        {
            Discord.Interactions.IResult result = await InteractionServices.ExecuteCommandAsync(context, Services);
            if (!result.IsSuccess)
            {
                Log.Error("Error in command!\n" + result.ErrorReason);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        ServiceCollection map = new();

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
        SLManager.Init();
        if (DestoryCommands)
        {
            Log.Info("Delete Commands!");
            foreach (SocketApplicationCommand cmd in await Client.GetGlobalApplicationCommandsAsync())
            {
                Log.Info($"Delete Global: {cmd.Name}");
                await cmd.DeleteAsync();
            }
            foreach (SocketApplicationCommand cmd in Client.Guilds.Select(x => x.GetApplicationCommandsAsync().Result).SelectMany(x => x).ToList())
            {
                Log.Info($"Delete Application: {cmd.Name}, Guild: {cmd.Guild.Name}");
                await cmd.DeleteAsync();
            }
            Environment.Exit(0);
        }
        try
        {
            List<Type> targets = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetCustomAttribute(typeof(ModuleAttribute)) != null).ToList();
            foreach (Type t in targets)
            {
                Log.Info($"Register Module: {t.Name}");
                await InteractionServices.AddModuleAsync(t, Services);
            }
            await InteractionServices.RegisterCommandsGloballyAsync();
        }
        catch (Exception ex)
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