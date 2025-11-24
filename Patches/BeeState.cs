using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace LethalSeedCracker2.Patches
{
    [HarmonyPatch(typeof(RedLocustBees))]
    internal class BeeState
    {
        public static HashSet<RedLocustBees> roamingBees = [];

        [HarmonyPatch(nameof(RedLocustBees.Update))]
        [HarmonyPrefix]
        public static void IsHiveMissing(RedLocustBees __instance)
        {
            //LethalSeedCracker2.Logger.LogInfo($"bee update {__instance} {__instance.GetHashCode()} {__instance.currentBehaviourStateIndex} hive {__instance.IsHiveMissing()} ({Vector3.Distance(__instance.eye.position, __instance.lastKnownHivePosition)}, {Vector3.Distance(__instance.hive.transform.position, __instance.lastKnownHivePosition)})");
            if (Vector3.Distance(__instance.hive.transform.position, __instance.lastKnownHivePosition) > 6)
            {
                roamingBees.Add(__instance);
            }
        }
    }
}
