using Fusion;
using FusionExamples.Tanknarok.Items;
using System.Linq;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
    // TODO: move this struct outside to another scriptable object file
    [System.Serializable]
    public struct LootboxData
    {
        public string id;
        public string coins;
    }

    public class LootboxBase : NetworkBehaviour
    {
        public static System.Action<LootData, string> OnOpen;

        #region Inspector

        [SerializeField] private MeshRenderer _loading;
        [SerializeField] private GameObject _open;
        [SerializeField] private float _openingTime = 4;
        [SerializeField] private Color _colorTeamBlue = default;
        [SerializeField] private Color _colorTeamRed = default;

        #endregion

        #region Networked properties

        [Networked] private NetworkBool _isOccupied { get; set; }
        [Networked(OnChanged = nameof(OnIsOpeningChanged))] private NetworkBool _isOpening { get; set; }
        [Networked] private TickTimer _delay { get; set; }
        [Networked] private GameLauncher.TeamEnum _playerTeam { get; set; }
        [Networked(OnChanged = nameof(OnIsOpenChanged))] private NetworkBool _isOpen { get; set; }
        [Networked] private string _playerId { get; set; }
        [Networked] public ref LootData _lootData => ref MakeRef<LootData>();

        #endregion

        #region Public methods

        public void StartInteracting(string id, GameLauncher.TeamEnum team)
        {
            if (_isOccupied) return;

            if (_isOpening) return;

            if (_isOpen)
            {
                _isOccupied = true;
                _playerId = id;

                ShowOpen();

                return;
            }

            StartOpening(id, team);

            return;
        }

        public void StopInteracting(string id)
        {
            _isOccupied = false;
            _playerId = string.Empty;
            _isOpening = false;

            HideLoading();
        }

        public void Take(int id)
        {
            for (int i = 0; i < _lootData.items.Length; i++)
            {
                var item = _lootData.items[i];
                if (item.id != id) continue;

                item.amount = 0;

                _lootData.items.Set(i, item);

                break;
            }
        }

        #endregion

        #region Networked methods

        public override void Spawned()
        {
            HideLoading();
        }

        public override void FixedUpdateNetwork()
        {
            if (_isOpen) return;

            if (!_isOpening) return;

            if (!_delay.Expired(Runner)) return;

            MarkAsOpen();
        }

        public override void Render()
        {
            if (!_isOpening) return;

            var remaining = _delay.RemainingTime(Runner);
            var remainingTime = (remaining / _openingTime);

            var progress = 1 - remainingTime ?? 0;

            RefreshOpeningProgress(progress);
        }

        #endregion

        #region Private methods

        private void StartOpening(string id, GameLauncher.TeamEnum team)
        {
            _playerId = id;
            _playerTeam = team;

            _isOccupied = true;
            _delay = TickTimer.CreateFromSeconds(Runner, _openingTime);

            _isOpening = true;

            _loading.material.color = (team == GameLauncher.TeamEnum.BLUE) ? _colorTeamBlue : _colorTeamRed;
        }

        private void MarkAsOpen()
        {
            _isOpening = false;
            _isOpen = true;

            ShowOpen();
        }

        private void ShowOpen()
        {
            OnOpen?.Invoke(_lootData, _playerId);

            _open.Toggle(true);

            HideLoading();
        }

        private void HideLoading()
        {
            _loading.material.SetFloat("_Recharge", 0);
        }

        private void RefreshOpeningProgress(float progress)
        {
            _loading.material.SetFloat("_Recharge", progress);
        }

        #endregion

        #region Remote methods

        public static void OnIsOpeningChanged(Changed<LootboxBase> changed)
        {
            if (!changed.Behaviour) return;

            if (changed.Behaviour._isOpening)
            {
                changed.Behaviour.StartOpening_Remote();
                return;
            }

            changed.Behaviour.StopOpening_Remote();
        }

        private void StartOpening_Remote()
        {
            _loading.material.color = (_playerTeam == GameLauncher.TeamEnum.BLUE) ? _colorTeamBlue : _colorTeamRed;
        }

        private void StopOpening_Remote()
        {
            HideLoading();
        }

        public static void OnIsOpenChanged(Changed<LootboxBase> changed)
        {
            if (!changed.Behaviour) return;

            if (!changed.Behaviour._isOpen) return;
           
            changed.Behaviour.Open_Remote();
        }

        private void Open_Remote()
        {
            ShowOpen();
        }

        #endregion
    }
}