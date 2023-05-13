
using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    [CreateAssetMenu(fileName = "Weapon_", menuName = "ScriptableObjects/Weapon")]
    public class ItemWeaponData : ScriptableObject
    {
		public string ItemCatalogId;
        public string DisplayName;
        public ItemWeaponType Type;
		/*
		public Weapon Prefab;
		public Projectile ProjectilePrefab;
		public ParticleSystem MuzzleFlashPrefab;
		public float RateOfFire;
		public byte InitialAmmo;
		public byte Ammo;
		public bool InfiniteAmmo;
		public PowerupType PowerupType = PowerupType.DEFAULT;
		public float reloadingTime = 2;
		*/
	}
}