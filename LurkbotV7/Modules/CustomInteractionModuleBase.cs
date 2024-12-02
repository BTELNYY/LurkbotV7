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
        protected virtual async void RespondWithExceptionAsync(Exception ex)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(Color.Red);
            builder.WithCurrentTimestamp();
            builder.WithTitle("An Exception Occured");
            builder.WithDescription($"```{ex.ToString()}```");
            await RespondAsync(embed: builder.Build());
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
