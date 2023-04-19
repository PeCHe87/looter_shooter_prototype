
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionExamples.Tanknarok.UI
{
    public class UI_InventoryItemPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _content = default;
        [SerializeField] private Button _btnClose = default;
        [SerializeField] private Button _btnUse = default;
        [SerializeField] private TextMeshProUGUI _txtName = default;
        [SerializeField] private Image _icon = default;

        private Items.ConsumableItemCatalogData _item = default;
        private int _slotIndex = default;
        private Player _player;
        private System.Action _callbackUse = default;

        public void Init(Player player, System.Action callbackUse)
        {
            _callbackUse = callbackUse;

            _player = player;

            _btnUse.onClick.AddListener(UseItem);
            _btnClose.onClick.AddListener(Close);
        }

        public void Teardown()
        {
            _btnUse.onClick.RemoveAllListeners();
            _btnClose.onClick.RemoveAllListeners();
        }

        public void Show(Items.ItemCatalogData itemCatalog, int index)
        {
            var isConsumable = itemCatalog.data.type == Items.ItemType.CONSUMABLE;

            _item = (isConsumable) ? (Items.ConsumableItemCatalogData) itemCatalog.data : null;

            _slotIndex = index;

            _txtName.text = itemCatalog.data.displayName.ToUpperInvariant();

            _btnUse.gameObject.Toggle(isConsumable);

            _icon.sprite = itemCatalog.data.icon;

            _content.Toggle(true);
        }

        public void Hide()
        {
            _content.Toggle(false);
        }

        private void UseItem()
        {
            Debug.LogError($"Use item <color=yellow>{_item.displayName}</color>");

            _item.Consume(_player);

            _player.ConsumeInventorySlot(_slotIndex);

            Hide();

            _callbackUse?.Invoke();
        }

        private void Close()
        {
            // TODO: sfx

            Hide();
        }
    }
}