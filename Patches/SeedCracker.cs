using HarmonyLib;

namespace LethalSeedCracker2.Patches;

[HarmonyPatch(typeof(RoundManager))]
public class SeedCracker
{
    internal static CrackingConfig? config;

    [HarmonyPatch("Start")]
    [HarmonyPrefix]
    private static void LoadConfig()
    {
        config = new("config2.txt");
        LethalSeedCracker2.Logger.LogInfo("Config loaded");
    }
}
