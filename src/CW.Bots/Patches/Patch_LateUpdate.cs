using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace CW.Bots.Patches
{
    [HarmonyPatch]
    internal static class Patch_LateUpdate
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("BotNetPlayer"), "LateUpdate");
        }

        private static void Prefix()
        {
            var dir = BotDirector.Instance;
            if (dir == null || !Refl.Ready) return;

            try
            {
                var game = Refl.ServerGame;
                if (game != null) Refl.SetBotType(dir.ClaimTeam(game));
            }
            catch { }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var think = typeof(BotHook).GetMethod("Think", BindingFlags.Public | BindingFlags.Static);
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                var c = codes[i];
                var m = c.operand as MethodInfo;
                if (m == null) continue;
                if (m.Name != "Save" || m.DeclaringType == null || m.DeclaringType.Name != "CWInput") continue;
                if (c.opcode != OpCodes.Callvirt && c.opcode != OpCodes.Call) continue;

                codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, think));
                i += 2;
            }

            return codes;
        }
    }
}

// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using System.Reflection.Emit;
// using HarmonyLib;
// using UnityEngine;

// namespace CW.Bots.Patches
// {
//     [HarmonyPatch]
//     internal static class Patch_LateUpdate
//     {
//         private static MethodBase TargetMethod()
//         {
//             return AccessTools.Method(AccessTools.TypeByName("BotNetPlayer"), "LateUpdate");
//         }

//         private static void Prefix()
//         {
//             var dir = BotDirector.Instance;
//             if (dir == null || !Refl.Ready) return;

//             try
//             {
//                 var game = Refl.ServerGame;
//                 if (game != null) Refl.SetBotType(dir.ClaimTeam(game));
//             }
//             catch { }
//         }

//         private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
//         {
//             // Use both Public and NonPublic to find internal methods
//             var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

//             var think = typeof(BotHook).GetMethod("Think", flags);
//             var setCmdButtons = typeof(Refl).GetMethod("SetCmdButtons", flags);
//             var getBotCmd = typeof(Refl).GetMethod("BotCmd", flags);

//             if (think == null)
//                 Plugin.Log.LogError("Failed to find BotHook.Think method");
//             if (setCmdButtons == null)
//                 Plugin.Log.LogError("Failed to find Refl.SetCmdButtons method");
//             if (getBotCmd == null)
//                 Plugin.Log.LogError("Failed to find Refl.BotCmd method");

//             if (think == null || setCmdButtons == null || getBotCmd == null)
//             {
//                 Plugin.Log.LogError("CRITICAL: Failed to find methods for bot movement patch!");
//                 return instructions;
//             }

//             var codes = new List<CodeInstruction>(instructions);

//             for (int i = 0; i < codes.Count; i++)
//             {
//                 var c = codes[i];
//                 var m = c.operand as MethodInfo;
//                 if (m == null) continue;
//                 if (m.Name != "Save" || m.DeclaringType == null || m.DeclaringType.Name != "CWInput") continue;
//                 if (c.opcode != OpCodes.Callvirt && c.opcode != OpCodes.Call) continue;

//                 // Stack has: saved (int)

//                 // 1. Load bot instance (this) for Think()
//                 codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
//                 // Stack: saved, bot

//                 // 2. Call BotHook.Think(saved, bot) - returns new buttons
//                 codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, think));
//                 // Stack: newButtons (int)

//                 // 3. Store buttons in local variable
//                 codes.Insert(i + 3, new CodeInstruction(OpCodes.Stloc_0));
//                 // Stack: (empty)

//                 // 4. Load bot instance again
//                 codes.Insert(i + 4, new CodeInstruction(OpCodes.Ldarg_0));
//                 // Stack: bot

//                 // 5. Get bot's command object
//                 codes.Insert(i + 5, new CodeInstruction(OpCodes.Call, getBotCmd));
//                 // Stack: cmd (object)

//                 // 6. Load stored buttons
//                 codes.Insert(i + 6, new CodeInstruction(OpCodes.Ldloc_0));
//                 // Stack: cmd, buttons

//                 // 7. Apply buttons to command
//                 codes.Insert(i + 7, new CodeInstruction(OpCodes.Call, setCmdButtons));
//                 // Stack: (empty)

//                 i += 7;
//             }

//             Plugin.Log.LogInfo("Bot movement patch applied successfully");
//             return codes;
//         }
//     }
// }