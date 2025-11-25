using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace LethalSeedCracker2.Patches
{
    [HarmonyPatch(typeof(RedLocustBees))]
    internal class BeeState
    {
        public static int totalTicks = 0;
        public static Dictionary<RedLocustBees, int> roamingBees = [];

        private static float lastTime;

        [HarmonyPatch(nameof(RedLocustBees.Update))]
        [HarmonyPrefix]
        public static void Update(RedLocustBees __instance)
        {
            //LethalSeedCracker2.Logger.LogInfo($"bee update {__instance} {__instance.GetHashCode()} {__instance.currentBehaviourStateIndex} hive {__instance.IsHiveMissing()} ({Vector3.Distance(__instance.eye.position, __instance.lastKnownHivePosition)}, {Vector3.Distance(__instance.hive.transform.position, __instance.lastKnownHivePosition)}): {Vector3.Distance(__instance.hive.transform.position, __instance.lastKnownHivePosition) > 6}");
            if (Vector3.Distance(__instance.hive.transform.position, __instance.lastKnownHivePosition) > 6)
            {
                //LethalSeedCracker2.Logger.LogInfo($"bee update {__instance} {__instance.GetHashCode()} {__instance.currentBehaviourStateIndex} hive {__instance.IsHiveMissing()} ({Vector3.Distance(__instance.eye.position, __instance.lastKnownHivePosition)}, {Vector3.Distance(__instance.hive.transform.position, __instance.lastKnownHivePosition)}): {Vector3.Distance(__instance.hive.transform.position, __instance.lastKnownHivePosition) > 6}");
                roamingBees[__instance] = roamingBees.GetValueOrDefault(__instance) + 1;
            }
            if (lastTime != Time.time)
            {
                lastTime = Time.time;
                ++totalTicks;
            }
        }

        public static void Reset()
        {
            totalTicks = 0;
            roamingBees.Clear();
        }
    }
}
