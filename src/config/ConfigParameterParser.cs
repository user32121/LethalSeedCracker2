using System;
using System.Collections.Generic;

namespace LethalSeedCracker2.src.config
{
    internal class ConfigParameterParser(string cmd,
        Action<Config> apply) : ConfigCommandParser(cmd)
    {
        internal override void Process(Config config)
        {
            apply(config);
        }
    }
    internal class ConfigParameterParser<T0>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Action<Config, T0> apply) : ConfigCommandParser<T0>(cmd, parser0, name0)
    {
        internal override void Process(Config config, T0 arg0)
        {
            apply(config, arg0);
        }
    }
    internal class ConfigParameterParser<T0, T1>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<Config, string, T1> parser1, string name1,
        Action<Config, T0, T1> apply) : ConfigCommandParser<T0, T1>(cmd, parser0, name0, parser1, name1)
    {
        internal override void Process(Config config, T0 arg0, T1 arg1)
        {
            apply(config, arg0, arg1);
        }
    }
    internal class ConfigParameterListParser<T0>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Action<Config, List<T0>> apply) : ConfigCommandListParser<T0>(cmd, parser0, name0)
    {
        internal override void Process(Config config, List<T0> arg0s)
        {
            apply(config, arg0s);
        }
    }
}
