
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FusionExamples.Tanknarok.Items;

namespace FusionExamples.Tanknarok
{
    public class UI_LootInGamePanel : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private GameObject _content = default;
        [SerializeField] private Button _btnClose = default;
        [SerializeField] private TextMeshProUGUI _itemDisplayName = default;
        [SerializeField] private Image _itemIcon = default;
        [SerializeField] private LevelManager _levelManager = default;
        [SerializeField] private UI_LootInGameSlotItem[] _slots = default;
        [SerializeField] private Button _btnTake = default;

        #endregion

        #region Private properties

        private int _slotItemId = default;
        private int _slotAmount = default;
        private System.Action<int, int> _callbackTake = default;

        #endregion

        #region Public methods

        public void Init(System.Action<int, int> callback)
        {
            Debug.LogError("<color=magenta>LootInGamePanel</color>::Init");

            _callbackTake = callback;

            _btnClose.onClick.AddListener(Close);
            _btnTake.onClick.AddListener(Take);

            HideTakeButton();

            InitSlots();

            Hide();
        }

        public void Teardown()
        {
            _btnClose.onClick.RemoveAllListeners();

            _btnTake.onClick.RemoveAllListeners();

            TeardownSlots();
        }

        public void Show(LootData lootboxData)
        {
            LoadSlots(lootboxData);

            HideTakeButton();

            _content.Toggle(true);
        }

        public void Close()
        {
            // TODO: sfx

            Hide();
        }

        public void Remove(int id)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                if (slot.Id != id) continue;

                slot.gameObject.Toggle(false);

                break;
            }
        }

        #endregion

        #region Private methods

        private void Hide()
        {
            _content.Toggle(false);
        }

        private void Take()
        {
            if (_levelManager.Catalog.TryGetItem(_slotItemId, out var itemData))
            {
                var displayName = itemData.displayName;

                Debug.LogError($"Take item <color=yellow>{displayName}</color>(<color=magenta>{_slotAmount}</color>)");

                _callbackTake?.Invoke(_slotItemId, _slotAmount);

                return;
            }

            Debug.LogError($"Item <color=orange>{_slotItemId}</color> not found!");
        }

        #endregion

        #region Slots

        private void InitSlots()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                slot.Init(SelectSlot);
            }

            HideSlots();
        }

        private void HideSlots()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                slot.gameObject.Toggle(false);
            }
        }

        private void TeardownSlots()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                slot.Teardown();
            }
        }

        private void SelectSlot(int id, int amount, bool isSelected)
        {
            _slotItemId = -1;
            _slotAmount = 0;

            if (isSelected)
            {
                _slotItemId = id;

                _slotAmount = amount;

                for (int i = 0; i < _slots.Length; i++)
                {
                    var slot = _slots[i];

                    if (slot.Id == id) continue;

                    slot.Deselect();
                }

                ShowTakeButton();

                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                slot.Deselect();
            }

            HideTakeButton();
        }

        private void ShowTakeButton()
        {
            _btnTake.gameObject.Toggle(true);
        }

        private void HideTakeButton()
        {
            _btnTake.gameObject.Toggle(false);
        }

        private void LoadSlots(LootData lootData)
        {
            HideSlots();

            var index = 0;

            for (int i = 0; i < lootData.items.Length; i++)
            {
                var itemLootData = lootData.items[i];

                if (itemLootData.id <= 0) continue;

                if (itemLootData.amount <= 0) continue;

                var itemExist = _levelManager.Catalog.TryGetItem(itemLootData.id, out var itemCatalogData);

                if (!itemExist) continue;

                var slot = _slots[index];
                slot.Setup(itemLootData.id, itemLootData.amount, itemCatalogData.icon);
                slot.gameObject.Toggle(true);

                index++;

                if (index >= _slots.Length) return;
            }
        }

        #endregion
    }
}