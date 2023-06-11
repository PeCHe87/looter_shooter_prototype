
using UnityEngine;
using UnityEngine.UI;

namespace FusionExamples.Tanknarok.UI
{
    public class UI_PlayerInfoWeaponsPanel : MonoBehaviour
    {
        #region Inspector

        [Header("Slots")]
        [SerializeField] private UI_PlayerInfoWeaponSlot _slot1 = default;
        [SerializeField] private UI_PlayerInfoWeaponSlot _slot2 = default;

        #endregion

        #region Private properties

        private Player _player = default;
        private int _itemId = default;

        #endregion

        #region Unity events

        private void Start()
        {
            Init();
        }

        private void OnDestroy()
        {
            Teardown();
        }

        #endregion

        #region Public methods

        public void EquipWeapon(Items.ItemCatalogData itemCatalog)
		{
            _slot1.Setup(itemCatalog);
            _slot1.SetEquip(true);

            _slot2.gameObject.Toggle(true);
            _slot2.SetEquip(false);
		}

        public void SetPlayer(Player player)
        {
            _player = player;
        }

        public void CleanWeapons()
		{
            _slot1.Clean();
            _slot2.Clean();
        }

        #endregion

        #region Private methods

        private void Init()
        {
            _slot1.Init(SelectSlot);
            _slot1.SetEquip(true);

            _slot2.Init(SelectSlot);
            _slot2.gameObject.Toggle(false);
        }

        private void Teardown()
        {
            _slot1.Teardown();
            _slot2.Teardown();
        }

        private void SelectSlot(int itemId)
		{
            _itemId = itemId;

            DeselectRestOfSlots(itemId);

            _player.UpdateEquippedWeapon(itemId);
		}

        private void DeselectRestOfSlots(int itemId)
		{
            // Slot 1
            if (_slot1.ItemId != itemId)
			{
                _slot1.SetEquip(false);
			}

            // Slot 2
            if (_slot2.ItemId != itemId)
            {
                _slot2.SetEquip(false);
            }
        }

        #endregion
    }
}