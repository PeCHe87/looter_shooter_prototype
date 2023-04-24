using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FusionExamples.Tanknarok
{
    public class UI_PlayerKillPlayerSlot : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txt = default;
        [SerializeField] private Image _team = default;

        private float _remainingTime = 0;

        public void Setup(string killer, string killed, float time, Color teamColor)
        {
            _remainingTime = time;

            _txt.text = $"{killer} killed {killed}";

            _team.color = teamColor;
        }

        private void Update()
        {
            if (_remainingTime <= 0) return;

            _remainingTime -= Time.deltaTime;

            if (_remainingTime > 0) return;

            gameObject.Toggle(false);
        }
    }
}