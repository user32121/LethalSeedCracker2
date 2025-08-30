using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LethalSeedCracker2.Patches.Defines;

namespace LethalSeedCracker2.Patches
{
    internal class LevelResult
    {
        internal DUNGEON currentDungeonType;
        internal int numRooms;
        internal bool meteor;
        internal float meteorShowerAtTime;
        internal Dictionary<TRAP, int> trapCounts = [];
        internal int numLockedDoors;
        internal Dictionary<string, int> outsideObjectCounts = [];
        internal bool blackout;
        internal CompanyMood currentCompanyMood;
        internal int numMeteors;
        internal int numVents;
        internal Dictionary<EntranceTeleport, Tuple<Component, float>> nearestEntranceTraps = [];

        public LevelResult()
        {
            currentDungeonType = RoundManager.Instance.currentDungeonType switch
            {
                -1 => DUNGEON.NONE,
                0 => DUNGEON.FACTORY,
                1 => DUNGEON.MANSION,
                2 => DUNGEON.FACTORY,
                3 => DUNGEON.FACTORY,
                4 => DUNGEON.MINESHAFT,
                _ => throw new NotImplementedException($"{RoundManager.Instance.currentDungeonType}")
            };
            numRooms = RoundManager.Instance.dungeonGenerator?.Generator.GenerationStats.TotalRoomCount ?? 0;
            meteor = TimeOfDay.Instance.MeteorWeather.meteorsEnabled;
            numLockedDoors = UnityEngine.Object.FindObjectsOfType<DoorLock>().Length;
            meteorShowerAtTime = TimeOfDayPatch.meteorShowerAtTime;
            blackout = !UnityEngine.Object.FindObjectOfType<BreakerBox>()?.isPowerOn ?? false;
            currentCompanyMood = TimeOfDay.Instance.currentCompanyMood;
            numMeteors = MeteorShowersPatch.numMeteors;
            numVents = UnityEngine.Object.FindObjectsOfType<EnemyVent>().Length;

            var mines = UnityEngine.Object.FindObjectsOfType<Landmine>();
            if (mines.Length > 0)
            {
                trapCounts[TRAP.LANDMINE] = mines.Length;
            }
            var turrets = UnityEngine.Object.FindObjectsOfType<Turret>();
            if (turrets.Length > 0)
            {
                trapCounts[TRAP.TURRET] = turrets.Length;
            }
            var spikes = UnityEngine.Object.FindObjectsOfType<SpikeRoofTrap>();
            if (spikes.Length > 0)
            {
                trapCounts[TRAP.SPIKETRAP] = spikes.Length;
            }
            var entrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
            var traps = mines.OfType<Component>().Concat(turrets).Concat(spikes);
            foreach (var entrance in entrances)
            {
                if (entrance.isEntranceToBuilding)
                {
                    continue;
                }
                foreach (var trap in traps)
                {
                    var best = nearestEntranceTraps.GetValueOrDefault(entrance, new(trap, float.PositiveInfinity));
                    var dist = Vector3.Distance(trap.transform.position, entrance.transform.position);
                    if (dist < best.Item2)
                    {
                        nearestEntranceTraps[entrance] = new(trap, dist);
                    }
                }
            }

            foreach (Transform item in RoundManager.Instance.mapPropsContainer.transform)
            {
                foreach (var item2 in RoundManager.Instance.currentLevel.spawnableOutsideObjects)
                {
                    if (item.name.Contains(item2.spawnableObject.prefabToSpawn.name))
                    {
                        int count = outsideObjectCounts.GetValueOrDefault(item2.spawnableObject.name);
                        outsideObjectCounts[item2.spawnableObject.name] = count + 1;
                    }
                }
            }
        }

        public override string ToString()
        {
            string trapList = string.Join(", ", [.. from item in trapCounts select $"{item.Key}: {item.Value}"]);
            string outsideObjectList = string.Join(", ", [.. from item in outsideObjectCounts select $"{item.Key}: {item.Value}"]);
            string nearestTrapList = string.Join(", ", [.. from item in nearestEntranceTraps select $"{item.Key.gameObject.name}: {item.Value.Item1.GetType().Name}: {item.Value.Item2}"]);
            return $"dungeon: {currentDungeonType}, num rooms: {numRooms}, locked doors: {numLockedDoors}, meteor shower: {meteor}, meteor shower time: {meteorShowerAtTime}, num meteors: {numMeteors}, blackout: {blackout}, vents: {numVents}, company mood: {currentCompanyMood.name}\n  traps: [{trapList}]\n  outside objects: [{outsideObjectList}]\n  nearest traps: [{nearestTrapList}]";
        }
    }
}
