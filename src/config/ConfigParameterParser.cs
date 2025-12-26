using System;
using System.Collections.Generic;
using System.IO;

namespace LethalSeedCracker2.src.config
{
    internal class ConfigParameterParser(string cmd,
        Action<Config, TextReader> apply) : ConfigCommandParser(cmd)
    {
        internal override void Process(Config config, TextReader stream)
        {
            apply(config, stream);
        }
    }
    internal class ConfigParameterParser<T0>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Action<Config, TextReader, T0> apply) : ConfigCommandParser<T0>(cmd, parser0, name0)
    {
        internal override void Process(Config config, TextReader stream, T0 arg0)
        {
            apply(config, stream, arg0);
        }
    }
    internal class ConfigParameterParser<T0, T1>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<Config, string, T1> parser1, string name1,
        Action<Config, TextReader, T0, T1> apply) : ConfigCommandParser<T0, T1>(cmd, parser0, name0, parser1, name1)
    {
        internal override void Process(Config config, TextReader stream, T0 arg0, T1 arg1)
        {
            apply(config, stream, arg0, arg1);
        }
    }
    internal class ConfigParameterListParser<T0>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Action<Config, TextReader, List<T0>> apply) : ConfigCommandListParser<T0>(cmd, parser0, name0)
    {
        internal override void Process(Config config, TextReader stream, List<T0> arg0s)
        {
            apply(config, stream, arg0s);
        }
    }
}
