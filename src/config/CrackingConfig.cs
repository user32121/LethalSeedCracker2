using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using LethalSeedCracker2.src.cracker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static LethalSeedCracker2.src.cracker.Defines;

namespace LethalSeedCracker2.src.config
{
    internal class CrackingConfigq
    {
        //parameters
        internal int curSeedIdx = 0;
        internal List<int> seeds = [];
        internal SelectableLevel currentLevel;
        internal int daysUntilDeadline = 3;
        internal int daysPlayersSurvivedInARow = 0;

        //filters
        internal EnemyType? infestation;
        internal DUNGEON dungeon = DUNGEON.INVALID;
        internal bool meteor = false;
        private readonly bool ignorepower;
        private readonly bool blackout = false;
        internal List<Tuple<LevelWeatherType, SelectableLevel>> weathers = [];
        internal List<Tuple<EnemyType, int>> enemies = [];
        internal List<Tuple<Item, int>> scraps = [];
        internal List<Tuple<TRAP, int>> traps = [];
        internal List<Tuple<string, int>> outsideObjects = [];
        private readonly bool indoorFog;
        internal bool eclipsed;
        private readonly CompanyMood? companyMood;
        private readonly List<Tuple<EnemyType, int>> maxEnemies = [];
        private readonly Predicate<float>? closestTrap;

        //convenience name mappings
        private static readonly Dictionary<string, string> colloquialNames = new()
        {
            ["oldbird"] = "radmech",
            ["bracken"] = "flowerman",
            ["lootbug"] = "hoarderbug",
            ["mimic"] = "masked",
        };

        private static bool IContains(string text, string substr) => substr.Length > 0 && text.Contains(substr, StringComparison.InvariantCultureIgnoreCase);

        public CrackingConfigq(string fileName)
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
                    switch (line[0].ToLower())
                    {
                        case "seeds":
                            seeds.AddRange(from seed in line[1..] select int.Parse(seed));
                            break;
                        case "seedrange":
                            int num1 = int.Parse(line[1]);
                            int num2 = int.Parse(line[2]);
                            int minSeed = Math.Min(num1, num2);
                            int maxSeed = Math.Max(num1, num2);
                            seeds.AddRange(Enumerable.Range(minSeed, maxSeed - minSeed + 1));
                            break;
                        case "daystildeadline":
                            daysUntilDeadline = int.Parse(line[1]);
                            break;
                        case "dayssurvived":
                            daysPlayersSurvivedInARow = int.Parse(line[1]);
                            break;
                        case "moon":
                            currentLevel = ParseMoon(line[1]);
                            break;
                        case "dungeon":
                            dungeon = ParseEnum<DUNGEON>(line[1]);
                            break;
                        case "infestation":
                            infestation = ParseEnemy(line[1]);
                            break;
                        case "meteor":
                            meteor = true;
                            break;
                        case "weather":
                            var weather = ParseEnum<LevelWeatherType>(line[1]);
                            var moon = ParseMoon(line[2]);
                            weathers.Add(new(weather, moon));
                            break;
                        case "enemy":
                            var enemy = ParseEnemy(line[1]);
                            var count = int.Parse(line[2]);
                            if (count > enemy.MaxCount)
                            {
                                ReportConfigIssue($"Requested {count} {enemy.name}, but {enemy.MaxCount} is the maximum. Set ignorepower to proceed anyways.", ignorepower);
                            }
                            enemies.Add(new(enemy, count));
                            CheckEnemyPower();
                            break;
                        case "scrap":
                            var scrap = ParseScrap(line[1]);
                            count = int.Parse(line[2]);
                            scraps.Add(new(scrap, count));
                            break;
                        case "trap":
                            var trap = ParseEnum<TRAP>(line[1]);
                            count = int.Parse(line[2]);
                            traps.Add(new(trap, count));
                            break;
                        case "ignorepower":
                            ignorepower = true;
                            break;
                        case "outsideobject":
                            var outsideobject = ParseOutsideObject(line[1]);
                            count = int.Parse(line[2]);
                            outsideObjects.Add(new(outsideobject, count));
                            break;
                        case "maxenemy":
                            enemy = ParseEnemy(line[1]);
                            count = int.Parse(line[2]);
                            maxEnemies.Add(new(enemy, count));
                            break;
                        case "blackout":
                            blackout = true;
                            break;
                        case "companymood":
                            companyMood = ParseCompanyMood(line[1]);
                            break;
                        case "indoorfog":
                            indoorFog = true;
                            break;
                        case "eclipsed":
                            eclipsed = true;
                            break;
                        case "closesttrap":
                            var comparator = ParseComparator(line[1]);
                            var dist = float.Parse(line[2]);
                            closestTrap = x => comparator(x, dist);
                            break;
                        default:
                            throw new Exception($"unknown command: {line[0]}");
                    }
                }
            }

            if (currentLevel is null)
            {
                PrintMoons();
                throw new Exception("No moon set. Set the current moon using \"moon\" e.g. \"moon experimentation\"");
            }
        }

        private Func<float, float, bool> ParseComparator(string op)
        {
            return op switch
            {
                ">" => (x, y) => x > y,
                "<" => (x, y) => x < y,
                ">=" => (x, y) => x >= y,
                "<=" => (x, y) => x <= y,
                "=" => (x, y) => x == y,
                _ => throw new Exception($"Unrecognized operator: {op}"),
            };
        }

        private CompanyMood ParseCompanyMood(string name)
        {
            foreach (var item in TimeOfDay.Instance.CommonCompanyMoods)
            {
                if (IContains(item.name, name))
                {
                    return item;
                }
            }
            LethalSeedCracker2.Logger.LogInfo("Company moods:");
            foreach (var item in TimeOfDay.Instance.CommonCompanyMoods)
            {
                LethalSeedCracker2.Logger.LogInfo($"{item}: timeToWaitBeforeGrabbingItem: {item.timeToWaitBeforeGrabbingItem}, irritability: {item.irritability}, judgementSpeed: {item.judgementSpeed}, startingPatience: {item.startingPatience}, mustBeWokenUp: {item.mustBeWokenUp}, maximumItemsToAnger: {item.maximumItemsToAnger}, sensitivity: {item.sensitivity}, manifestation: {item.manifestation}");
            }
            throw new Exception($"Unrecognized company mood: {name}");
        }

        private string ParseOutsideObject(string name)
        {
            if (currentLevel is null)
            {
                throw new Exception("Set a moon before configuring enemies.");
            }
            foreach (var item in currentLevel.spawnableOutsideObjects)
            {
                if (IContains(item.spawnableObject.name, name) || IContains(item.spawnableObject.prefabToSpawn.name, name))
                {
                    return item.spawnableObject.name;
                }
            }
            LethalSeedCracker2.Logger.LogInfo("Outside objects:");
            foreach (var item in currentLevel.spawnableOutsideObjects)
            {
                LethalSeedCracker2.Logger.LogInfo($"{item.spawnableObject.name}, {item.spawnableObject.prefabToSpawn.name}");
            }
            throw new Exception($"Unrecognized outside object: {name}");
        }

        private void ReportConfigIssue(string message, bool ignorepower)
        {
            if (ignorepower)
            {
                LethalSeedCracker2.Logger.LogWarning(message);
            }
            else
            {
                throw new Exception(message);
            }
        }

        private void CheckEnemyPower()
        {
            if (currentLevel is null)
            {
                throw new Exception("Set a moon before checking enemies.");
            }
            float insidePower = 0;
            float outsidePower = 0;
            float daytimePower = 0;
            foreach (var enemy in enemies)
            {
                foreach (var item in currentLevel.Enemies)
                {
                    if (item.enemyType == enemy.Item1)
                    {
                        insidePower += item.enemyType.PowerLevel * enemy.Item2;
                    }
                }
                foreach (var item in currentLevel.OutsideEnemies)
                {
                    if (item.enemyType == enemy.Item1)
                    {
                        outsidePower += item.enemyType.PowerLevel * enemy.Item2;
                    }
                }
                foreach (var item in currentLevel.DaytimeEnemies)
                {
                    if (item.enemyType == enemy.Item1)
                    {
                        daytimePower += item.enemyType.PowerLevel * enemy.Item2;
                    }
                }
            }
            float maxInsidePower = 30f;
            float maxOutsidePower = currentLevel.maxOutsideEnemyPowerCount;
            float maxDaytimePower = currentLevel.maxDaytimeEnemyPowerCount;
            if (insidePower > maxInsidePower)
            {
                ReportConfigIssue($"Inside power required ({insidePower}) exceeds max possible limit ({maxInsidePower}). Set ignorepower to proceed anyways.", ignorepower);
            }
            if (outsidePower > maxOutsidePower)
            {
                ReportConfigIssue($"Outside power required ({outsidePower}) exceeds max possible limit ({maxOutsidePower}). Set ignorepower to proceed anyways.", ignorepower);
            }
            if (daytimePower > maxDaytimePower)
            {
                ReportConfigIssue($"Daytime power required ({daytimePower}) exceeds max possible limit ({maxDaytimePower}). Set ignorepower to proceed anyways.", ignorepower);
            }
        }

        internal bool CheckFilter(CrackingResult result)
        {
            if (dungeon != DUNGEON.INVALID && dungeon != result.levelResult.currentDungeonType)
            {
                return false;
            }
            if (infestation != null && infestation != result.enemyResult.infestation)
            {
                return false;
            }
            if (meteor && !result.levelResult.meteor)
            {
                return false;
            }
            if (blackout && !result.levelResult.blackout)
            {
                return false;
            }
            if (companyMood != null && companyMood != result.levelResult.currentCompanyMood)
            {
                return false;
            }
            if (indoorFog && !result.enemyResult.indoorFog)
            {
                return false;
            }
            if (closestTrap != null)
            {
                var closest = result.levelResult.nearestEntranceTraps.MinBy(x => x.Value.Item2);
                if (!closestTrap(closest.Value.Item2))
                {
                    return false;
                }
            }
            foreach (var item in weathers)
            {
                if (result.weatherResult.weathers.GetValueOrDefault(item.Item2, (LevelWeatherType)(-11)) != item.Item1)
                {
                    return false;
                }
            }
            foreach (var item in enemies)
            {
                if (result.enemyResult.enemyCounts.GetValueOrDefault(item.Item1) < item.Item2)
                {
                    return false;
                }
            }
            foreach (var item in scraps)
            {
                if (result.scrapResult.scrapCounts.GetValueOrDefault(item.Item1) < item.Item2)
                {
                    return false;
                }
            }
            foreach (var item in traps)
            {
                if (result.levelResult.trapCounts.GetValueOrDefault(item.Item1) < item.Item2)
                {
                    return false;
                }
            }
            foreach (var item in maxEnemies)
            {
                if (result.enemyResult.enemyCounts.GetValueOrDefault(item.Item1, 0) > item.Item2)
                {
                    return false;
                }
            }
            return true;
        }

        private static void PrintMoons()
        {
            LethalSeedCracker2.Logger.LogInfo("Moons:");
            foreach (var level in StartOfRound.Instance.levels)
            {
                LethalSeedCracker2.Logger.LogInfo($"{level.name}, {level.PlanetName}, {level.sceneName}");
            }
        }

        private static SelectableLevel ParseMoon(string name)
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

        private T ParseEnum<T>(string name) where T : Enum
        {
            foreach (var item in (T[])Enum.GetValues(typeof(T)))
            {
                if (IContains(item.ToString(), name))
                {
                    return item;
                }
            }
            LethalSeedCracker2.Logger.LogInfo($"{typeof(T).Name}s:");
            foreach (var item in (T[])Enum.GetValues(typeof(T)))
            {
                LethalSeedCracker2.Logger.LogInfo($"  {item}");
            }
            throw new Exception($"Unrecognized {typeof(T).Name}: {name}");
        }

        private EnemyType ParseEnemy(string name)
        {
            if (currentLevel is null)
            {
                throw new Exception("Set a moon before configuring enemies.");
            }
            foreach (var item in currentLevel.Enemies)
            {
                if (IContains(item.enemyType.name, name) || IContains(item.enemyType.enemyName, name))
                {
                    return item.enemyType;
                }
            }
            foreach (var item in currentLevel.OutsideEnemies)
            {
                if (IContains(item.enemyType.name, name) || IContains(item.enemyType.enemyName, name))
                {
                    return item.enemyType;
                }
            }
            foreach (var item in currentLevel.DaytimeEnemies)
            {
                if (IContains(item.enemyType.name, name) || IContains(item.enemyType.enemyName, name))
                {
                    return item.enemyType;
                }
            }
            LethalSeedCracker2.Logger.LogInfo("Enemies:");
            foreach (var item in currentLevel.Enemies)
            {
                LethalSeedCracker2.Logger.LogInfo($"{item.enemyType.name}, {item.enemyType.enemyName}");
            }
            foreach (var item in currentLevel.OutsideEnemies)
            {
                LethalSeedCracker2.Logger.LogInfo($"{item.enemyType.name}, {item.enemyType.enemyName}");
            }
            foreach (var item in currentLevel.DaytimeEnemies)
            {
                LethalSeedCracker2.Logger.LogInfo($"{item.enemyType.name}, {item.enemyType.enemyName}");
            }
            throw new Exception($"Unrecognized enemy: {name}");
        }

        private Item ParseScrap(string name)
        {
            if (currentLevel is null)
            {
                throw new Exception("Set a moon before configuring scrap.");
            }
            foreach (var item in currentLevel.spawnableScrap)
            {
                if (IContains(item.spawnableItem.name, name) || IContains(item.spawnableItem.itemName, name))
                {
                    return item.spawnableItem;
                }
            }
            LethalSeedCracker2.Logger.LogInfo("Scrap:");
            foreach (var item in currentLevel.spawnableScrap)
            {
                LethalSeedCracker2.Logger.LogInfo($"{item.spawnableItem.name}, {item.spawnableItem.itemName}");
            }
            throw new Exception($"Unrecognized scrap: {name}");
        }
    }
}
