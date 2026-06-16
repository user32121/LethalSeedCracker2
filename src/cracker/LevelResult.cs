using DunGen;
using LethalSeedCracker2.Patches;
using LethalSeedCracker2.src.config;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LethalSeedCracker2.src.cracker.Defines;

namespace LethalSeedCracker2.src.cracker
{
    internal class LevelResult
    {
        internal DUNGEON currentDungeonType;
        internal int numRooms;
        internal bool meteor;
        internal float meteorShowerAtTime;
        internal Dictionary<TRAP, int> trapCounts = [];
        internal int numDoors;
        internal int numLockedDoors;
        internal int numLockedBigDoors;
        internal Dictionary<string, int> outsideObjectCounts = [];
        internal Dictionary<EntranceTeleport, float> nearestPumpkins = [];
        internal bool blackout;
        internal CompanyMood currentCompanyMood;
        internal int numMeteors;
        internal int numVents;
        internal Dictionary<EntranceTeleport, Tuple<Component, float>> nearestEntranceTraps = [];
        internal int numValves;
        internal int numBurstValves;
        internal float highestRoom;
        internal float nearestRoomToMain;
        internal Dictionary<Tuple<EntranceTeleport, EntranceTeleport>, float> distanceBetweenEntrances = [];
        internal Dictionary<EnemyType, Dictionary<EntranceTeleport, float>> closestNestToEntrance = [];
        internal Dictionary<EnemyType, float> closestNestToShip = [];

        public LevelResult(Config config)
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
            numDoors = 0;
            numLockedDoors = 0;
            foreach (var doorLock in UnityEngine.Object.FindObjectsOfType<DoorLock>())
            {
                ++numDoors;
                if (doorLock.isLocked)
                {
                    ++numLockedDoors;
                }
            }
            numLockedBigDoors = 0;
            foreach (var item in UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>())
            {
                if (item.isBigDoor && !item.isDoorOpen)
                {
                    ++numLockedBigDoors;
                }
            }
            meteorShowerAtTime = TimeOfDayPatch.meteorShowerAtTime;
            blackout = !UnityEngine.Object.FindObjectOfType<BreakerBox>()?.isPowerOn ?? false;
            currentCompanyMood = TimeOfDay.Instance.currentCompanyMood;
            numMeteors = MeteorShowersPatch.numMeteors;
            numVents = UnityEngine.Object.FindObjectsOfType<EnemyVent>().Length;
            foreach (var valve in UnityEngine.Object.FindObjectsOfType<SteamValveHazard>())
            {
                ++numValves;
                if (valve.valveBurstTime > 0)
                {
                    ++numBurstValves;
                }
            }
            highestRoom = UnityEngine.Object.FindObjectsOfType<Tile>().Select(x => x.transform.position.y).Max();
            var main = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>().Where(x => !x.isEntranceToBuilding && x.gameObject.name.Contains("EntranceTeleportA")).First();
            nearestRoomToMain = UnityEngine.Object.FindObjectsOfType<Tile>().Select(x => (x.transform.position - main.entrancePoint.position).magnitude).Min();

            var entrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
            var insideEntrances = entrances.Where(x => !x.isEntranceToBuilding);
            var outsideEntrances = entrances.Where(x => x.isEntranceToBuilding);

            foreach (var e1 in insideEntrances)
            {
                foreach (var e2 in insideEntrances)
                {
                    if (e1 == e2)
                    {
                        break;
                    }
                    distanceBetweenEntrances[new(e1, e2)] = (e1.entrancePoint.position - e2.entrancePoint.position).magnitude;
                }
            }

            if (!config.skipTraps)
            {
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
                var traps = mines.OfType<Component>().Concat(turrets).Concat(spikes);
                foreach (var entrance in insideEntrances)
                {
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
            }

            if (!config.skipOutsideObjects)
            {
                foreach (Transform item in RoundManager.Instance.mapPropsContainer.transform)
                {
                    foreach (var item2 in RoundManager.Instance.currentLevel.spawnableOutsideObjects)
                    {
                        if (item.name.Contains(item2.spawnableObject.prefabToSpawn.name))
                        {
                            int count = outsideObjectCounts.GetValueOrDefault(item2.spawnableObject.name);
                            outsideObjectCounts[item2.spawnableObject.name] = count + 1;

                            if (item2.spawnableObject.name.ToLower().Contains("pumpkin"))
                            {
                                foreach (var entrance in outsideEntrances)
                                {
                                    var best = nearestPumpkins.GetValueOrDefault(entrance, float.PositiveInfinity);
                                    var dist = Vector3.Distance(item.transform.position, entrance.transform.position);
                                    if (dist < best)
                                    {
                                        nearestPumpkins[entrance] = dist;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var item in UnityEngine.Object.FindObjectsOfType<EnemyAINestSpawnObject>())
                {
                    if (!closestNestToEntrance.ContainsKey(item.enemyType))
                    {
                        closestNestToEntrance[item.enemyType] = [];
                    }
                    foreach (var entrance in outsideEntrances)
                    {
                        var dist = (item.transform.position - entrance.entrancePoint.position).magnitude;
                        if (!closestNestToEntrance[item.enemyType].ContainsKey(entrance) || closestNestToEntrance[item.enemyType][entrance] < dist)
                        {
                            closestNestToEntrance[item.enemyType][entrance] = dist;
                        }
                    }

                    foreach (var pos in StartOfRound.Instance.playerSpawnPositions)
                    {
                        var dist = (item.transform.position - pos.position).magnitude;
                        if (!closestNestToShip.ContainsKey(item.enemyType) || closestNestToShip[item.enemyType] < dist)
                        {
                            closestNestToShip[item.enemyType] = dist;
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            string trapList = string.Join(", ", [.. from item in trapCounts select $"{item.Key}: {item.Value}"]);
            string outsideObjectList = string.Join(", ", [.. from item in outsideObjectCounts select $"{item.Key}: {item.Value}"]);
            string nearestTrapList = string.Join(", ", [.. from item in nearestEntranceTraps select $"{item.Key.gameObject.name}: {item.Value.Item1.GetType().Name}: {item.Value.Item2}"]);
            string nearestPumpkinList = string.Join(", ", [.. from item in nearestPumpkins select $"{item.Key.gameObject.name}: {item.Value}"]);
            string entranceDistanceList = string.Join(", ", [.. from item in distanceBetweenEntrances select $"({item.Key.Item1.name}, {item.Key.Item2.name}): {item.Value}"]);
            string closestNestEntranceList = string.Join(", ", [.. from item in closestNestToEntrance select $"{item.Key.name}: [{string.Join(", ", [.. from item2 in item.Value select $"{item2.Key.name}: {item2.Value}"])}]"]);
            string closestNestShipList = string.Join(", ", [.. from item in closestNestToShip select $"{item.Key.name}: {item.Value}"]);
            return $"dungeon: {currentDungeonType}, blackout: {blackout}, vents: {numVents}, num rooms: {numRooms}, doors: {numDoors}, locked doors: {numLockedDoors}, locked powered doors: {numLockedBigDoors}, valves: {numValves}, burstvalves: {numBurstValves}\n  highestroom: {highestRoom}, nearestroomtomain: {nearestRoomToMain}\n  company mood: {currentCompanyMood.name}, meteor shower: {meteor}, meteor shower time: {meteorShowerAtTime}, num meteors: {numMeteors}\n  traps: [{trapList}]\n  outside objects: [{outsideObjectList}]\n  nearest traps: [{nearestTrapList}]\n  nearest pumpkins: [{nearestPumpkinList}]\n  distancebetweenentrances: [{entranceDistanceList}]\n  closestnesttoentrance: [{closestNestEntranceList}]\n  closestnesttoship: [{closestNestShipList}]";
        }
    }
}
