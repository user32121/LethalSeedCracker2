using HarmonyLib;

namespace LethalSeedCracker2.Patches
{
    [HarmonyPatch(typeof(StartMatchLever))]
    internal class AutoStartRounds
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static void UpdatePrefix(StartMatchLever __instance)
        {
            if (SeedCracker.config is null || SeedCracker.config.seeds.Count == 0)
            {
                return;
            }
            if (__instance.playersManager.inShipPhase)
            {
                LethalSeedCracker2.Logger.LogInfo("Auto starting round");
                __instance.LeverAnimation();
                __instance.PullLever();
            }
            else if (SeedCracker.config.skipDay && __instance.leverHasBeenPulled && !__instance.playersManager.newGameIsLoading)
            {
                LethalSeedCracker2.Logger.LogInfo("Early ending round");
                __instance.playersManager.shipHasLanded = true;
                __instance.LeverAnimation();
                __instance.PullLever();
            }
        }
    }
}
