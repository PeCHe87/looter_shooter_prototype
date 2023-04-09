
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FusionExamples.Tanknarok
{
    public class UI_LootInGamePanel : MonoBehaviour
    {
        [SerializeField] private GameObject _content = default;
        [SerializeField] private Button _btnClose = default;
        [SerializeField] private TextMeshProUGUI _txtCoins = default;

        public void Init()
        {
            _btnClose.onClick.AddListener(Close);

            Hide();
        }

        public void Teardown()
        {
            _btnClose.onClick.RemoveAllListeners();
        }

        public void Show(LootboxData lootboxData)
        {
            _txtCoins.text = $"Coins: {lootboxData.coins}";
            _content.Toggle(true);
        }

        public void Close()
        {
            // TODO: sfx

            Hide();
        }

        private void Hide()
        {
            _content.Toggle(false);
        }
    }
}