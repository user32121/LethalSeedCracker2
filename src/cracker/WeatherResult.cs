using LethalSeedCracker2.src.config;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalSeedCracker2.src.cracker
{
    internal class WeatherResult
    {
        internal Dictionary<SelectableLevel, LevelWeatherType> weathers;

        public WeatherResult(Config config)
        {
            weathers = [];
            if (!config.skipWeather)
            {
                var levels = StartOfRound.Instance.levels;
                for (int i = 0; i < levels.Length; i++)
                {
                    levels[i].currentWeather = LevelWeatherType.None;
                    if (levels[i].overrideWeather)
                    {
                        levels[i].currentWeather = levels[i].overrideWeatherType;
                    }
                }
                System.Random random = new(StartOfRound.Instance.randomMapSeed + 35);
                List<SelectableLevel> list = [.. levels];
                float num = 1f;
                int daysPlayersSurvivedInARow = StartOfRound.Instance.daysPlayersSurvivedInARow + 1;
                if (daysPlayersSurvivedInARow > 2 && daysPlayersSurvivedInARow % 3 == 0)
                {
                    num = random.Next(15, 25) / 10f;
                }
                float num2 = Mathf.Clamp(StartOfRound.Instance.planetsWeatherRandomCurve.Evaluate((float)random.NextDouble()) * num, 0f, 1f);
                int num3 = Mathf.Clamp((int)(num2 * (levels.Length - 2f)), 0, levels.Length);
                for (int j = 0; j < num3; j++)
                {
                    SelectableLevel selectableLevel = list[random.Next(0, list.Count)];
                    if (selectableLevel.randomWeathers != null && selectableLevel.randomWeathers.Length != 0)
                    {
                        selectableLevel.currentWeather = selectableLevel.randomWeathers[random.Next(0, selectableLevel.randomWeathers.Length)].weatherType;
                        weathers[selectableLevel] = selectableLevel.currentWeather;
                    }
                    list.Remove(selectableLevel);
                }
            }
        }

        public override string ToString()
        {
            string weatherList = string.Join(", ", [.. from item in weathers select $"{item.Key.name}: {item.Value}"]);
            return $"weather: [{weatherList}]";
        }
    }
}