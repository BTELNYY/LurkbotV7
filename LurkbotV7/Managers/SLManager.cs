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
    public static event EventHandler ListRefreshed;
    public static event EventHandler<Player[]> PlayersJoinedEvent;
    public static event EventHandler<Player[]> PlayersLeftEvent;

    private static Player[] Players = Array.Empty<Player>();
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

    private static int playercount = 0;
    public static string[] players = Array.Empty<string>();
    public static RestClient client = new RestClient();
    public static int cooldown = 0;

    public static Response GetResponse()
    {
        return Response;
    }

    public static Server GetServerResponse()
    {
        foreach (Server s in Response.Servers)
        {
            if (s.ID == int.Parse(Program.Config.AccountID))
            {
                return s;
            }
            else
            {
                Log.Warning("Returning Server Response for server that is not the one listed in config.");
                return Response.Servers.FirstOrDefault();
            }
        }
        return Response.Servers.FirstOrDefault();
    }

    public static int GetPlayerAmount()
    {
        return playercount;
    }

    public static Player[] GetPlayers()
    {
        return Players;
    }

    public static string[] GetPlayerNames()
    {
        return players;
    }

    public static async Task PullData()
    {
        Log.Info("Pulling data...");
        Log.Info("Creating request...");
        var request = new RestRequest(Program.Config.APIUrl, Method.Get);

        Log.Info("Fetching...");
        var resp = await client.ExecuteAsync(request);
        Log.Info("Deserializing " + resp.Content);
        var data = JsonConvert.DeserializeObject<Response>(resp.Content);
        //fix issue with error thrown cuz playercount is null when rate limit it exceeded
        if (!data.Success)
        {
            Log.Fatal("Unable to fetch playercount: " + data.Error);
            cooldown = Program.Config.RefreshMaxCooldown;
            Response.Success = false;
            Response.Error = data.Error;
            return;
        }
        else
        {
            Response = data;
        }
        playercount = data.Servers[0].PlayersList.Length;
        //if (playercount > Statistics.Statistics.CurrentServerStats.MaxPlayersEver)
        //{
        //    Statistics.Statistics.CurrentServerStats.MaxPlayersEver = playercount;
        //}
        //if (data.Servers[0].Online == false)
        //{
        //    Statistics.Statistics.CurrentServerStats.Uptime = 0;
        //}
        players = data.Servers[0].PlayersList.Select(player => player.Nickname).ToArray();
        if (Program.Config.RefreshCooldown< data.Cooldown)
        {
            Log.Warning("Requested Refresh Cooldown is too fast! Specify a higher value.");
            cooldown = Math.Max(data.Cooldown + 1, Program.Config.RefreshMinCooldown);
        }
        else
        {
            cooldown = Program.Config.RefreshCooldown;
            Log.Info("Setting Requested Cooldown to: " + Program.Config.RefreshCooldown+ "; Cooldown Used: " + cooldown);
        }
        Log.Info($"Playercount: {playercount}; (Player Details Omitted for brevity) Cooldown: {cooldown}");
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
                Players = list.ToArray();
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
        Players = data.Servers[0].PlayersList;
        Response = data;
        //Statistics.Statistics.CurrentServerStats.Uptime += cooldown;
        Log.Info("List Refresh complete, called event");
        ListRefreshed.Invoke("", EventArgs.Empty);
        //DiscordHandler.SetStatusMessage("with " + playercount + " other players!");
        return;
    }

    public static void PlayerChangeDetector(object obj, EventArgs e)
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
                //these players have not left
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
        PlayersJoinedEvent.Invoke("", JoinedPlayers.ToArray());
        PlayersLeftEvent.Invoke("", LeftPlayers.ToArray());
        Log.Info("Players who left since last check: " + string.Join(", ", LeftPlayersNames));
        Log.Info("Players who joined since last check: " + string.Join(", ", JoinedPlayersNames));
        return;
    }
    public static void Init()
    {
        ListRefreshed += PlayerChangeDetector;
        Task.Run(() => StartTimer());
    }

    public static async Task StartTimer()
    {
        while (true)
        {
            await PullData();
            Log.Info($"Waiting {cooldown} seconds");
            await Task.Delay(cooldown * 1000);
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
