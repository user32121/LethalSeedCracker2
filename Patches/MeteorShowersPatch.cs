using HarmonyLib;

namespace LethalSeedCracker2.Patches
{
    [HarmonyPatch(typeof(MeteorShowers))]
    internal class MeteorShowersPatch
    {
        internal static int numMeteors;

        [HarmonyPatch("BeginDay")]
        [HarmonyPostfix]
        public static void GetMeteors(MeteorShowers __instance)
        {
            numMeteors = __instance.meteors.Count;
        }
    }
}
