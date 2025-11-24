using System;

namespace LethalSeedCracker2.src.config
{
    internal class ConfigParameter(string cmd,
        Action<Config> apply) : ConfigCommand(cmd)
    {
        internal override void Process(Config config)
        {
            apply(config);
        }
    }
    internal class ConfigParameter<T0>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Action<Config, T0> apply) : ConfigCommand<T0>(cmd, parser0, name0)
    {
        internal override void Process(Config config, T0 arg0)
        {
            apply(config, arg0);
        }
    }
    internal class ConfigParameter<T0, T1>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<Config, string, T1> parser1, string name1,
        Action<Config, T0, T1> apply) : ConfigCommand<T0, T1>(cmd, parser0, name0, parser1, name1)
    {
        internal override void Process(Config config, T0 arg0, T1 arg1)
        {
            apply(config, arg0, arg1);
        }
    }
}
