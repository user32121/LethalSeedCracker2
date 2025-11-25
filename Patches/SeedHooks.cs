using HarmonyLib;
using LethalSeedCracker2.src.cracker;
using System;

namespace LethalSeedCracker2.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class SeedHooks
    {
        [HarmonyPatch("StartGame")]
        [HarmonyPrefix]
        public static void SetSeed(StartOfRound __instance)
        {
            BeeState.Reset();

            if (SeedCracker.config is null)
            {
                LethalSeedCracker2.Logger.LogInfo("No config loaded; not setting parameters");
                return;
            }
            if (SeedCracker.config.eclipsed)
            {
                SeedCracker.config.currentLevel.currentWeather = LevelWeatherType.Eclipsed;
                LethalSeedCracker2.Logger.LogInfo($"Eclipsed weather");
            }
            else
            {
                SeedCracker.config.currentLevel.currentWeather = LevelWeatherType.None;
                LethalSeedCracker2.Logger.LogInfo($"Cleared weather");
            }
            __instance.overrideRandomSeed = true;
            __instance.overrideSeedNumber = SeedCracker.config.seeds[SeedCracker.config.curSeedIdx];
            LethalSeedCracker2.Logger.LogInfo($"Seed set to {__instance.overrideSeedNumber}");
            __instance.ChangeLevel(Array.IndexOf(__instance.levels, SeedCracker.config.currentLevel));
            LethalSeedCracker2.Logger.LogInfo($"Moon set to {__instance.currentLevel}");
            TimeOfDay.Instance.timeUntilDeadline = TimeOfDay.Instance.totalTime * SeedCracker.config.daysUntilDeadline;
            TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
            LethalSeedCracker2.Logger.LogInfo($"DaysUntilDeadline set to {TimeOfDay.Instance.daysUntilDeadline}");
            __instance.daysPlayersSurvivedInARow = SeedCracker.config.daysPlayersSurvivedInARow;
            LethalSeedCracker2.Logger.LogInfo($"DaysPlayersSurvivedInARow set to {__instance.daysPlayersSurvivedInARow}");
        }

        [HarmonyPatch("ShipHasLeft")]
        [HarmonyPrefix]
        public static void CollectResults()
        {
            if (SeedCracker.config is null)
            {
                LethalSeedCracker2.Logger.LogInfo("No config loaded; not collecting results");
                return;
            }
            CrackingResult result = new(SeedCracker.config);
            LethalSeedCracker2.Logger.LogInfo(result);
            if (SeedCracker.config.Filter(result))
            {
                LethalSeedCracker2.Logger.LogInfo("Passed filter");
                result.Save("results2.txt", SeedCracker.config.foundSeeds != 0);
                ++SeedCracker.config.foundSeeds;
            }
            else
            {
                LethalSeedCracker2.Logger.LogInfo("Seed did not pass filter");
            }
            ++SeedCracker.config.curSeedIdx;
            if (SeedCracker.config.curSeedIdx >= SeedCracker.config.seeds.Count)
            {
                LethalSeedCracker2.Logger.LogInfo("Finished all seeds");
                SeedCracker.config = null;
            }
        }
    }
}
