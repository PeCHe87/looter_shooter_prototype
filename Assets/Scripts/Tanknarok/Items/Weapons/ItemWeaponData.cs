
using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    [CreateAssetMenu(fileName = "Weapon_", menuName = "ScriptableObjects/Weapon")]
    public class ItemWeaponData : ScriptableObject
    {
		public string ItemCatalogId;
        public string DisplayName;
        public ItemWeaponType Type;
		public GameObject Visual;
	}
}