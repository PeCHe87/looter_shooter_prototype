
using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    [CreateAssetMenu(fileName = "ItemCatalog_Consumable_Healing_", menuName = "ScriptableObjects/ItemCatalog_Consumable_Healing")]
    public class ConsumableItemCatalogData_Healing : ConsumableItemCatalogData
    {
        public int healAmount;

        public override void Consume(Player player) 
        {
            player.Heal(healAmount);
        }
    }
}