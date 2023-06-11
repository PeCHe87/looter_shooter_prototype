using UnityEngine;
using UnityEngine.UI;

namespace FusionExamples.Tanknarok.UI
{
    public class UI_PlayerInfoWeaponSlot : MonoBehaviour
    {
		#region Inspector

		[SerializeField] private Button _btn = default;
		[SerializeField] private Image _icon = default;
		[SerializeField] private Image _selection = default;
		[SerializeField] private Image _equipped = default;
		[SerializeField] private Sprite _emptyIcon = default;

		#endregion

		#region Private properties

		private bool _isEquipped = false;
		private int _itemId = -1;
		private System.Action<int> _callback = default;

		#endregion

		#region Public properties

		public bool IsEmpty => _itemId == -1;
		public int ItemId => _itemId;

		#endregion

		#region Public methods

		public void Init(System.Action<int> callback)
		{
			_callback = callback;

			_btn.onClick.AddListener(CheckSelection);

			SetEquip(false);
		}

		public void Teardown()
		{
			_callback = null;

			_btn.onClick.RemoveAllListeners();
		}

		public void Setup(Items.ItemCatalogData item)
		{
			_itemId = item.data.id;

			_icon.sprite = item.data.icon;
		}

		public void SetEquip(bool equipped)
		{
			_equipped.enabled = equipped;
			_selection.enabled = equipped;

			_isEquipped = equipped;
		}

		public void Clean()
		{
			_itemId = -1;

			_icon.sprite = _emptyIcon;

			SetEquip(false);
		}

		#endregion

		#region Private methods

		private void CheckSelection()
		{
			if (_isEquipped) return;

			SetEquip(true);

			_callback?.Invoke(_itemId);
		}

		#endregion
	}
}