using Discord;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LurkbotV7.Managers;
public static class SLManager
{
    public static event Action ListRefreshed;
    public static event Action<List<Player>> PlayersJoinedEvent;
    public static event Action<List<Player>> PlayersLeftEvent;

    private static List<Player> Players = new();
    private static List<Player> OldPlayers = new();
    public static List<Player> GetOldPlayers() { return OldPlayers; }
    private static List<Player> LeftPlayers = new();
    public static List<Player> GetLeftPlayers() { return LeftPlayers; }
    private static List<Player> JoinedPlayers = new();
    public static List<Player> GetJoinedPlayers() { return JoinedPlayers; }
    private static List<string> LeftPlayersNames = new();
    public static List<string> GetLeftPlayersNames() { return LeftPlayersNames; }
    private static List<string> JoinedPlayersNames = new();
    public static List<string> GetJoinedPlayersNames() { return JoinedPlayersNames; }
    private static Dictionary<string, Player> NameToObject = new();
    public static Dictionary<string, Player> GetNameToObject() { return NameToObject; }
    private static Dictionary<string, Player> IDToObject = new();
    public static Dictionary<string, Player> GetIDToObject() { return IDToObject; }

    private static Response Response;

    private static int totalPlayerCount = 0;
    public static Player[] allPlayers = new Player[0];
    public static RestClient client = new RestClient();
    public static int cooldown = 0;

    public static Response GetResponse()
    {
        return Response;
    }

    public static int GetPlayerAmount()
    {
        return totalPlayerCount;
    }

    public static List<Player> GetPlayers()
    {
        return Players;
    }

    public static Player[] GetAllPlayerNames()
    {
        return allPlayers;
    }

    public static Embed[] GetEmbeds()
    {
        List<Embed> embeds = new List<Embed>();
        foreach(Server server in Response.Servers)
        {
            EmbedBuilder builder = new EmbedBuilder();
            string name = "No Name";
            if(Program.Config.IDToName.ContainsKey(server.ID))
            {
                name = Program.Config.IDToName[server.ID];
            }
            builder.WithCurrentTimestamp();
            builder.WithTitle(name);
            if(server.Online)
            {
                builder.WithColor(Color.Green);
                builder.AddField("Playercount", server.PlayersList.Length);
                string desc = "```";
                desc += string.Join(",\n", server.PlayersList.Select(x => x.Nickname));
                desc = desc.Trim();
                desc += "```";
                builder.WithDescription(desc);
            }
            else
            {
                builder.WithColor(Color.Red);
                builder.WithDescription("Server is offline.");
            }
            embeds.Add(builder.Build());
        }
        return embeds.ToArray();
    }

    public static void PullData()
    {
        Log.Info("Pulling data...");
        Log.Info("Creating request...");
        var request = new RestRequest(Program.Config.APIUrl.Replace("{id}", Program.Config.AccountID.ToString()).Replace("{key}", Program.Config.APIKey), Method.Get);
        Log.Info("Fetching...");
        var resp = client.ExecuteAsync(request);
        Log.Info("Deserializing " + resp.Result.Content);
        var data = JsonConvert.DeserializeObject<Response>(resp.Result.Content);
        if (!data.Success)
        {
            Log.Fatal("Unable to fetch totalPlayerCount: " + data.Error);
            cooldown = Program.Config.RefreshMaxCooldown;
            Response.Success = false;
            Response.Error = data.Error;
            return;
        }
        else
        {
            Response = data;
        }
        if (Program.Config.RefreshCooldown < data.Cooldown)
        {
            Log.Warning("Requested Refresh Cooldown is too fast! Specify a higher value.");
            cooldown = Math.Max(data.Cooldown + 1, Program.Config.RefreshMinCooldown);
        }
        else
        {
            cooldown = Program.Config.RefreshCooldown;
            Log.Info("Setting Requested Cooldown to: " + Program.Config.RefreshCooldown + "; Cooldown Used: " + cooldown);
        }

        totalPlayerCount = data.Servers.Sum(x => x.PlayersList.Length);
        allPlayers = data.Servers.Select(x => x.PlayersList).SelectMany(x => x).ToArray();

        Log.Info($"Playercount: {totalPlayerCount}; (Player Details Omitted for brevity) Cooldown: {cooldown}");
        OldPlayers.Clear();
        NameToObject.Clear();
        IDToObject.Clear();
        Log.Debug("Cleared old lists....");
        foreach (Player p in Players)
        {
            Log.Debug("Configuring player: " + p.Nickname + " or " + p.ID);

            if (OldPlayers.Contains(p))
            {
                Log.Error("The player object is already part of the list. List: OldPlayers");
            }
            OldPlayers.Add(p);
            if (string.IsNullOrEmpty(p.Nickname))
            {
                Log.Warning("Name is null or empty! ID: " + p.ID);
                //p.Nickname = "[Blank Name]";
                Player player = p;
                player.Nickname = "[Blank Name]";
                List<Player> list = Players.ToList();
                list.Remove(p);
                list.Add(player);
                Players = list;
                NameToObject.Add("[Blank Name]", player);
                continue;
            }
            if (NameToObject.ContainsKey(p.Nickname))
            {
                int counter = 0;
                while (true)
                {
                    if (NameToObject.ContainsKey(p.Nickname + " (" + counter.ToString() + ")"))
                    {
                        counter++;
                    }
                    else
                    {
                        if (counter == 0)
                        {
                            NameToObject.Add(p.Nickname, p);
                            break;
                        }
                        else
                        {
                            NameToObject.Add(p.Nickname + " (" + counter.ToString() + ")", p);
                            break;
                        }
                    }
                }
            }
            if (IDToObject.ContainsKey(p.ID))
            {
                Log.Error("The ID already exists.");
            }
            IDToObject.Add(p.ID, p);
        }
        Players = data.Servers.Select(x => x.PlayersList).SelectMany(x => x).ToList();
        Response = data;
        Log.Info("List Refresh complete, called event");
        ListRefreshed?.Invoke();
        return;
    }

    public static void PlayerChangeDetector()
    {
        Log.Debug("Still alive: PlayerChangeDetector");
        LeftPlayers.Clear();
        LeftPlayersNames.Clear();
        JoinedPlayers.Clear();
        JoinedPlayersNames.Clear();
        List<Player> PlayersWhoDidntLeave = new();
        foreach (Player p in OldPlayers)
        {
            if (!Players.Contains(p))
            {
                LeftPlayers.Add(p);
                LeftPlayersNames.Add(p.Nickname);
            }
            else
            {
                //these allPlayers have not left
                PlayersWhoDidntLeave.Add(p);
            }
        }
        Log.Debug("First foreach in playerchange detector done");
        foreach (Player p in Players)
        {
            if (PlayersWhoDidntLeave.Contains(p))
            {
                continue;
            }
            else
            {
                JoinedPlayers.Add(p);
                JoinedPlayersNames.Add(p.Nickname);
            }
        }
        Log.Debug("second foreach in playerchange detector done, invoking");
        PlayersJoinedEvent?.Invoke(JoinedPlayers);
        PlayersLeftEvent?.Invoke(LeftPlayers);
        Log.Info("Players who left since last check: " + string.Join(", ", LeftPlayersNames));
        Log.Info("Players who joined since last check: " + string.Join(", ", JoinedPlayersNames));
        return;
    }
    public static void Init()
    {
        ListRefreshed += PlayerChangeDetector;
        Thread thread = new Thread(UpdateThread);
        Runner = thread;
        thread.Start();
    }

    public static Thread Runner { get; set; } = null;

    public static void UpdateThread()
    {
        while (true)
        {
            PullData();
            Log.Info($"Waiting {cooldown} seconds");
            Thread.Sleep(cooldown * 1000);
        }
    }
}
//un-nested the structs so you can access them
public struct Response
{
    public string Error;
    public bool Success;
    public int Cooldown;
    public Server[] Servers;
}

public struct Server
{
    public int ID;
    public int Port;
    public bool Online;
    public Player[] PlayersList;
}

public struct Player
{
    public string ID;
    public string Nickname;
    public override string ToString()
    {
        if (ID == null || Nickname == null)
        {
            return null;
        }
        return "Nickname: " + Nickname + "; ID: " + ID;
    }
}
