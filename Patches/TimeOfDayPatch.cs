using HarmonyLib;
using UnityEngine;

namespace LethalSeedCracker2.Patches
{
    [HarmonyPatch(typeof(TimeOfDay))]
    internal class TimeOfDayPatch
    {
        internal static float meteorShowerAtTime;

        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        public static void SetTimeSpeed(TimeOfDay __instance)
        {
            __instance.globalTimeSpeedMultiplier = __instance.lengthOfHours * 4;
            LethalSeedCracker2.Logger.LogInfo($"Set time speed multiplier to {__instance.globalTimeSpeedMultiplier}");
            float speed = StartOfRound.Instance.shipAnimator.GetComponent<Animator>().speed *= 10f;
            LethalSeedCracker2.Logger.LogInfo($"Set ship speed to {speed}");
        }

        [HarmonyPatch("DecideRandomDayEvents")]
        [HarmonyPostfix]
        public static void GetDayEventsResult(TimeOfDay __instance)
        {
            meteorShowerAtTime = __instance.meteorShowerAtTime;
        }
    }
}
