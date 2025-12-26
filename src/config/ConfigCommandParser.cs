using LethalSeedCracker2.src.common;
using LethalSeedCracker2.src.cracker;
using System;
using System.Collections.Generic;
using System.IO;

namespace LethalSeedCracker2.src.config
{
    internal abstract class BaseConfigCommandParser(string cmd, int numArgs)
    {
        public string cmd = cmd;
        internal virtual void Parse(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, string[] args)
        {
            if (numArgs != -1)
            {
                Util.Assert(args.Length == numArgs, $"{cmd} expected {numArgs} arg, got {args.Length} ({this})");
            }
        }
    }
    internal abstract class ConfigCommandParser(string cmd
        ) : BaseConfigCommandParser(cmd, 0)
    {
        internal sealed override void Parse(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, string[] args)
        {
            base.Parse(config, filterConsumer, stream, args);
            Process(config, filterConsumer, stream);
        }
        internal abstract void Process(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream);
        public override string ToString()
        {
            return $"{cmd}";
        }
    }
    internal abstract class ConfigCommandParser<T0>(string cmd,
        Func<Config, string, T0> parser0, string name0
        ) : BaseConfigCommandParser(cmd, 1)
    {
        internal sealed override void Parse(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, string[] args)
        {
            base.Parse(config, filterConsumer, stream, args);
            Process(config, filterConsumer, stream, parser0(config, args[0]));
        }
        internal abstract void Process(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, T0 arg0);
        public override string ToString()
        {
            return $"{cmd} <{name0}>";
        }
    }
    internal abstract class ConfigCommandParser<T0, T1>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<Config, string, T1> parser1, string name1
        ) : BaseConfigCommandParser(cmd, 2)
    {
        internal sealed override void Parse(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, string[] args)
        {
            base.Parse(config, filterConsumer, stream, args);
            Process(config, filterConsumer, stream, parser0(config, args[0]), parser1(config, args[1]));
        }
        internal abstract void Process(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, T0 arg0, T1 arg1);
        public override string ToString()
        {
            return $"{cmd} <{name0}> <{name1}>";
        }
    }
    internal abstract class ConfigCommandParser<T0, T1, T2>(string cmd,
        Func<Config, string, T0> parser0, string name0,
        Func<Config, string, T1> parser1, string name1,
        Func<Config, string, T2> parser2, string name2
        ) : BaseConfigCommandParser(cmd, 3)
    {
        internal sealed override void Parse(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, string[] args)
        {
            base.Parse(config, filterConsumer, stream, args);
            Process(config, filterConsumer, stream, parser0(config, args[0]), parser1(config, args[1]), parser2(config, args[2]));
        }
        internal abstract void Process(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, T0 arg0, T1 arg1, T2 arg2);
        public override string ToString()
        {
            return $"{cmd} <{name0}> <{name1}> <{name2}>";
        }
    }
    internal abstract class ConfigCommandListParser<T0>(string cmd,
        Func<Config, string, T0> parser0, string name0
        ) : BaseConfigCommandParser(cmd, -1)
    {
        internal sealed override void Parse(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, string[] args)
        {
            base.Parse(config, filterConsumer, stream, args);
            List<T0> arg0s = [];
            for (int i = 0; i < args.Length; ++i)
            {
                arg0s.Add(parser0(config, args[i]));
            }
            Process(config, filterConsumer, stream, arg0s);
        }
        internal abstract void Process(Config config, Action<Predicate<CrackingResult>> filterConsumer, TextReader stream, List<T0> arg0);
        public override string ToString()
        {
            return $"{cmd} <{name0}> ...";
        }
    }
}
