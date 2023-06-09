using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
    public class UI_LootInGameSlotItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtAmount = default;
        [SerializeField] private Image _icon = default;
        [SerializeField] private Button _btn = default;
        [SerializeField] private Image _selectionBorder = default;

        private int _index = -1;
        private int _id = default;
        private int _amount = default;
        private System.Action<int, int, int, bool> _callback = default;
        private bool _isSelected = false;

        public int Id => _id;

        public void Init(int index, System.Action<int, int, int, bool> callback)
        {
            _index = index;

            _callback = callback;

            _btn.onClick.AddListener(Selection);

            MarkAsUnselected();
        }

        public void Teardown()
        {
            _btn.onClick.RemoveAllListeners();

            _callback = null;
        }

        public void Setup(int id, int amount, Sprite icon)
        {
            _id = id;

            _amount = amount;

            _txtAmount.text = $"{amount}";

            _icon.sprite = icon;

            _isSelected = false;

            MarkAsUnselected();
        }

        public void Deselect()
        {
            _isSelected = false;

            MarkAsUnselected();
        }

        private void Selection()
        {
            if (_isSelected)
            {
                MarkAsUnselected();

                _isSelected = false;
            }
            else
            {
                MarkAsSelected();

                _isSelected = true;
            }

            _callback?.Invoke(_index, _id, _amount, _isSelected);
        }

        private void MarkAsSelected()
        {
            _selectionBorder.enabled = true;
        }

        private void MarkAsUnselected()
        {
            _selectionBorder.enabled = false;
        }
    }
}