using System;

namespace LethalSeedCracker2.src.common
{
    internal static class Util
    {
        internal static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }

        internal static T NonNull<T>(T? obj, string name) => obj ?? throw new ArgumentNullException(name);
        internal static T NonNull<T>(T? obj, string name) where T : struct => obj ?? throw new ArgumentNullException(name);

        internal static T Inspect<T>(T obj, string format = "{0}")
        {
            LethalSeedCracker2.Logger.LogInfo(string.Format(format, obj));
            return obj;
        }
    }
}