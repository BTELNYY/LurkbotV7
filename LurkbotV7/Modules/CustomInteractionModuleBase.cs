using Discord;
using Discord.Commands;
using Discord.Interactions;
using LurkbotV7.Config;
using LurkbotV7.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LurkbotV7.Modules
{
    public class CustomInteractionModuleBase<T> : InteractionModuleBase<SocketInteractionContext> where T : ModuleConfiguration
    {
        protected virtual Task RespondWithExceptionAsync(Exception ex, bool hidden = false)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(Color.Red);
            builder.WithCurrentTimestamp();
            builder.WithTitle("An Exception Occured");
            builder.WithDescription($"```{ex.ToString()}```");
            RespondAsync(embed: builder.Build(), ephemeral: hidden);
            return Task.CompletedTask;
        }

        protected virtual Task RespondWithErrorAsync(string error, string title = "Error", bool hidden = false)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(Color.Red);
            builder.WithCurrentTimestamp();
            builder.WithTitle(title);
            if(!string.IsNullOrEmpty(error))
            {
                builder.WithDescription($"`{error}`");
            }
            RespondAsync(embed: builder.Build(), ephemeral: hidden);
            return Task.CompletedTask;
        }

        protected virtual Task RespondWithSuccesAsync(string success, string title = "Success", bool hidden = false)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(Color.Green);
            builder.WithCurrentTimestamp();
            builder.WithTitle(title);
            if (!string.IsNullOrEmpty(success))
            {
                builder.WithDescription($"`{success}`");
            }
            RespondAsync(embed: builder.Build(), ephemeral: hidden);
            return Task.CompletedTask;
        }

        public CustomInteractionModuleBase()
        {
            _config = ConfigurationManager.GetConfiguration<T>();
        }

        private T _config = default(T);

        [DontInject]
        public T Config
        {
            get
            {
                if(_config == default(T))
                {
                    _config = ConfigurationManager.GetConfiguration<T>();
                }
                return _config;
            }
            protected set
            {
                _config = value;
                SaveConfig();
            }
        }

        public void SaveConfig()
        {
            ConfigurationManager.SaveConfiguration(_config);
        }

        public void SaveConfig(T config)
        {
            Config = config;
        }
    }
}
