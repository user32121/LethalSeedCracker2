using LethalSeedCracker2.src.config;
using System;
using System.IO;

namespace LethalSeedCracker2.src.cracker
{
    internal class CrackingResult(Config config)
    {
        internal int seed = StartOfRound.Instance.randomMapSeed;
        internal SelectableLevel currentLevel = StartOfRound.Instance.currentLevel;
        internal int daysUntilDeadline = TimeOfDay.Instance.daysUntilDeadline;
        internal int daysPlayersSurvivedInARow = StartOfRound.Instance.daysPlayersSurvivedInARow;
        internal bool eclipsed = TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Eclipsed;
        internal LevelResult levelResult = new(config);
        internal ScrapResult scrapResult = new(config);
        internal EnemyResult enemyResult = new(config);
        internal WeatherResult weatherResult = new(config);

        internal void Save(string fileName, bool append)
        {
            string folderPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "32121", "LethalSeedCracker");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string filePath = Path.Join(folderPath, fileName);
            LethalSeedCracker2.Logger.LogInfo($"writing seed to: {filePath}");
            using StreamWriter file = new(File.Open(filePath, append ? FileMode.Append : FileMode.Create));
            file.WriteLine(this + "\n");
        }

        public override string ToString()
        {
            return $"seed: {seed}, moon: {currentLevel.name}, eclipsed: {eclipsed}, daystildeadline: {daysUntilDeadline}, dayssurvived: {daysPlayersSurvivedInARow}\n  {levelResult}\n  {scrapResult}\n  {enemyResult}\n  {weatherResult}";
        }
    }
}
