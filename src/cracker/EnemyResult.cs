using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalSeedCracker2.src.cracker
{
    internal class EnemyResult
    {
        internal EnemyType? infestation;
        internal Dictionary<EnemyType, int> enemyCounts;
        internal bool indoorFog;

        public EnemyResult()
        {
            if (RoundManager.Instance.enemyRushIndex != -1)
            {
                infestation = RoundManager.Instance.currentLevel.Enemies[RoundManager.Instance.enemyRushIndex].enemyType;
            }
            indoorFog = RoundManager.Instance.indoorFog.isActiveAndEnabled;
            enemyCounts = [];
            var enemies = Object.FindObjectsOfType<EnemyAI>();
            foreach (var enemy in enemies)
            {
                int count = enemyCounts.GetValueOrDefault(enemy.enemyType);
                enemyCounts[enemy.enemyType] = count + 1;
            }
        }

        public override string ToString()
        {
            string enemyList = string.Join(", ", [.. from item in enemyCounts select $"{item.Key.name}: {item.Value}"]);
            return $"indoor fog: {indoorFog}, infestation: {infestation?.enemyName ?? "none"}\n  enemies: [{enemyList}]";
        }
    }
}
