using CW.Bots.Nav;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace CW.Bots
{
    internal static class Commands
    {
        internal static void Bots(string[] args)
        {
            var dir = BotDirector.Instance;
            if (dir == null) { Refl.Print("bots: director not running"); return; }

            if (args.Length == 0 || args[0] == "status")
            {
                Refl.Print("bots: " + dir.AgentCount + " active, " + dir.Wanted + " wanted, skill "
                           + dir.SkillTier + " (" + Ai.Difficulty.NameOf(dir.SkillTier) + ")");
                Refl.Print("nav: " + (dir.Grid == null ? "none" : dir.Grid.Count + " nodes, " + dir.Grid.LinkCount + " links")
                           + "  bake: " + dir.BakeStatus);
                foreach (var a in dir.Agents)
                    Refl.Print("  g" + a.Group + " team=" + a.Team + " " + a.State);
                return;
            }

            if (args[0] == "off" || args[0] == "stop")
            {
                dir.Clear();
                Refl.Print("bots: cleared");
                return;
            }

            int n;
            if (!int.TryParse(args[0], out n)) { Refl.Print("usage: bots <count|off|status>"); return; }
            if (n < 0) n = 0;
            if (n > 30) n = 30;
            dir.Wanted = n;
            Refl.Print("bots: target " + n);
        }

        internal static void BotSkill(string[] args)
        {
            var dir = BotDirector.Instance;
            if (dir == null) { Refl.Print("botskill: director not running"); return; }

            if (args.Length == 0)
            {
                Refl.Print("botskill: " + dir.SkillTier + " (" + Ai.Difficulty.NameOf(dir.SkillTier) + ")");
                for (int i = 0; i < Ai.Difficulty.Count; i++)
                    Refl.Print("  " + i + " = " + Ai.Difficulty.NameOf(i));
                return;
            }

            int tier;
            if (!int.TryParse(args[0], out tier))
            {
                tier = -1;
                for (int i = 0; i < Ai.Difficulty.Count; i++)
                    if (Ai.Difficulty.NameOf(i) == args[0].ToLowerInvariant()) tier = i;
                if (tier < 0) { Refl.Print("usage: botskill <0-" + (Ai.Difficulty.Count - 1) + "|name>"); return; }
            }

            dir.SkillTier = Mathf.Clamp(tier, 0, Ai.Difficulty.Count - 1);
            Refl.Print("botskill: " + dir.SkillTier + " (" + Ai.Difficulty.NameOf(dir.SkillTier) + ")");
        }

        internal static void BotKits(string[] args)
        {
            var dir = BotDirector.Instance;
            if (dir == null) { Refl.Print("botkits: director not running"); return; }

            if (args.Length > 0 && (args[0] == "reroll" || args[0] == "new"))
            {
                dir.RerollKits();
                Refl.Print("botkits: rerolled (seed " + dir.KitSeed + ") - takes effect on next respawn");
                return;
            }

            Refl.Print("botkits: random=" + Plugin.RandomKits.Value + " seed=" + dir.KitSeed
                       + " skills=" + Plugin.SkillCount.Value + " grenades=" + Plugin.Grenades.Value);
            foreach (var a in dir.Agents)
            {
                var k = dir.PeekKit(a.Group);
                Refl.Print("  g" + a.Group + " " + (k == null ? "<not rolled>" : k.Summary));
            }
        }

        internal static void NavBake(string[] args)
        {
            var dir = BotDirector.Instance;
            if (dir == null) { Refl.Print("navbake: director not running"); return; }
            if (!Refl.IsServer) { Refl.Print("navbake: only the host can bake"); return; }
            if (!Refl.IsGameLoaded) { Refl.Print("navbake: no map loaded"); return; }

            bool force = args.Length > 0 && (args[0] == "force" || args[0] == "1");
            string map = Refl.MapName;

            if (force) NavCache.Delete(map);
            if (dir.StartBake(map, force)) Refl.Print("navbake: baking " + map + " ...");
            else Refl.Print("navbake: already baked (use 'navbake force') or busy");
        }

        internal static void NavInfo(string[] args)
        {
            var dir = BotDirector.Instance;
            if (dir == null) { Refl.Print("navinfo: director not running"); return; }

            string map = Refl.MapName;
            Refl.Print("map: " + (string.IsNullOrEmpty(map) ? "<none>" : map));
            Refl.Print("cache: " + NavCache.PathFor(map ?? string.Empty) + (NavCache.Exists(map) ? " (present)" : " (absent)"));
            if (dir.Grid == null) { Refl.Print("grid: not loaded  bake: " + dir.BakeStatus); return; }

            var g = dir.Grid;
            Refl.Print("grid: " + g.Count + " nodes, " + g.LinkCount + " links, cell " + g.Cell);
            Refl.Print("bounds: " + g.Min + " .. " + g.Max);
        }

        internal static void NavDraw(string[] args)
        {
            var d = NavDebug.Instance;
            if (d == null) { Refl.Print("navdraw: renderer not running"); return; }
            d.Enabled = args.Length == 0 ? !d.Enabled : args[0] != "0";
            Refl.Print("navdraw: " + (d.Enabled ? "on" : "off"));
        }

        internal static void BotDebug(string[] args)
        {
            var dir = BotDirector.Instance;
            if (dir == null) { Refl.Print("botdebug: director not running"); return; }
            dir.ShowDebug = args.Length == 0 ? !dir.ShowDebug : args[0] != "0";
            Refl.Print("botdebug: " + (dir.ShowDebug ? "on" : "off"));
        }

        internal static void BotRebalance(string[] args)
        {
            var dir = BotDirector.Instance;
            if (dir == null) { Refl.Print("botrebalance: director not running"); return; }

            var game = Refl.ServerGame;
            if (game == null) { Refl.Print("botrebalance: no active game"); return; }

            // Check if there are any bots
            if (dir.AgentCount == 0) { Refl.Print("botrebalance: no bots to rebalance"); return; }

            int targetTeam = 0;
            int moved = 0;

            // Strictly alternate teams to guarantee a 50/50 split
            foreach (var a in dir.Agents)
            {
                ChangeBotTeam(a, targetTeam, game);
                targetTeam = 1 - targetTeam; // Alternates between 0 (Bear) and 1 (USEC)
                moved++;
            }

            Refl.Print($"botrebalance: reassigned {moved} bots to balance teams (0=Bear, 1=USEC)");
        }

        internal static void BotTeam(string[] args)
        {
            var dir = BotDirector.Instance;
            if (dir == null) { Refl.Print("botteam: director not running"); return; }
            if (args.Length < 2) { Refl.Print("usage: botteam <group> <0|1>  (0=Bear, 1=USEC)"); return; }

            if (!int.TryParse(args[0], out int group)) { Refl.Print("botteam: invalid group number"); return; }
            if (!int.TryParse(args[1], out int team) || (team != 0 && team != 1)) { Refl.Print("botteam: invalid team (use 0 or 1)"); return; }

            var agent = dir.AgentByGroup(group);
            if (agent == null) { Refl.Print($"botteam: bot g{group} not found"); return; }

            var game = Refl.ServerGame;
            ChangeBotTeam(agent, team, game);
            Refl.Print($"botteam: moved bot g{group} to team {(team == 0 ? "Bear" : "USEC")}");
        }

        // private static void ChangeBotTeam(BotAgent agent, int newTeam, object game)
        // {
        //     if (agent.Server == null) return;
        //     var info = Refl.PlayerInfo(agent.Server);
        //     if (info != null)
        //     {
        //         Refl.SetPlayerType(info, newTeam);
        //         agent.Team = newTeam;
        //     }
        // }
        private static void ChangeBotTeam(BotAgent agent, int newTeam, object game)
        {
            if (agent == null || agent.Server == null) return;

            // Update both the PlayerInfo AND the server player object
            Refl.SetPlayerType(Refl.PlayerInfo(agent.Server), newTeam);
            Refl.SetPlayerTeam(agent.Server, newTeam);

            // Update the agent's cached team
            agent.Team = newTeam;

            Plugin.Log.LogInfo($"Changed bot g{agent.Group} to team {newTeam}");
        }

        // internal static void KickAllBots(string[] args)
        // {
        //     var dir = BotDirector.Instance;
        //     if (dir == null) { Refl.Print("kickbots: director not running"); return; }

        //     int kicked = dir.AgentCount;

        //     // Clear all bot tracking
        //     dir.Clear();

        //     Refl.Print($"kickbots: removed {kicked} bots from the match");
        // }
        // internal static void KickAllBots(string[] args)
        // {
        //     var dir = BotDirector.Instance;
        //     if (dir == null) { Refl.Print("kickbots: director not running"); return; }

        //     var game = Refl.ServerGame;
        //     if (game == null) { Refl.Print("kickbots: no active game"); return; }

        //     int kicked = 0;

        //     // Actually remove each bot from the server
        //     foreach (var agent in dir.Agents.ToList())
        //     {
        //         if (agent.Server != null)
        //         {
        //             Refl.RemovePlayer(agent.Server);
        //             kicked++;
        //         }
        //     }

        //     // Wait a frame for removals to process, then clear tracking
        //     UnityEngine.CoroutineRunner.Start(ClearAfterDelay(dir, kicked));

        //     Refl.Print($"kickbots: removed {kicked} bots from the match");
        // }

        internal static void KickAllBots(string[] args)
        {
            var dir = BotDirector.Instance;
            if (dir == null) { Refl.Print("kickbots: director not running"); return; }

            var game = Refl.ServerGame;
            if (game == null) { Refl.Print("kickbots: no active game"); return; }

            int count = dir.AgentCount;

            // Clear bot tracking
            dir.Clear();
            dir.Wanted = 0;

            // Reset the server to clear all player entries
            Refl.ResetServer();

            Refl.Print($"kickbots: cleared {count} bots and reset server");
            Refl.Print("kickbots: RESTART THE MATCH to fully clear loading players");
        }



        private static System.Collections.IEnumerator ClearAfterDelay(BotDirector dir, int count)
        {
            yield return new UnityEngine.WaitForEndOfFrame();
            dir.Clear();
            Plugin.Log.LogInfo($"Cleared bot tracking after kicking {count} bots");
        }
    }
}
