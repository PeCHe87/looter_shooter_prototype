using UnityEngine;

namespace FusionExamples.Tanknarok
{
    public class UI_PlayerKillPlayerPanel : MonoBehaviour
    {
        [SerializeField] private UI_PlayerKillPlayerSlot[] _slots = default;
        [SerializeField] private float _slotVisibleDuration = 3;

        public void ShowMessage(string playerKillerName, string playerKilledName, Color teamColor)
        {
            var slot = GetFreeSlot();

            slot.Setup(playerKillerName, playerKilledName, _slotVisibleDuration, teamColor);

            slot.gameObject.Toggle(true);
        }

        private UI_PlayerKillPlayerSlot GetFreeSlot()
        {
            int index = 0;

            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];

                if (slot.gameObject.activeSelf) continue;

                index = i;

                break;
            }

            return _slots[index];
        }
    }
}