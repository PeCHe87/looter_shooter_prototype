
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionExamples.Tanknarok.UI
{
    public class UI_LootItemInfoPanel : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private GameObject _content = default;
        [SerializeField] private TextMeshProUGUI _txtName = default;
        [SerializeField] private TextMeshProUGUI _txtDescription = default;
        [SerializeField] private Image _icon = default;

        [Header("Ammo slot")]
        [SerializeField] private GameObject _ammoSlot = default;
        [SerializeField] private Image _ammoIcon = default;

        #endregion

        #region Private properties

        private const string AMMO_DESCRIPTION = "Needed for Assault weapons";
        private const string WEAPON_ASSAULT_DESCRIPTION = "Assault weapon";
        private const string WEAPON_MELEE_DESCRIPTION = "Melee weapon";

        #endregion

        #region Public methods

        public void Show(Items.ItemCatalogData itemCatalog)
        {
            _txtName.text = itemCatalog.data.displayName.ToUpperInvariant();

            var isEquipable = itemCatalog.IsEquipable();

            if (isEquipable)
			{
                RefreshAmmoSlot(itemCatalog);
			}
            else
			{
                HideAmmoSlot();
			}

            ShowDescription(itemCatalog);

            _icon.sprite = itemCatalog.data.icon;

            _content.Toggle(true);
        }

        public void Hide()
        {
            _content.Toggle(false);
        }

        #endregion

        #region Private methods

        private void ShowDescription(Items.ItemCatalogData itemCatalog)
		{
            var type = itemCatalog.data.type;

            if (type == Items.ItemType.RESOURCE)
			{
                _txtDescription.enabled = false;
                return;
			}

            if (type == Items.ItemType.AMMO)
            {
                _txtDescription.enabled = true;
                _txtDescription.text = AMMO_DESCRIPTION;
                return;
            }

            if (type == Items.ItemType.CONSUMABLE)
			{
                _txtDescription.enabled = true;
                _txtDescription.text = itemCatalog.data.description;
                return;
			}

            if (type == Items.ItemType.EQUIPABLE_ARMOR)
			{
                // TODO
                _txtDescription.enabled = false;
                return;
            }

            if (type == Items.ItemType.EQUIPABLE_WEAPON)
			{
                var equipableData = (Items.EquipableItemCatalogData)(itemCatalog.data);

                _txtDescription.enabled = true;
                _txtDescription.text = (equipableData.WeaponData.Type == Items.ItemWeaponType.ASSAULT) ? WEAPON_ASSAULT_DESCRIPTION : WEAPON_MELEE_DESCRIPTION;
                return;
            }

            _txtDescription.enabled = false;
        }

        private void RefreshAmmoSlot(Items.ItemCatalogData itemCatalog)
		{
            var weaponData = ((Items.EquipableItemCatalogData)itemCatalog.data).WeaponData;

            if (weaponData.Type != Items.ItemWeaponType.ASSAULT)
			{
                HideAmmoSlot();
                return;
			}

            var assaultWeaponData = (Items.ItemWeaponAssaultData)weaponData;

            var icon = assaultWeaponData.AmmoType.icon;

            _ammoIcon.sprite = icon;

            _ammoSlot.Toggle(true);
		}

        private void HideAmmoSlot()
		{
            _ammoSlot.Toggle(false);
		}

        #endregion
    }
}