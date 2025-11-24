using HarmonyLib;
using LethalSeedCracker2.src.config;

namespace LethalSeedCracker2.Patches;

[HarmonyPatch(typeof(RoundManager))]
public class SeedCracker
{
    internal static Config? config;

    [HarmonyPatch("Start")]
    [HarmonyPrefix]
    private static void LoadConfig()
    {
        config = new("config2.txt");
        LethalSeedCracker2.Logger.LogInfo("Config loaded");
    }
}
