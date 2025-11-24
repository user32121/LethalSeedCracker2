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

        internal int foundSeeds = 0;

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
        private readonly List<BaseConfigCommand> commands =
        [
            new ConfigParameter<int>("seed", ParseInt, "seed", (config, seed) => config.seeds.Add(seed)),
            new ConfigParameter<int, int>("seedrange", ParseInt, "min", ParseInt, "max", (config, min, max) => config.seeds.AddRange(Enumerable.Range(min, max - min + 1))),
            new ConfigParameter<int>("daystildeadline", ParseInt, "days", (config, days) => config.daysUntilDeadline = days),
            new ConfigParameter<int>("dayssurvived", ParseInt, "days", (config, days) => config.daysPlayersSurvivedInARow = days),
            new ConfigParameter<SelectableLevel>("moon", ParseMoon, "moon", (config, moon) => config.currentLevel = moon),
            new ConfigParameter("eclipsed", config => config.eclipsed = true),
            new ConfigParameter("ignorepower", config => config.ignorepower = true),
            new ConfigParameter("skipenemies", config => config.skipEnemies = true),
            new ConfigParameter("skiptraps", config => config.skipTraps = true),
            new ConfigParameter("skipoutsideobjects", config => config.skipOutsideObjects = true),
            new ConfigParameter("skipscrap", config => config.skipScrap = true),
            new ConfigParameter("skipweather", config => config.skipWeather = true),

            new ConfigFilter<Defines.DUNGEON>("dungeon", ParseEnum<Defines.DUNGEON>, Defines.DUNGEON.INVALID, "dungeon", (result, dungeon) => dungeon == Defines.DUNGEON.INVALID || dungeon == result.levelResult.currentDungeonType),
            new ConfigFilter<EnemyType?>("infestation", ParseEnemy, null, "enemy", (result, enemy) => enemy == null || enemy == result.enemyResult.infestation),
            new ConfigFilter("meteor", (result, meteor) => !meteor || result.levelResult.meteor),
            new ConfigFilters<LevelWeatherType, SelectableLevel>("weather", ParseEnum<LevelWeatherType>, "weather", ParseMoon, "moon", (result, weathers, moons) => {
                for (int i = 0; i < weathers.Count; ++i) {
                    if (result.weatherResult.weathers.GetValueOrDefault(moons[i], LevelWeatherType.None) != weathers[i]) {
                        return false;
                    }
                }
                return true;
            }),
            new ConfigFilters<EnemyType, Func<float, float, bool>, int>("enemy", ParseEnemy, "enemy", ParseComparator, "comparator", ParseInt, "num", (result, enemies, ops, nums) => {
                for (int i = 0; i < enemies.Count; ++i) {
                    if (!ops[i](result.enemyResult.enemyCounts.GetValueOrDefault(enemies[i], 0), nums[i])) {
                        return false;
                    }
                }
                return true;
            }, CheckEnemyPower),
            new ConfigFilters<Item, Func<float, float, bool>, int>("scrap", ParseScrap, "scrap", ParseComparator, "comparator", ParseInt, "num", (result, scraps, ops, nums) => {
                for (int i = 0; i < scraps.Count; ++i) {
                    if (!ops[i](result.scrapResult.scrapCounts.GetValueOrDefault(scraps[i], 0), nums[i])) {
                        return false;
                    }
                }
                return true;
            }),
            new ConfigFilters<Defines.TRAP, Func<float, float, bool>, int>("trap", ParseEnum<Defines.TRAP>, "trap", ParseComparator, "comparator", ParseInt, "num", (result, traps, ops, nums) => {
                for (int i = 0; i < traps.Count; ++i) {
                    if (!ops[i](result.levelResult.trapCounts.GetValueOrDefault(traps[i], 0), nums[i])) {
                        return false;
                    }
                }
                return true;
            }),
            new ConfigFilters<string, Func<float, float, bool>, int>("outsideobject", ParseOutsideObject, "object", ParseComparator, "comparator", ParseInt, "num", (result, objs, ops, nums) => {
                for (int i = 0; i < objs.Count; ++i) {
                    if (!ops[i](result.levelResult.outsideObjectCounts.GetValueOrDefault(objs[i], 0), nums[i])) {
                        return false;
                    }
                }
                return true;
            }),
            new ConfigFilter("blackout", (result, blackout) => !blackout || result.levelResult.blackout),
            new ConfigFilter<CompanyMood?>("companymood", ParseCompanyMood, null, "mood", (result, mood) => mood == null || mood == result.levelResult.currentCompanyMood),
            new ConfigFilter("indoorfog", (result, indoorfog) => !indoorfog || result.enemyResult.indoorFog),
            new ConfigFilter<Func<float, float, bool>?, float>("closesttrap", ParseComparator, null, "comparator", ParseFloat, 0, "num", (result, op, num) => op == null || op(result.levelResult.nearestEntranceTraps.Min(x => x.Value.Item2), num)),
            new ConfigFilter<Func<float, float, bool>?, int>("roamingbees", ParseComparator, null, "comparator", ParseInt, 0, "num", (result, op, num) => op == null || op(result.enemyResult.roamingBees, num)),
        ];

        private static readonly Dictionary<string, Func<float, float, bool>> comparators = new()
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
                    string[] line = file.ReadLine().Split('#')[0].Trim().Split();
                    if (line.Length == 0 || line.Length == 1 && line[0].Length == 0)
                    {
                        continue;
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
                            item.Parse(this, line[1..]);
                            goto PARSED_COMMAND;
                        }
                    }
                    PrintCommands();
                    throw new Exception($"unknown command: {line[0]}");
                PARSED_COMMAND:;
                }
            }

            if (currentLevel is null)
            {
                PrintMoons();
                throw new Exception("No moon set. Set the current moon using \"moon\" e.g. \"moon experimentation\"");
            }
            LethalSeedCracker2.Logger.LogInfo("Successfully loaded config");
        }

        internal bool Filter(CrackingResult result)
        {
            foreach (var item in commands)
            {
                if (item is IConfigFilter cf)
                {
                    if (!cf.Filter(result))
                    {
                        return false;
                    }
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

        private static Func<float, float, bool> ParseComparator(Config _, string op)
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

        private void PrintCommands()
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

        private static void PrintTraps(Config config)
        {
            LethalSeedCracker2.Logger.LogInfo("Traps:");
            foreach (var item in config.currentLevel.spawnableScrap)
            {
                LethalSeedCracker2.Logger.LogInfo($"{item.spawnableItem.name}, {item.spawnableItem.itemName}");
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

        private static void CheckEnemyPower(Config config, List<EnemyType> enemies, List<Func<float, float, bool>> ops, List<int> nums)
        {
            Action<string> handleMsg;
            if (config.ignorepower)
            {
                handleMsg = LethalSeedCracker2.Logger.LogWarning;
            }
            else
            {
                handleMsg = s => throw new Exception(s);
            }

            if (config.currentLevel is null)
            {
                throw new Exception("Set a moon before checking enemies.");
            }
            float insidePower = 0;
            float outsidePower = 0;
            float daytimePower = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                int count = MinNum(x => ops[i](x, nums[i]));
                EnemyType enemy = enemies[i];
                if (count > enemy.MaxCount)
                {
                    handleMsg($"Requested at least {count} {enemy.name}, but {enemy.MaxCount} is the maximum.");
                }

                foreach (var item in config.currentLevel.Enemies)
                {
                    if (item.enemyType == enemy)
                    {
                        insidePower += item.enemyType.PowerLevel * count;
                    }
                }
                foreach (var item in config.currentLevel.OutsideEnemies)
                {
                    if (item.enemyType == enemy)
                    {
                        outsidePower += item.enemyType.PowerLevel * count;
                    }
                }
                foreach (var item in config.currentLevel.DaytimeEnemies)
                {
                    if (item.enemyType == enemy)
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
