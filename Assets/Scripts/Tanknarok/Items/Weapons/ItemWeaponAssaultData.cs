
using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
	[CreateAssetMenu(fileName = "Weapon_Assault_", menuName = "ScriptableObjects/Weapons/Assault")]
	public class ItemWeaponAssaultData : ItemWeaponData
    {
		public Projectile ProjectilePrefab;
		public ParticleSystem MuzzleFlashPrefab;
		public float RateOfFire;
		public byte InitialAmmo;
		public byte Ammo;
		public bool InfiniteAmmo;
		public PowerupType PowerupType = PowerupType.DEFAULT;
		public float reloadingTime = 2;
		public float RadiusDetection;
		public BaseItemCatalogData AmmoType;
	}
}