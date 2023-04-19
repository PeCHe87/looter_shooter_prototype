
using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    [CreateAssetMenu(fileName = "ItemCatalog_Consumable_", menuName = "ScriptableObjects/ItemCatalog_Consumable")]
    public class ConsumableItemCatalogData : BaseItemCatalogData
    {
        public virtual void Consume(Player player) { }
    }
}