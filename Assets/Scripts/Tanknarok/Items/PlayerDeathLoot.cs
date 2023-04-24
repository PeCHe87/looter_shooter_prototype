
using UnityEngine;
using Fusion;

namespace FusionExamples.Tanknarok.Items
{
    /// <summary>
    /// Class in charge of spawn a death body loot when player dies.
    /// It is created where player has died and the content is the player's inventory.
    /// </summary>
    public class PlayerDeathLoot : NetworkBehaviour
    {
        [SerializeField] private int _id = 0;
        [SerializeField] private LootboxDeathBody _prefab = default;
        [SerializeField] private LevelManager _levelManager = default;
        
        [Header("For debug")]
        [SerializeField] private Transform _target = default;
        [SerializeField] private ItemLootData[] _itemsForDebug = default;

        public void SpawnLoot(Vector3 position, ItemLootData[] items)
        {
            var spawnPosition = new Vector3(position.x, 0, position.z);

            var loot = _levelManager.Runner.Spawn(_prefab, spawnPosition, Quaternion.identity);

            loot.Configure(_id, items);

            _id++;

            loot.transform.SetParent(_levelManager.LevelBehavior.LootsContainer);
        }

        [ContextMenu("TEST")]
        public void Test()
        {
            var position = _target.position;

            SpawnLoot(position, _itemsForDebug);
        }
    }
}