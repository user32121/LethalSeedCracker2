using LethalSeedCracker2.src.cracker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LethalSeedCracker2.src.config
{
    internal class Config
    {
        //parameters
        internal int curSeedIdx = 0;
        internal List<int> seeds = [];
        internal SelectableLevel currentLevel;
        internal int daysUntilDeadline = 3;
        internal int daysPlayersSurvivedInARow = 0;
        private bool ignorepower;
        internal bool eclipsed;
        internal bool skipEnemies;
        internal bool skipTraps;
        internal bool skipOutsideObjects;
        internal bool skipScrap;
        internal bool skipWeather;
        internal bool skipDay;

        internal int foundSeeds = 0;

        private readonly List<Predicate<CrackingResult>> filters = [];
        private delegate bool Comparator(float arg1, float arg2);
        private readonly List<Tuple<EnemyType, Comparator, int>> enemyConstraints = [];

        //convenience name mappings
        private static readonly Dictionary<string, string> colloquialNames = new()
        {
            ["oldbird"] = "radmech",
            ["bracken"] = "flowerman",
            ["lootbug"] = "hoarderbug",
            ["mimic"] = "masked",
        };

        private static readonly Func<Config, string, int> ParseInt = (config, s) => int.Parse(s);
        private static readonly Func<Config, string, float> ParseFloat = (config, s) => float.Parse(s);
        private static readonly List<BaseConfigCommandParser> commands =
        [
            new ConfigParameterParser<int>("seed", ParseInt, "seed", (config, seed) => config.seeds.Add(seed)),
            new ConfigParameterListParser<int>("seeds", ParseInt, "seed", (config, seeds) => config.seeds.AddRange(seeds)),
            new ConfigParameterParser<int, int>("seedrange", ParseInt, "min", ParseInt, "max", (config, min, max) => config.seeds.AddRange(Enumerable.Range(min, max - min + 1))),
            new ConfigParameterParser<string>("seedfile", (config, s) => s, "file", (config,filename) => {
                string folderPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "32121", "LethalSeedCracker");
                string filepath = Path.Join(folderPath, filename);
                using StreamReader file = new(File.OpenRead(filepath));
                while (!file.EndOfStream) {
                    string[] line = file.ReadLine().Trim().Split();
                    if (line.Length == 0) {
                        continue;
                    }
                    if (!line[0].StartsWith("seed")) {
                        LethalSeedCracker2.Logger.LogInfo($"Skipping line: {string.Join(" ", line)}");
                        continue;
                    }
                    for (int i = 1; i < line.Length; i++) {
                        LethalSeedCracker2.Logger.LogInfo($"Adding seed {line[i]}");
                        config.seeds.Add(int.Parse(line[i]));
                    }
                }
            }),
            new ConfigParameterParser<int>("daystildeadline", ParseInt, "days", (config, days) => config.daysUntilDeadline = days),
            new ConfigParameterParser<int>("dayssurvived", ParseInt, "days", (config, days) => config.daysPlayersSurvivedInARow = days),
            new ConfigParameterParser<SelectableLevel>("moon", ParseMoon, "moon", (config, moon) => config.currentLevel = moon),
            new ConfigParameterParser("eclipsed", config => config.eclipsed = true),
            new ConfigParameterParser("ignorepower", config => config.ignorepower = true),
            new ConfigParameterParser("skipenemies", config => config.skipEnemies = true),
            new ConfigParameterParser("skiptraps", config => config.skipTraps = true),
            new ConfigParameterParser("skipoutsideobjects", config => config.skipOutsideObjects = true),
            new ConfigParameterParser("skipscrap", config => config.skipScrap = true),
            new ConfigParameterParser("skipweather", config => config.skipWeather = true),
            new ConfigParameterParser("skipday", config => config.skipDay = true),

            new ConfigFilterParser<Defines.DUNGEON>("dungeon", ParseEnum<Defines.DUNGEON>, "dungeon", (result, dungeon) => dungeon == result.levelResult.currentDungeonType),
            new ConfigFilterParser<EnemyType>("infestation", ParseEnemy, "enemy", (result, enemy) => enemy == result.enemyResult.infestation),
            new ConfigFilterParser("meteor", result => result.levelResult.meteor),
            new ConfigFilterParser<LevelWeatherType, SelectableLevel>("weather", ParseEnum<LevelWeatherType>, "weather", ParseMoon, "moon", (result, weather, moon) => result.weatherResult.weathers.GetValueOrDefault(moon, LevelWeatherType.None) == weather),
            new ConfigFilterParser<EnemyType, Comparator, int>("enemy", ParseEnemy, "enemy", ParseComparator, "comparator", ParseInt, "num", (result, enemy, op, num) => op(result.enemyResult.enemyCounts.GetValueOrDefault(enemy), num), CheckEnemyPower),
            new ConfigFilterParser<Item, Comparator, int>("scrap", ParseScrap, "scrap", ParseComparator, "comparator", ParseInt, "num", (result, scrap, op, num) => op(result.scrapResult.scrapCounts.GetValueOrDefault(scrap), num)),
            new ConfigFilterParser<Defines.TRAP, Comparator, int>("trap", ParseEnum<Defines.TRAP>, "trap", ParseComparator, "comparator", ParseInt, "num", (result, trap, op, num) => op(result.levelResult.trapCounts.GetValueOrDefault(trap), num)),
            new ConfigFilterParser<string, Comparator, int>("outsideobject", ParseOutsideObject, "object", ParseComparator, "comparator", ParseInt, "num", (result, obj, op, num) => op(result.levelResult.outsideObjectCounts.GetValueOrDefault(obj), num)),
            new ConfigFilterParser("blackout", result => result.levelResult.blackout),
            new ConfigFilterParser<CompanyMood>("companymood", ParseCompanyMood, "mood", (result, mood) => mood == null || mood == result.levelResult.currentCompanyMood),
            new ConfigFilterParser("indoorfog", result => result.enemyResult.indoorFog),
            new ConfigFilterParser<Comparator, float>("closesttrap", ParseComparator, "comparator", ParseFloat, "distance", (result, op, num) => result.levelResult.nearestEntranceTraps.Count > 0 && op(result.levelResult.nearestEntranceTraps.Min(x => x.Value.Item2), num)),
            new ConfigFilterParser<Comparator, int>("roamingbees", ParseComparator, "comparator", ParseInt, "num", (result, op, num) => op(result.enemyResult.roamingBees, num)),
            new ConfigFilterParser<Comparator, float>("closestpumpkin", ParseComparator, "comparator", ParseFloat, "distance", (result, op, num) => result.levelResult.nearestPumpkins.Count > 0 && op(result.levelResult.nearestPumpkins.Min(x => x.Value), num)),
            new ConfigFilterParser<Comparator, int>("doors", ParseComparator, "comparator", ParseInt, "num", (result, op, num) => op(result.levelResult.numDoors, num)),
            new ConfigFilterParser<Comparator, int>("lockeddoors", ParseComparator, "comparator", ParseInt, "num", (result, op, num) => op(result.levelResult.numLockedDoors, num)),
            new ConfigFilterParser<Comparator, int>("lockedpowereddoors", ParseComparator, "comparator", ParseInt, "num", (result, op, num) => op(result.levelResult.numLockedBigDoors, num)),
            new ConfigFilterParser<Comparator, int>("valves", ParseComparator, "comparator", ParseInt, "num", (result, op, num) => op(result.levelResult.numValves, num)),
            new ConfigFilterParser<Comparator, int>("burstvalves", ParseComparator, "comparator", ParseInt, "num", (result, op, num) => op(result.levelResult.numBurstValves, num)),
            new ConfigFilterParser<Comparator, float>("highestroom", ParseComparator, "comparator", ParseFloat, "y", (result, op, num) => op(result.levelResult.highestRoom, num)),
            new ConfigFilterParser<Comparator, float>("nearestroomtomain", ParseComparator, "comparator", ParseFloat, "distance", (result, op, num) => op(result.levelResult.nearestRoomToMain, num)),
            new ConfigFilterParser<Comparator, float>("distancebetweenentrances", ParseComparator, "comparator", ParseFloat, "distance", (result, op, num) => { foreach (var item in result.levelResult.distanceBetweenEntrances)
                {
                    if(!op(item.Value, num)) {
                        return false;
                    }
                }
                return true;
            }),
            new ConfigFilterParser<EnemyType, Comparator, float>("closestnesttoentrance", ParseEnemy, "enemy", ParseComparator, "comparator", ParseFloat, "distance", (result, enemy, op, num) => result.levelResult.closestNestToEntrance.ContainsKey(enemy) && op(result.levelResult.closestNestToEntrance[enemy].Min(x => x.Value), num)),
            new ConfigFilterParser<EnemyType, Comparator, float>("closestnesttoship", ParseEnemy, "enemy", ParseComparator, "comparator", ParseFloat, "distance", (result, enemy, op, num) => result.levelResult.closestNestToShip.ContainsKey(enemy) && op(result.levelResult.closestNestToShip[enemy], num)),

            new ConfigMetaFilterParser("[", (config, stream) => {
                List<Predicate<CrackingResult>> filters1 = [];
                List<Predicate<CrackingResult>> filters2 = [];
                while (true) {
                    BaseConfigCommandParser? cmd = ParseCommand(config, filters1.Add, stream) ?? throw new Exception("Expected \"]or(\" line");
                    if (cmd.cmd == "]or(") {
                        break;
                    }
                }
                while (true) {
                    BaseConfigCommandParser? cmd = ParseCommand(config, filters2.Add, stream) ?? throw new Exception("Expected \")\" line");
                    if (cmd.cmd == ")") {
                        break;
                    }
                }
                return result => {
                    foreach (var filter in filters1) {
                        if (!filter(result)) {
                            goto FILTER2;
                        }
                    }
                    return true;
                FILTER2:
                    foreach (var filter in filters2) {
                        if (!filter(result)) {
                            return false;
                        }
                    }
                    return true;
                };
            }, "[asd\n<filters>\n]or(\n<filters>\n)"),
            new ConfigMarkerParser("]or("),
            new ConfigMarkerParser(")"),
        ];

        private static readonly Dictionary<string, Comparator> comparators = new()
        {
            [">"] = (x, y) => x > y,
            ["<"] = (x, y) => x < y,
            [">="] = (x, y) => x >= y,
            ["<="] = (x, y) => x <= y,
            ["=="] = (x, y) => x == y,
            ["!="] = (x, y) => x != y,
        };

        private static bool IContains(string text, string substr) => substr.Length > 0 && text.Contains(substr, StringComparison.InvariantCultureIgnoreCase);

        public Config(string fileName)
        {
            //ensure folder exists
            string folderPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "32121", "LethalSeedCracker");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            //create sample config
            string filePath = Path.Join(folderPath, fileName);
            LethalSeedCracker2.Logger.LogInfo($"Loading config: {filePath}");
            if (!File.Exists(filePath))
            {
                using StreamWriter file = new(File.Open(filePath, FileMode.Create));
                file.WriteLine("seeds 200 300 400");
                file.WriteLine("seedrange 0 100");
                file.WriteLine();
                file.WriteLine("moon experimentation");
                file.WriteLine();
                file.WriteLine("daystildeadline 1");
                file.WriteLine("dayssurvived 0");
                file.WriteLine();
                file.WriteLine("#infestation lootbug");
            }

            //read config
            {
                using StreamReader file = new(File.OpenRead(filePath));
                while (!file.EndOfStream)
                {
                    ParseCommand(this, filters.Add, file);
                }
            }

            if (currentLevel is null)
            {
                PrintMoons();
                throw new Exception("No moon set. Set the current moon using \"moon\" e.g. \"moon experimentation\"");
            }
            if (seeds.Count == 0)
            {
                PrintCommands();
                throw new Exception("No seeds.");
            }
            LethalSeedCracker2.Logger.LogInfo("Successfully loaded config");
        }

        private static BaseConfigCommandParser? ParseCommand(Config config, Action<Predicate<CrackingResult>> filters, TextReader stream)
        {
        LOOP:
            string[]? line = stream.ReadLine()?.Split('#')[0].Trim().Split();
            if (line is null)
            {
                return null;
            }
            if (line.Length == 0 || line.Length == 1 && line[0].Length == 0)
            {
                goto LOOP;
            }
            for (int i = 0; i < line.Length; ++i)
            {
                foreach (var item in colloquialNames)
                {
                    if (IContains(item.Key, line[i]))
                    {
                        LethalSeedCracker2.Logger.LogInfo($"Substituting {line[i]} to {item.Value}");
                        line[i] = item.Value;
                    }
                }
            }
            LethalSeedCracker2.Logger.LogInfo($"Processing line: {string.Join(" ", line)}");
            string cmd = line[0].ToLower();
            foreach (var item in commands)
            {
                if (item.cmd == cmd)
                {
                    item.Parse(config, filters, stream, line[1..]);
                    return item;
                }
            }
            PrintCommands();
            throw new Exception($"unknown command: {line[0]}");
        }

        internal bool Filter(CrackingResult result)
        {
            foreach (var filter in filters)
            {
                if (!filter(result))
                {
                    return false;
                }
            }
            return true;
        }

        private static SelectableLevel ParseMoon(Config _, string name)
        {
            foreach (var level in StartOfRound.Instance.levels)
            {
                if (IContains(level.name, name) || IContains(level.PlanetName, name) || IContains(level.sceneName, name))
                {
                    return level;
                }
            }
            PrintMoons();
            throw new Exception($"Unrecognized moon: {name}");
        }

        private static EnemyType ParseEnemy(Config config, string name)
        {
            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before configuring enemies.");
            }
            foreach (var item in config.currentLevel.Enemies)
            {
                if (IContains(item.enemyType.name, name) || IContains(item.enemyType.enemyName, name))
                {
                    return item.enemyType;
                }
            }
            foreach (var item in config.currentLevel.OutsideEnemies)
            {
                if (IContains(item.enemyType.name, name) || IContains(item.enemyType.enemyName, name))
                {
                    return item.enemyType;
                }
            }
            foreach (var item in config.currentLevel.DaytimeEnemies)
            {
                if (IContains(item.enemyType.name, name) || IContains(item.enemyType.enemyName, name))
                {
                    return item.enemyType;
                }
            }
            PrintEnemies(config);
            throw new Exception($"Unrecognized enemy: {name}");
        }

        private static Item ParseScrap(Config config, string name)
        {
            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before configuring scrap.");
            }
            foreach (var item in config.currentLevel.spawnableScrap)
            {
                if (IContains(item.spawnableItem.name, name) || IContains(item.spawnableItem.itemName, name))
                {
                    return item.spawnableItem;
                }
            }
            PrintScrap(config);
            throw new Exception($"Unrecognized scrap: {name}");
        }

        private static Comparator ParseComparator(Config _, string op)
        {
            if (!comparators.ContainsKey(op))
            {
                PrintComparators();
                throw new Exception($"Unrecognized operator: {op}");
            }
            return comparators[op];
        }

        private static T ParseEnum<T>(Config _, string name) where T : Enum
        {
            foreach (var item in (T[])Enum.GetValues(typeof(T)))
            {
                if (IContains(item.ToString(), name))
                {
                    return item;
                }
            }
            PrintEnums<T>();
            throw new Exception($"Unrecognized {typeof(T).Name}: {name}");
        }

        private static CompanyMood ParseCompanyMood(Config _, string name)
        {
            foreach (var item in TimeOfDay.Instance.CommonCompanyMoods)
            {
                if (IContains(item.name, name))
                {
                    return item;
                }
            }
            PrintCompanyMoods();
            throw new Exception($"Unrecognized company mood: {name}");
        }

        private static string ParseOutsideObject(Config config, string name)
        {
            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before configuring traps.");
            }
            foreach (var item in config.currentLevel.spawnableOutsideObjects)
            {
                if (IContains(item.spawnableObject.prefabToSpawn.name, name))
                {
                    return item.spawnableObject.prefabToSpawn.name;
                }
            }
            if (IContains(RoundManager.Instance.quicksandPrefab.name, name))
            {
                return RoundManager.Instance.quicksandPrefab.name;
            }
            PrintOutsideObjects(config);
            throw new Exception($"Unrecognized trap: {name}");
        }

        private static void PrintCommands()
        {
            LethalSeedCracker2.Logger.LogInfo("Commands:");
            foreach (var item in commands)
            {
                LethalSeedCracker2.Logger.LogInfo($"  {item}");
            }
        }

        private static void PrintMoons()
        {
            LethalSeedCracker2.Logger.LogInfo("Moons:");
            foreach (var level in StartOfRound.Instance.levels)
            {
                LethalSeedCracker2.Logger.LogInfo($"  {level.name}, {level.PlanetName}, {level.sceneName}");
            }
        }

        private static void PrintEnemies(Config config)
        {
            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before listing enemies.");
            }
            LethalSeedCracker2.Logger.LogInfo("Enemies:");
            foreach (var item in config.currentLevel.Enemies)
            {
                LethalSeedCracker2.Logger.LogInfo($"  {item.enemyType.name}, {item.enemyType.enemyName}");
            }
            foreach (var item in config.currentLevel.OutsideEnemies)
            {
                LethalSeedCracker2.Logger.LogInfo($"  {item.enemyType.name}, {item.enemyType.enemyName}");
            }
            foreach (var item in config.currentLevel.DaytimeEnemies)
            {
                LethalSeedCracker2.Logger.LogInfo($"  {item.enemyType.name}, {item.enemyType.enemyName}");
            }
        }

        private static void PrintScrap(Config config)
        {
            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before listing scrap.");
            }
            LethalSeedCracker2.Logger.LogInfo("Scrap:");
            foreach (var item in config.currentLevel.spawnableScrap)
            {
                LethalSeedCracker2.Logger.LogInfo($"  {item.spawnableItem.name}, {item.spawnableItem.itemName}");
            }
        }

        private static void PrintComparators()
        {
            LethalSeedCracker2.Logger.LogInfo("Comparators:");
            foreach (var item in comparators)
            {
                LethalSeedCracker2.Logger.LogInfo($"  {item.Key}");
            }
        }

        private static void PrintEnums<T>() where T : Enum
        {
            LethalSeedCracker2.Logger.LogInfo($"{typeof(T).Name}s:");
            foreach (var item in (T[])Enum.GetValues(typeof(T)))
            {
                LethalSeedCracker2.Logger.LogInfo($"  {item}");
            }
        }

        private static void PrintCompanyMoods()
        {
            LethalSeedCracker2.Logger.LogInfo("Company moods:");
            foreach (var item in TimeOfDay.Instance.CommonCompanyMoods)
            {
                LethalSeedCracker2.Logger.LogInfo($"{item}: timeToWaitBeforeGrabbingItem: {item.timeToWaitBeforeGrabbingItem}, irritability: {item.irritability}, judgementSpeed: {item.judgementSpeed}, startingPatience: {item.startingPatience}, mustBeWokenUp: {item.mustBeWokenUp}, maximumItemsToAnger: {item.maximumItemsToAnger}, sensitivity: {item.sensitivity}, manifestation: {item.manifestation}");
            }
        }

        private static void PrintOutsideObjects(Config config)
        {
            LethalSeedCracker2.Logger.LogInfo("Outside objects:");
            foreach (var item in config.currentLevel.spawnableOutsideObjects)
            {
                LethalSeedCracker2.Logger.LogInfo($"{item.spawnableObject.prefabToSpawn.name}");
            }
            LethalSeedCracker2.Logger.LogInfo($"{RoundManager.Instance.quicksandPrefab.name}");
        }

        //Find the smallest int that satisfies f
        private static int MinNum(Func<float, bool> f)
        {
            if (f(0))
            {
                return 0;
            }
            int l = 0;
            int r = 1;
            while (!f(r))
            {
                l = r;
                r *= 2;
            }
            while (l < r)
            {
                int m = (l + r) / 2;
                if (f(m))
                {
                    r = m - 1;
                }
                else
                {
                    l = m + 1;
                }
            }
            if (f(l))
            {
                return l;
            }
            else
            {
                return l - 1;
            }
        }

        private static void CheckEnemyPower(Config config, EnemyType enemy, Comparator op, int num)
        {
            config.enemyConstraints.Add(new(enemy, op, num));

            Action<string> handleMsg = config.ignorepower ? LethalSeedCracker2.Logger.LogWarning : s => throw new Exception(s);

            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before checking enemies.");
            }
            float insidePower = 0;
            float outsidePower = 0;
            float daytimePower = 0;

            foreach (var constraint in config.enemyConstraints)
            {
                int count = MinNum(x => constraint.Item2(x, constraint.Item3));
                if (count > constraint.Item1.MaxCount)
                {
                    handleMsg($"Requested at least {count} {constraint.Item1.name}, but {constraint.Item1.MaxCount} is the maximum.");
                }

                foreach (var item in config.currentLevel.Enemies)
                {
                    if (item.enemyType == constraint.Item1)
                    {
                        insidePower += item.enemyType.PowerLevel * count;
                    }
                }
                foreach (var item in config.currentLevel.OutsideEnemies)
                {
                    if (item.enemyType == constraint.Item1)
                    {
                        outsidePower += item.enemyType.PowerLevel * count;
                    }
                }
                foreach (var item in config.currentLevel.DaytimeEnemies)
                {
                    if (item.enemyType == constraint.Item1)
                    {
                        daytimePower += item.enemyType.PowerLevel * count;
                    }
                }
            }
            float maxInsidePower = 30f;
            float maxOutsidePower = config.currentLevel.maxOutsideEnemyPowerCount;
            float maxDaytimePower = config.currentLevel.maxDaytimeEnemyPowerCount;
            if (insidePower > maxInsidePower)
            {
                handleMsg($"Inside power required ({insidePower}) exceeds max possible limit ({maxInsidePower}).");
            }
            if (outsidePower > maxOutsidePower)
            {
                handleMsg($"Outside power required ({outsidePower}) exceeds max possible limit ({maxOutsidePower}).");
            }
            if (daytimePower > maxDaytimePower)
            {
                handleMsg($"Daytime power required ({daytimePower}) exceeds max possible limit ({maxDaytimePower}).");
            }
        }
    }
}
