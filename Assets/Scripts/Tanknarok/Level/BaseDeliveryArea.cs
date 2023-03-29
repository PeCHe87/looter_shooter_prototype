using Fusion;
using UnityEngine;
using UnityEngine.UI;
using static FusionExamples.Tanknarok.GameLauncher;

namespace FusionExamples.Tanknarok
{
    public class BaseDeliveryArea : NetworkBehaviour
    {
        #region Inspector

        [SerializeField] private int _capacity;
        [SerializeField] private TeamEnum _initialTeam = TeamEnum.NONE;
        [SerializeField] private Image _progressBarFill = default;
        [SerializeField] private Image _iconActive = default;
        [SerializeField] private Image _iconCompleted = default;
        [SerializeField] private GameObject _teamArea = default;
        [SerializeField] private Material _materialBlue = default;
        [SerializeField] private Material _materialRed = default;
        [SerializeField] private AudioEmitter _audioEmitter = default;
        [SerializeField] private AudioClipData _audioClip = default;
        [SerializeField] private ParticleSystem _vfx = default;
        [SerializeField] private Color _colorBlue = default;
        [SerializeField] private Color _colorRed = default;

        #endregion

        #region Networked properties

        [Networked(OnChanged = nameof(OnTeamChanged))]
        public int team { get; set; }

        [Networked(OnChanged = nameof(OnAmountChanged))]
        public int amount { get; set; }

        #endregion

        #region Networked methods

        public override void Spawned()
        {
            this.amount = 0;
            this.team = (int)_initialTeam;

            _vfx.Stop();

            RefreshMaterial();

            RefreshAmount();
        }

        #endregion

        #region Public methods

        public bool Interact(TeamEnum playerTeam, int amountToDeposit)
        {
            var deliveryTeam = (TeamEnum)this.team;

            if (deliveryTeam != TeamEnum.NONE && deliveryTeam != playerTeam) return false;

            // Refresh team for first time
            if (deliveryTeam == TeamEnum.NONE)
            {
                this.team = (int)playerTeam;

                RefreshMaterial();
            }

            if (this.amount >= _capacity) return false;

            this.amount = Mathf.Clamp(this.amount + amountToDeposit, 0, _capacity);

            RefreshAmount();

            _audioEmitter.PlayOneShot(_audioClip);

            _vfx.Play();

            return true;
        }

        #endregion

        #region Team methods

        public static void OnTeamChanged(Changed<BaseDeliveryArea> changed)
        {
            if (!changed.Behaviour) return;

            changed.Behaviour.OnTeamChanged();
        }

        private void OnTeamChanged()
        {
            RefreshMaterial();
        }

        private void RefreshMaterial()
        {
            var renderer = _teamArea.GetComponent<Renderer>();

            renderer.material = ((TeamEnum)this.team == TeamEnum.BLUE) ? _materialBlue : _materialRed;
        }

        #endregion

        #region Capacity methods

        public static void OnAmountChanged(Changed<BaseDeliveryArea> changed)
        {
            if (!changed.Behaviour) return;

            changed.Behaviour.OnAmountChanged();
        }

        private void OnAmountChanged()
        {
            RefreshAmount();

            _audioEmitter.PlayOneShot(_audioClip);

            var mainModule = _vfx.main;

            mainModule.startColor = ((TeamEnum)this.team == TeamEnum.BLUE) ? _colorBlue : _colorRed;

            _vfx.Play();
        }

        private void RefreshAmount()
        {
            var progress = (float)this.amount / (float)_capacity;

            _progressBarFill.fillAmount = progress;

            if (progress < 1) return;

            ShowCompletion();
        }

        private void ShowCompletion()
        {
            _iconCompleted.enabled = true;
        }

        #endregion
    }
}