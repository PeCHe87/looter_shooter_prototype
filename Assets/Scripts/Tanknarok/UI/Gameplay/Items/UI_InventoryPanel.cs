using FusionExamples.Tanknarok.Items;
using UnityEngine;

namespace FusionExamples.Tanknarok.UI
{
    public class UI_InventoryPanel : MonoBehaviour
    {
        [SerializeField] private UI_InventorySlot[] _slots = default;
        [SerializeField] private LevelManager _levelManager = default;
        [SerializeField] private UI_InventoryItemPanel _itemInfoPanel = default;

        public void Init(PlayerInventoryData data, Player player)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                slot.Init(_levelManager, SlotSelection);
            }

            Refresh(data);

            _itemInfoPanel.Init(player, HideAllSlots);
        }

        public void Teardown()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                slot.Teardown();
            }

            _itemInfoPanel.Teardown();
        }

        public void Refresh(PlayerInventoryData data)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                if (data.items.Length <= i)
                {
                    slot.SetupEmpty();
                    continue;
                }

                var item = data.items[i];

                if (item.locked)
                {
                    slot.SetupLocked();
                    continue;
                }

                if (item.id <= 0)
                {
                    slot.SetupEmpty();
                    continue;
                }

                slot.SetupRegular(item);
            }
        }

        private void SlotSelection(int index, bool isSelected, InventorySlotStatus status, PlayerInventoryItemData playerItem)
        {
            // Regular
            if (status == InventorySlotStatus.REGULAR)
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    var slot = _slots[i];

                    if (slot.Index == index) continue;

                    slot.Deselect();
                }

                if (!isSelected)
                {
                    _itemInfoPanel.Hide();

                    return;
                }

                if (!_levelManager.Catalog.TryGetItem(playerItem.id, out var itemCatalog)) return;

                Debug.LogError($"Slot [<color=yellow>{index}</color>] - Item '<color=magenta>{itemCatalog.data.displayName}</color>'");

                _itemInfoPanel.Show(itemCatalog, index);

                return;
            }

            // Empty
            if (status == InventorySlotStatus.EMPTY)
            {
                if (!isSelected) return;

                Debug.LogError($"Slot [<color=yellow>{index}</color>] - <color=cyan>EMPTY</color>");

                return;
            }

            // Locked
            if (status == InventorySlotStatus.LOCKED)
            {
                if (!isSelected) return;

                Debug.LogError($"Slot [<color=yellow>{index}</color>] - <color=orange>LOCKED</color>");

                return;
            }
        }

        private void HideAllSlots()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                slot.Deselect();
            }
        }
    }
}