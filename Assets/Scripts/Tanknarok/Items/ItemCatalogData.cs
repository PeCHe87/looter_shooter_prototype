
using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    public enum ItemType { NONE, RESOURCE, CONSUMABLE, EQUIPABLE_WEAPON, EQUIPABLE_ARMOR, AMMO }

    /// <summary>
    /// Class that represents the base item catalog information that is showable on UI
    /// </summary>
    [System.Serializable]
    public class ItemCatalogData
    {
        public BaseItemCatalogData data;

        public bool IsConsumable()
        {
            return data.type == ItemType.CONSUMABLE;
        }

        public bool IsEquipable()
        {
            return (data.type == ItemType.EQUIPABLE_WEAPON) || (data.type == ItemType.EQUIPABLE_ARMOR);
        }

        public bool IsWeapon()
		{
            return data.type == ItemType.EQUIPABLE_WEAPON;
		}
    }
}