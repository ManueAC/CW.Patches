using System;
using System.Reflection;
using HarmonyLib;

namespace CW.ServerCmds.Patches
{
    [HarmonyPatch]
    internal static class Patch_ParseLine
    {
        private static readonly char[] Sep = { ' ', '=' };

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("Console"), "ParseLine", new[] { typeof(string) });
        }

        private static bool Prefix(string line)
        {
            if (string.IsNullOrEmpty(line)) return true;
            var parts = line.Split(Sep);
            var cmd = parts[0];
            if (cmd != "startserver" && cmd != "joinserver") return true;

            var args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);
            if (cmd == "startserver") ServerCmds.StartServer(args);
            else ServerCmds.JoinServer(args);
            return false;
        }
        // private static bool Prefix(string line)
        // {
        //     if (string.IsNullOrEmpty(line)) return true;

        //     var parts = line.Split(Sep);
        //     var cmd = parts[0].ToLowerInvariant();

        //     // Forward bot commands to CW.Bots using reflection
        //     if (cmd == "bots" || cmd == "navbake" || cmd == "navinfo" ||
        //         cmd == "navdraw" || cmd == "botdebug" || cmd == "botskill" ||
        //         cmd == "botkits" || cmd == "botrebalance" || cmd == "botteam")
        //     {
        //         try
        //         {
        //             var botsAssembly = Assembly.Load("CW.Bots");
        //             if (botsAssembly != null)
        //             {
        //                 var commandsType = botsAssembly.GetType("CW.Bots.Commands");
        //                 if (commandsType != null)
        //                 {
        //                     string methodName = null;
        //                     if (cmd == "bots") methodName = "Bots";
        //                     else if (cmd == "navbake") methodName = "NavBake";
        //                     else if (cmd == "navinfo") methodName = "NavInfo";
        //                     else if (cmd == "navdraw") methodName = "NavDraw";
        //                     else if (cmd == "botdebug") methodName = "BotDebug";
        //                     else if (cmd == "botskill") methodName = "BotSkill";
        //                     else if (cmd == "botkits") methodName = "BotKits";
        //                     else if (cmd == "botrebalance") methodName = "BotRebalance"; // <-- ADDED
        //                     else if (cmd == "botteam") methodName = "BotTeam";
        //                     // else if (cmd == "kickbots") methodName = "KickAllBots";
        //                     if (methodName != null)
        //                     {
        //                         var method = commandsType.GetMethod(methodName,
        //                             BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        //                         if (method != null)
        //                         {
        //                             var args = new string[parts.Length - 1];
        //                             Array.Copy(parts, 1, args, 0, args.Length);
        //                             method.Invoke(null, new object[] { args });
        //                             return false; // Command handled
        //                         }
        //                     }
        //                 }
        //             }
        //         }
        //         catch (Exception e)
        //         {
        //             UnityEngine.Debug.LogError("Bot command failed: " + e.Message);
        //         }
        //         return false;
        //     }

        //     // Handle server commands only
        //     if (cmd != "startserver" && cmd != "joinserver") return true;

        //     var serverArgs = new string[parts.Length - 1];
        //     Array.Copy(parts, 1, serverArgs, 0, serverArgs.Length);
        //     if (cmd == "startserver") ServerCmds.StartServer(serverArgs);
        //     else ServerCmds.JoinServer(serverArgs);
        //     return false;
        // }
    }
}