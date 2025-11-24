using LethalSeedCracker2.Patches;
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
        internal int roamingBees;

        public EnemyResult()
        {
            if (RoundManager.Instance.enemyRushIndex != -1)
            {
                infestation = RoundManager.Instance.currentLevel.Enemies[RoundManager.Instance.enemyRushIndex].enemyType;
            }
            indoorFog = RoundManager.Instance.indoorFog.isActiveAndEnabled;
            enemyCounts = [];
            foreach (var enemy in Object.FindObjectsOfType<EnemyAI>())
            {
                int count = enemyCounts.GetValueOrDefault(enemy.enemyType);
                enemyCounts[enemy.enemyType] = count + 1;
            }

            roamingBees = BeeState.roamingBees.Count;
            BeeState.roamingBees.Clear();
        }

        public override string ToString()
        {
            string enemyList = string.Join(", ", [.. from item in enemyCounts select $"{item.Key.name}: {item.Value}"]);
            return $"indoor fog: {indoorFog}, infestation: {infestation?.enemyName ?? "none"}, roamingbees: {roamingBees}\n  enemies: [{enemyList}]";
        }
    }
}
