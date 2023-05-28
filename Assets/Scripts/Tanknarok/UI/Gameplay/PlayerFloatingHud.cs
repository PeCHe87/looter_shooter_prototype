
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static FusionExamples.Tanknarok.GameLauncher;

namespace FusionExamples.Tanknarok.UI
{
    public class PlayerFloatingHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtDisplayName = default;
        [SerializeField] private Image _imgTeam = default;
        [SerializeField] private Color _teamBlueColor = default;
        [SerializeField] private Color _teamRedColor = default;
        [SerializeField] private Image _imgCollectable = default;
        [SerializeField] private GameObject _maxCapacity = default;
        [SerializeField] private Image _imgHealth = default;

        private void Start()
        {
            RefreshCollectableProgress(0);
        }

        public void SetDisplayName(string displayName)
        {
            _txtDisplayName.text = displayName.ToUpperInvariant();
        }

        public void SetTeam(TeamEnum team)
        {
            _imgTeam.color = (team == TeamEnum.BLUE) ? _teamBlueColor : _teamRedColor;
        }

        public void UpdateCollectables(int amount, int max)
        {
            var progress = Mathf.Clamp((float)amount / (float)max, 0, 1);

            RefreshCollectableProgress(progress);
        }

        public void RefreshHealth(float progress)
        {
            _imgHealth.fillAmount = progress;
        }

        private void RefreshCollectableProgress(float progress)
        {
            _imgCollectable.fillAmount = progress;

            _maxCapacity.Toggle(progress >= 1);
        }
    }
}