using LethalSeedCracker2.src.cracker;
using System;
using System.Collections.Generic;

namespace LethalSeedCracker2.src.config
{
    internal interface IConfigFilter
    {
        internal bool Filter(CrackingResult result);
    }

    internal class ConfigFilterParser(string cmd,
        Func<CrackingResult, bool, bool> filter) : ConfigCommandParser(cmd), IConfigFilter
    {
        protected bool active = false;
        internal override void Process(Config config)
        {
            active = true;
        }
        bool IConfigFilter.Filter(CrackingResult result)
        {
            return filter(result, active);
        }
    }
    internal class ConfigFilter<T0>(string cmd,
        Func<Config, string, T0> parser0, T0 default0, string name0,
        Func<CrackingResult, T0, bool> filter) : ConfigCommand<T0>(cmd, parser0, name0), IConfigFilter
    {
        protected T0 arg0 = default0;
        internal override void Process(Config config, T0 arg0)
        {
            this.arg0 = arg0;
        }
        bool IConfigFilter.Filter(CrackingResult result)
        {
            return filter(result, arg0);
        }
    }
    internal class ConfigFilter<T0, T1>(string cmd,
        Func<Config, string, T0> parser0, T0 default0, string name0,
        Func<Config, string, T1> parser1, T1 default1, string name1,
        Func<CrackingResult, T0, T1, bool> filter) : ConfigCommand<T0, T1>(cmd, parser0, name0, parser1, name1), IConfigFilter
    {
        protected T0 arg0 = default0;
        protected T1 arg1 = default1;
        internal override void Process(Config config, T0 arg0, T1 arg1)
        {
            this.arg0 = arg0;
            this.arg1 = arg1;
        }
        bool IConfigFilter.Filter(CrackingResult result)
        {
            return filter(result, arg0, arg1);
        }
    }

    internal class ConfigFilters<T0>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<CrackingResult, List<T0>, bool> filter, Action<Config, List<T0>>? validation = null) : ConfigCommand<T0>(cmd, parser0, name0), IConfigFilter
    {
        protected List<T0> arg0s = [];
        internal override void Process(Config config, T0 arg0)
        {
            arg0s.Add(arg0);
            validation?.Invoke(config, arg0s);
        }
        bool IConfigFilter.Filter(CrackingResult result)
        {
            return filter(result, arg0s);
        }
    }
    internal class ConfigFilters<T0, T1>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<Config, string, T1> parser1, string name1,
        Func<CrackingResult, List<T0>, List<T1>, bool> filter, Action<Config, List<T0>, List<T1>>? validation = null) : ConfigCommand<T0, T1>(cmd, parser0, name0, parser1, name1), IConfigFilter
    {
        protected List<T0> arg0s = [];
        protected List<T1> arg1s = [];
        internal override void Process(Config config, T0 arg0, T1 arg1)
        {
            arg0s.Add(arg0);
            arg1s.Add(arg1);
            validation?.Invoke(config, arg0s, arg1s);
        }
        bool IConfigFilter.Filter(CrackingResult result)
        {
            return filter(result, arg0s, arg1s);
        }
    }
    internal class ConfigFilters<T0, T1, T2>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<Config, string, T1> parser1, string name1,
        Func<Config, string, T2> parser2, string name2,
        Func<CrackingResult, List<T0>, List<T1>, List<T2>, bool> filter, Action<Config, List<T0>, List<T1>, List<T2>>? validation = null) : ConfigCommand<T0, T1, T2>(cmd, parser0, name0, parser1, name1, parser2, name2), IConfigFilter
    {
        protected List<T0> arg0s = [];
        protected List<T1> arg1s = [];
        protected List<T2> arg2s = [];
        internal override void Process(Config config, T0 arg0, T1 arg1, T2 arg2)
        {
            arg0s.Add(arg0);
            arg1s.Add(arg1);
            arg2s.Add(arg2);
            validation?.Invoke(config, arg0s, arg1s, arg2s);
        }
        bool IConfigFilter.Filter(CrackingResult result)
        {
            return filter(result, arg0s, arg1s, arg2s);
        }
    }
}
