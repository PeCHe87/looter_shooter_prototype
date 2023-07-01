
using Fusion;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
    public class ControlPoint : NetworkBehaviour
	{
		#region Inspector

		[SerializeField] private MeshRenderer _loading;
		[SerializeField] private Renderer _teamRenderer = default;
		[SerializeField] private Material _teamBlue = default;
		[SerializeField] private Material _teamRed = default;
		[SerializeField] private Material _teamNone = default;
		[SerializeField] private Color _colorTeamBlue = default;
		[SerializeField] private Color _colorTeamRed = default;
		[SerializeField] private float _openingTime = 4;
		[SerializeField] private SpriteRenderer _miniMapIndicator = default;
		[SerializeField] private SpriteRenderer _interactionIndicator = default;

		#endregion

		#region Networked properties

		[Networked(OnChanged = nameof(OnTeamChanged))] private GameLauncher.TeamEnum _playerTeam { get; set; }
		[Networked(OnChanged = nameof(OnInteractionChanged))] private NetworkBool _isInteracting { get; set; }
		[Networked] private string _playerId { get; set; }
		[Networked] private TickTimer _delay { get; set; }
		[Networked] private GameLauncher.TeamEnum _teamInProgress { get; set; }

		#endregion

		#region Public methods

		public void StartInteracting(string id, GameLauncher.TeamEnum team)
		{
			_teamInProgress = team;

			_isInteracting = true;

			_playerId = id;

			_delay = TickTimer.CreateFromSeconds(Runner, _openingTime);

			_loading.material.color = (team == GameLauncher.TeamEnum.BLUE) ? _colorTeamBlue : _colorTeamRed;

			RefreshInteractionColor();
		}

		public void StopInteracting(string id)
		{
			if (!_playerId.Equals(id)) return;

			_playerId = string.Empty;

			_isInteracting = false;

			HideLoading();
		}

		public bool CanInteract(GameLauncher.TeamEnum team)
		{
			// Check if it was already taken
			if (_playerTeam == team) return false;

			return !_isInteracting;
		}

		#endregion

		#region Networked methods

		public override void Spawned()
		{
			HideLoading();

			if (!Object.HasStateAuthority) return;

			_playerTeam = GameLauncher.TeamEnum.NONE;

			RefreshTeamColor();
		}

		public override void FixedUpdateNetwork()
		{
			if (!_isInteracting) return;

			if (!_delay.Expired(Runner)) return;

			CompleteInteraction();
		}

		public override void Render()
		{
			if (!_isInteracting) return;

			var remaining = _delay.RemainingTime(Runner);
			var remainingTime = (remaining / _openingTime);

			var progress = 1 - remainingTime ?? 0;

			RefreshOpeningProgress(progress);
		}

		#endregion

		#region Private methods

		private void HideLoading()
		{
			_loading.material.SetFloat("_Recharge", 0);
			_interactionIndicator.gameObject.Toggle(false);
		}

		private void RefreshOpeningProgress(float progress)
		{
			_loading.material.SetFloat("_Recharge", progress);

			_interactionIndicator.gameObject.Toggle(true);
		}

		public static void OnTeamChanged(Changed<ControlPoint> changed)
		{
			if (!changed.Behaviour) return;

			changed.Behaviour.ChangeTeam_Remote();
		}

		private void RefreshTeamColor()
		{
			if (_playerTeam == GameLauncher.TeamEnum.BLUE)
			{
				_teamRenderer.material = _teamBlue;
				_miniMapIndicator.color = _colorTeamBlue;

				return;
			}

			if (_playerTeam == GameLauncher.TeamEnum.RED)
			{
				_teamRenderer.material = _teamRed;
				_miniMapIndicator.color = _colorTeamRed;

				return;
			}

			_teamRenderer.material = _teamNone;
			_miniMapIndicator.color = Color.white;
		}

		private void ChangeTeam_Remote()
		{
			if (Object.HasStateAuthority) return;

			RefreshTeamColor();
		}

		private void CompleteInteraction()
		{
			_isInteracting = false;

			_playerId = string.Empty;

			_playerTeam = _teamInProgress;

			RefreshTeamColor();

			HideLoading();
		}

		public static void OnInteractionChanged(Changed<ControlPoint> changed)
		{
			if (!changed.Behaviour) return;

			changed.Behaviour.ChangeInteraction_Remote();
		}

		private void ChangeInteraction_Remote()
		{
			if (Object.HasStateAuthority) return;

			if (_isInteracting)
			{
				RefreshInteractionColor();
				return;
			}

			HideLoading();
		}

		private void RefreshInteractionColor()
		{
			if (_teamInProgress == GameLauncher.TeamEnum.BLUE)
			{
				_interactionIndicator.color = _colorTeamBlue;

				return;
			}

			if (_teamInProgress == GameLauncher.TeamEnum.RED)
			{
				_interactionIndicator.color = _colorTeamRed;

				return;
			}
		}

		#endregion
	}
}