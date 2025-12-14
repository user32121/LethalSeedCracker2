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
                __instance.PullLeverAnim(true);
                __instance.StartGame();
            }
            else if (SeedCracker.config.skipDay && !__instance.playersManager.shipIsLeaving && !__instance.playersManager.newGameIsLoading)
            {
                LethalSeedCracker2.Logger.LogInfo("Early ending round");
                __instance.PullLeverAnim(false);
                __instance.EndGame();
            }
        }
    }
}
