using LethalSeedCracker2.src.cracker;
using System;
using System.IO;

namespace LethalSeedCracker2.src.config
{
    internal class ConfigFilterParser(string cmd,
        Predicate<CrackingResult> filter) : ConfigCommandParser(cmd)
    {
        internal override void Process(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream)
        {
            filterConsumer(filter);
        }
    }
    internal class ConfigFilterParser<T0>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<CrackingResult, T0, bool> filter)
        : ConfigCommandParser<T0>(cmd, parser0, name0)
    {
        internal override void Process(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, T0 arg0)
        {
            filterConsumer(result => filter(result, arg0));
        }
    }
    internal class ConfigFilterParser<T0, T1>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<Config, string, T1> parser1, string name1,
        Func<CrackingResult, T0, T1, bool> filter)
        : ConfigCommandParser<T0, T1>(cmd, parser0, name0, parser1, name1)
    {
        internal override void Process(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, T0 arg0, T1 arg1)
        {
            filterConsumer(result => filter(result, arg0, arg1));
        }
    }
    internal class ConfigFilterParser<T0, T1, T2>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<Config, string, T1> parser1, string name1,
        Func<Config, string, T2> parser2, string name2,
        Func<CrackingResult, T0, T1, T2, bool> filter,
        Action<Config, T0, T1, T2>? validation = null)
        : ConfigCommandParser<T0, T1, T2>(cmd, parser0, name0, parser1, name1, parser2, name2)
    {
        internal override void Process(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, T0 arg0, T1 arg1, T2 arg2)
        {
            validation?.Invoke(config, arg0, arg1, arg2);
            filterConsumer(result => filter(result, arg0, arg1, arg2));
        }
    }
    internal class ConfigMetaFilterParser(string cmd,
        Func<Config, TextReader, Predicate<CrackingResult>> metaFilter,
        string? usage = null) //meta filters tend to not have descriptive commands, so a more helpful usage can be provided
        : ConfigCommandParser(cmd)
    {
        internal override void Process(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream)
        {
            filterConsumer(metaFilter(config, stream));
        }
        public override string ToString()
        {
            return usage ?? base.ToString();
        }
    }
}
