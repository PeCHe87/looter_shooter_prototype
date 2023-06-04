
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionExamples.Tanknarok.UI
{
    public class UI_InventoryItemPanel : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private GameObject _content = default;
        [SerializeField] private Button _btnClose = default;
        [SerializeField] private Button _btnUse = default;
        [SerializeField] private Button _btnEquip = default;
        [SerializeField] private TextMeshProUGUI _txtName = default;
        [SerializeField] private Image _icon = default;

        #endregion

        #region Private properties

        private Items.BaseItemCatalogData _item = default;
        private int _slotIndex = default;
        private Player _player;
        private System.Action _callbackSlotInteraction = default;

        #endregion

        #region Public methods

        public void Init(Player player, System.Action callbackSlotInteraction)
        {
            _callbackSlotInteraction = callbackSlotInteraction;

            _player = player;

            _btnUse.onClick.AddListener(UseItem);
            _btnEquip.onClick.AddListener(EquipItem);
            _btnClose.onClick.AddListener(Close);
        }

        public void Teardown()
        {
            _btnUse.onClick.RemoveAllListeners();
            _btnEquip.onClick.RemoveAllListeners();
            _btnClose.onClick.RemoveAllListeners();
        }

        public void Show(Items.ItemCatalogData itemCatalog, int index)
        {
            GetItemByType(itemCatalog);

            _slotIndex = index;

            _txtName.text = itemCatalog.data.displayName.ToUpperInvariant();

            _btnUse.gameObject.Toggle(itemCatalog.IsConsumable());

            _btnEquip.gameObject.Toggle(itemCatalog.IsEquipable());

            _icon.sprite = itemCatalog.data.icon;

            _content.Toggle(true);
        }

        public void Hide()
        {
            _content.Toggle(false);
        }

        #endregion

        #region Private methods

        private void GetItemByType(Items.ItemCatalogData itemCatalog)
        {
            if (itemCatalog.IsConsumable())
            {
                _item = (Items.ConsumableItemCatalogData)itemCatalog.data;
                return;
            }

            if (itemCatalog.IsEquipable())
            {
                _item = (Items.EquipableItemCatalogData)itemCatalog.data;
                return;
            }

            _item = null;
        }

        private void UseItem()
        {
            Debug.LogError($"Use item <color=yellow>{_item.displayName}</color>");

            ((Items.ConsumableItemCatalogData)_item).Consume(_player);

            _player.ConsumeInventorySlot(_slotIndex);

            Hide();

            _callbackSlotInteraction?.Invoke();
        }

        private void EquipItem()
        {
            ((Items.EquipableItemCatalogData)_item).Equip(_player);

            _player.EquipInventorySlot(_slotIndex);

            Hide();

            _callbackSlotInteraction?.Invoke();
        }

        private void Close()
        {
            // TODO: sfx

            Hide();
        }

        #endregion
    }
}