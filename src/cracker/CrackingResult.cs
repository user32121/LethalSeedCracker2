using System;
using System.IO;

namespace LethalSeedCracker2.src.cracker
{
    internal class CrackingResult
    {
        internal int seed;
        internal SelectableLevel currentLevel;
        internal int daysUntilDeadline;
        internal int daysPlayersSurvivedInARow;
        internal bool eclipsed;
        internal LevelResult levelResult;
        internal ScrapResult scrapResult;
        internal EnemyResult enemyResult;
        internal WeatherResult weatherResult;

        public CrackingResult()
        {
            seed = StartOfRound.Instance.randomMapSeed;
            currentLevel = StartOfRound.Instance.currentLevel;
            daysUntilDeadline = TimeOfDay.Instance.daysUntilDeadline;
            daysPlayersSurvivedInARow = StartOfRound.Instance.daysPlayersSurvivedInARow;
            eclipsed = TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Eclipsed;
            levelResult = new();
            scrapResult = new();
            enemyResult = new();
            weatherResult = new();
        }

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
