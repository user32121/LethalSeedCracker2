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
            if (SeedCracker.config is null)
            {
                return;
            }
            if (__instance.playersManager.inShipPhase)
            {
                LethalSeedCracker2.Logger.LogInfo("Auto starting round");
                __instance.LeverAnimation();
                __instance.PullLever();
            }
        }
    }
}
