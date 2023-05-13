
using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    [CreateAssetMenu(fileName = "ItemCatalog_Equipable_", menuName = "ScriptableObjects/ItemCatalog_Equipable")]
    public class EquipableItemCatalogData : BaseItemCatalogData
    {
        public ItemWeaponData WeaponData;

        public virtual void Equip(Player player) { }
    }
}