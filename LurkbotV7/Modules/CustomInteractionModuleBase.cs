using Discord;
using Discord.Commands;
using Discord.Interactions;
using LurkbotV7.Config;
using LurkbotV7.Managers;

namespace LurkbotV7.Modules
{
    public class CustomInteractionModuleBase<T> : InteractionModuleBase<SocketInteractionContext> where T : ModuleConfiguration
    {
        protected virtual Task RespondWithExceptionAsync(Exception ex, bool ephemeral = false)
        {
            EmbedBuilder builder = new();
            builder.WithColor(Color.Red);
            builder.WithCurrentTimestamp();
            builder.WithTitle("An Exception Occured");
            builder.WithDescription($"```{ex.ToString()}```");
            RespondAsync(embed: builder.Build(), ephemeral: ephemeral);
            return Task.CompletedTask;
        }

        protected virtual Task RespondWithErrorAsync(string error, string title = "Error", bool ephemeral = false)
        {
            EmbedBuilder builder = new();
            builder.WithColor(Color.Red);
            builder.WithCurrentTimestamp();
            builder.WithTitle(title);
            if (!string.IsNullOrEmpty(error))
            {
                builder.WithDescription($"`{error}`");
            }
            RespondAsync(embed: builder.Build(), ephemeral: ephemeral);
            return Task.CompletedTask;
        }

        protected virtual Task RespondWithSuccesAsync(string success, string title = "Success", bool ephemeral = false)
        {
            EmbedBuilder builder = new();
            builder.WithColor(Color.Green);
            builder.WithCurrentTimestamp();
            builder.WithTitle(title);
            if (!string.IsNullOrEmpty(success))
            {
                builder.WithDescription($"`{success}`");
            }
            RespondAsync(embed: builder.Build(), ephemeral: ephemeral);
            return Task.CompletedTask;
        }

        public CustomInteractionModuleBase()
        {
            _config = ConfigurationManager.GetConfiguration<T>();
            ConfigUpdated += CustomInteractionModuleBase_ConfigUpdated;
        }

        private void CustomInteractionModuleBase_ConfigUpdated(T obj)
        {
            _config = obj;
        }

        ~CustomInteractionModuleBase()
        {
            ConfigUpdated -= CustomInteractionModuleBase_ConfigUpdated;
        }

        private event Action<T> ConfigUpdated;

        private object _lock = new();

        private T _config = default;

        [DontInject]
        public T Config
        {
            get
            {
                if (_config == default(T))
                {
                    _config = ConfigurationManager.GetConfiguration<T>();
                }
                return _config;
            }
            protected set
            {
                _config = value;
                SaveConfig();
                ConfigUpdated?.Invoke(value);
            }
        }

        public void SaveConfig()
        {
            ConfigurationManager.SaveConfiguration(_config);
            _config = ConfigurationManager.GetConfiguration<T>();
        }

        public void SaveConfig(T config)
        {
            Config = config;
        }
    }
}
