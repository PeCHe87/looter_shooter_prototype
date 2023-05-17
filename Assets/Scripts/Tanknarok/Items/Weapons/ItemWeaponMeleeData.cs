using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    [CreateAssetMenu(fileName = "Weapon_Melee", menuName = "ScriptableObjects/Weapons/Melee")]
    public class ItemWeaponMeleeData : ItemWeaponData
    {
        public float Radius;
        public float Angle;
        public int Damage;
        public LayerMask HitMask;
        public float Impulse;
    }
}