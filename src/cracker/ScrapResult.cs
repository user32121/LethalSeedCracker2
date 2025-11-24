using LethalSeedCracker2.src.config;
using System.Collections.Generic;
using System.Linq;

namespace LethalSeedCracker2.src.cracker
{
    internal class ScrapResult
    {
        internal int numScrap;
        internal int totalScrapValue;
        internal Dictionary<Item, int> scrapCounts;

        public ScrapResult(Config config)
        {
            numScrap = 0;
            scrapCounts = [];
            totalScrapValue = 0;
            if (!config.skipScrap)
            {
                GrabbableObject[] items = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].itemProperties.isScrap && !items[i].isInShipRoom && !items[i].isInElevator)
                    {
                        totalScrapValue += items[i].scrapValue;
                        numScrap++;
                        int count = scrapCounts.GetValueOrDefault(items[i].itemProperties);
                        scrapCounts[items[i].itemProperties] = count + 1;
                    }
                }
            }
        }

        public override string ToString()
        {
            string scrapList = string.Join(", ", [.. from item in scrapCounts select $"{item.Key.name}: {item.Value}"]);
            return $"num scrap: {numScrap}, total scrap value: {totalScrapValue}\n  scrap: [{scrapList}]";
        }
    }
}