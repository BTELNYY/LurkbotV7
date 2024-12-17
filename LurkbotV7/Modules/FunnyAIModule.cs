using Discord.Interactions;
using LurkbotV7.Attributes;
using Discord;
using LurkbotV7.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using KoboldSharp;
using System.ComponentModel;
using Discord.Rest;

namespace LurkbotV7.Modules
{
    [Module]
    public class FunnyAIModule : CustomInteractionModuleBase<FunnyAIModuleConfig>
    {
        public static KoboldClient AIClient { get; set; } = null;

        public override void OnModuleBuilding(InteractionService commandService, ModuleInfo module)
        {
            base.OnModuleBuilding(commandService, module);
            Program.Client.MessageReceived += Client_MessageReceived;
            if(AIClient == null)
            {
                AIClient = new KoboldClient(Config.Url);
            }
        }

        private async Task Client_MessageReceived(SocketMessage message)
        {
            try
            {
                if(message.Author.Id == Program.Client.CurrentUser.Id)
                {
                    return;
                }
                if (message.Channel is not SocketGuildChannel guildChannel)
                {
                    return;
                }
                if (message.Channel is not SocketTextChannel textChannel)
                {
                    return;
                }
                if (!_aiState)
                {
                    return;
                }
                if (Config.UserBlacklist.Contains(message.Author.Id))
                {
                    await textChannel.SendMessageAsync(text: "Access is Denied.", messageReference: new MessageReference(message.Id, guildChannel.Id, guildChannel.Guild.Id));
                    return;
                }
                _ = Task.Run(async () =>
                {
                    List<IMessage> messages = message.GetMessageTree();
                    if(messages.Count > 0)
                    {
                        if (!messages[0].MentionedUserIds.Contains(Program.Client.CurrentUser.Id))
                        {
                            return;
                        }
                    }
                    else
                    {
                        if(!message.MentionedUsers.Select(x => x.Id).Contains(Program.Client.CurrentUser.Id))
                        {
                            return;
                        }
                    }
                    IDisposable typing = textChannel.EnterTypingState();
                    string memoryGenerated = Config.Memory + "\n";
                    string magicString = $"@{Program.Client.CurrentUser.Username}#{Program.Client.CurrentUser.Discriminator}";
                    for(int i = 0; i < messages.Count; i++)
                    {
                        IMessage msg = messages[i];
                        string content = string.IsNullOrEmpty(msg.Content.StripMentions().Replace(magicString, "").Trim()) ? "[Blank Message]" : msg.Content.Replace(magicString, "").StripMentions().Trim();
                        string memline = $"{msg.Author.Username}: {content}\n";
                        memoryGenerated += memline;
                    }
                    //cheap hack
                    string newPrompt = string.IsNullOrEmpty(message.Content) ? "[Blank Message]" : $"{message.Author.Username}: " + message.Content.Replace(magicString, "").StripMentions().Trim();
                    //Log.Info($"Prompt: {newPrompt}\nMemory: ```{memoryGenerated}```\nMessage Tree: {messages.Count}");
                    GenParams genParams = new GenParams(prompt: newPrompt, memory: memoryGenerated = memoryGenerated.Replace("{botName}", Program.Client.CurrentUser.Username), maxLength: Config.MaxGenerationSize, maxContextLength: Config.MaxContextSize, temperature: Config.Temperature, stopSequence: messages.Select(x => x.Author.Username).Except(new List<string>() { Program.Client.CurrentUser.Username }).ToList());
                    ModelOutput output = await AIClient.Generate(genParams);
                    string sResult = string.Join('\n', output.Results.Select(x => x.Text));
                    if (string.IsNullOrWhiteSpace(sResult))
                    {
                        sResult = "[Blank Output]";
                    }
                    typing.Dispose();
                    await textChannel.SendMessageAsync(text: output.Results[0].Text.Replace($"{Program.Client.CurrentUser.Username}: ", ""), messageReference: new MessageReference(message.Id, guildChannel.Id, guildChannel.Guild.Id));
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }


        static bool _aiState = false;

        [SlashCommand("aisetstate", "Sets the AI state", runMode: RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetAIState(bool state)
        {
            if(_aiState == state)
            {
                await RespondAsync($"No change, state is already `{state}`", ephemeral: true);
                return;
            }
            _aiState = state;
            await RespondWithSuccesAsync("Set AI State to: " + state, ephemeral: true);
        }

        [SlashCommand("aisetblacklist", "Set the blacklist of a user", runMode: RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task BlacklistUser(SocketUser user, bool state = true)
        {
            if(Config.UserBlacklist.Contains(user.Id) && state)
            {
                await RespondWithErrorAsync("That user is already blacklisted.", ephemeral: true);
            }
            else if(Config.UserBlacklist.Contains(user.Id) && !state)
            {
                Config.UserBlacklist.Remove(user.Id);
                SaveConfig();
                await RespondWithSuccesAsync("User has been removed from the blacklist.", ephemeral: true);
            }
            else if(!Config.UserBlacklist.Contains(user.Id) && state)
            {
                Config.UserBlacklist.Add(user.Id);
                SaveConfig();
                await RespondWithSuccesAsync("User has been added to the blacklist.", ephemeral: true);
            }
            else
            {
                await RespondWithErrorAsync("That user is not blacklisted.", ephemeral: true);
            }
        }
    }

    public class FunnyAIModuleConfig : ModuleConfiguration
    {
        public override string FileName => "AIConfig";

        public string Url { get; set; } = "http://localhost:5001";

        public string Memory { get; set; } = "[You are {botName}.]";

        public int MaxGenerationSize { get; set; } = 100;

        public int MaxContextSize { get; set; } = 2048;

        public float Temperature { get; set; } = 0.7f;

        public List<ulong> UserBlacklist { get; set; } = new List<ulong>();
    }
}
