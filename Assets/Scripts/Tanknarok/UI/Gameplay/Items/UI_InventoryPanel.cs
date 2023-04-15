using FusionExamples.Tanknarok.Items;
using UnityEngine;

namespace FusionExamples.Tanknarok.UI
{
    public class UI_InventoryPanel : MonoBehaviour
    {
        [SerializeField] private UI_InventorySlot[] _slots = default;
        [SerializeField] private LevelManager _levelManager = default;

        public void Init(PlayerInventoryData data)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                slot.Init(_levelManager);
            }

            Refresh(data);
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
    }
}