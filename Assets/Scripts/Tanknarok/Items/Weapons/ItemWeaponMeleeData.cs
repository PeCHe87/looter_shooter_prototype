using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    [CreateAssetMenu(fileName = "Weapon_Melee", menuName = "ScriptableObjects/Weapons/Melee")]
    public class ItemWeaponMeleeData : ItemWeaponData
    {
        public float RadiusAttack;
        public int Damage;
	}
}