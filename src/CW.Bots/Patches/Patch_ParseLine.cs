using System;
using System.Reflection;
using HarmonyLib;

namespace CW.Bots.Patches
{
    // This patch is now disabled - CW.ServerCmds handles all commands
    [HarmonyPatch]
    internal static class Patch_ParseLine
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("Console"), "ParseLine", new[] { typeof(string) });
        }

        private static bool Prefix(string line)
        {
            // Always return true - let CW.ServerCmds handle it
            return true;
        }
    }
}