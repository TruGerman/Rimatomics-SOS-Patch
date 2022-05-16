using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Rimatomics;
using RimWorld;
using RimWorld.Planet;
using SaveOurShip2;
using Verse;

namespace RimatomicsPatch
{
    [StaticConstructorOnStartup]
    [HarmonyPatch]
    public static class Patch
    {
        private static RimatomicsResearch tempComp = null;

        static Patch()
        {
            var harmony = new Harmony("TruGerman.RimatomicsSOSPatch");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldSwitchUtility), "SwitchToNewWorld")]
        static void WorldSwitchPrefix(Map shipMap, Building_ShipBridge bridge) => RefreshTempComp();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldSwitchUtility), "ReturnToPreviousWorld")]
        static void NewWorldPrefix(Map shipMap, Building_ShipBridge bridge) => RefreshTempComp();

        //Holy mother of Jesus, it actually works
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(WorldSwitchUtility), "DoWorldSwitch")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(SymbolExtensions.GetMethodInfo(() => Find.World.renderer.RegenerateAllLayersNow())))
                {
                    yield return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => AssignNewComp()));
                }

                yield return instruction;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorldGenerator), "GenerateWorld")]
        static void GenerateWorlPostfix(World __result) => AssignNewComp(__result);

        //Default parameters didn't work, unfortunately
        static void AssignNewComp() => AssignNewComp(Find.World);

        static void AssignNewComp(World world)
        {
            if (tempComp == null || world == null) return;
            RimatomicsResearch researchComp = GetResearch(world);
            if (researchComp != null) world.components.Remove(researchComp);
            //It took me WAY too long to figure out that the instance is the root of all evil
            RimatomicsResearch._instance = tempComp;
            world.components.Add(tempComp);
        }

        //Typeof comparisons also didn't work
        static void RefreshTempComp () => tempComp = GetResearch();

        static RimatomicsResearch GetResearch(World w = null) => (RimatomicsResearch)(w ?? Find.World).components.First(x => x.ToString() == "Rimatomics.RimatomicsResearch");
    }
}
