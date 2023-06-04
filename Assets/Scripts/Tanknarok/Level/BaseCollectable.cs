using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FusionExamples.Tanknarok
{
    public class BaseCollectable : NetworkBehaviour
    {
		#region Inspector

		[SerializeField] private PowerupElement[] _powerupElements;
		[SerializeField] private Renderer _renderer;
		[SerializeField] private MeshFilter _meshFilter;
		[SerializeField] private MeshRenderer _rechargeCircle;
		[SerializeField] private float _respawnDuration = 5f;
		[SerializeField] private int _minAmount = 5;
		[SerializeField] private int _maxAmount = 10;

		[Header("Colors")]
		[SerializeField] private Color _mainPowerupColor;
		[SerializeField] private Color _specialPowerupColor;
		[SerializeField] private Color _buffPowerupColor;

		#endregion

		#region Networked properties

		[Networked(OnChanged = nameof(OnRespawningChanged))]
		public NetworkBool isRespawning { get; set; }

		[Networked(OnChanged = nameof(OnActivePowerupIndexChanged))]
		public int activePowerupIndex { get; set; }

		[Networked] public float respawnTimerFloat { get; set; }

		#endregion

		#region Private properties

		private int _amount = 5;
		private BoxCollider _boxCollider = default;

        #endregion

        #region Public properties

        public float respawnProgress => respawnTimerFloat / _respawnDuration;
		public int Amount => _amount;
		public bool IsRespawning => isRespawning;

		#endregion

		#region Unity events

		void OnEnable()
		{
			SetRechargeAmount(0f);
		}

        #endregion

        #region Networked methods

        public override void Spawned()
		{
			_boxCollider = GetComponent<BoxCollider>();
			_renderer.enabled = false;
			isRespawning = true;
			SetNextPowerup();
		}

		public override void FixedUpdateNetwork()
		{
			if (!Object.HasStateAuthority) return;

			// Update the respawn timer
			respawnTimerFloat = Mathf.Min(respawnTimerFloat + Runner.DeltaTime, _respawnDuration);

			// Spawn a new powerup whenever the respawn duration has been reached
			if (respawnTimerFloat >= _respawnDuration && isRespawning)
			{
				isRespawning = false;

				_boxCollider.enabled = true;
			}
		}

		// Create a simple scale in effect when spawning
		public override void Render()
		{
			if (!isRespawning)
			{
				_renderer.transform.localScale = Vector3.Lerp(_renderer.transform.localScale, Vector3.one, Time.deltaTime * 5f);
			}
			else
			{
				_renderer.transform.localScale = Vector3.zero;
				SetRechargeAmount(respawnProgress);
			}
		}

        #endregion

        #region Public methods

        /// <summary>
        /// Get the pickup contained in this spawner and trigger the spawning of a new powerup
        /// </summary>
        /// <returns></returns>
        public bool Pickup()
		{
			if (isRespawning) return false;

			_boxCollider.enabled = false;

			// Store the active powerup index for returning
			int lastIndex = activePowerupIndex;

			// Trigger the pickup effect, hide the powerup and select the next powerup to spawn
			if (respawnTimerFloat >= _respawnDuration)
			{
				if (_renderer.enabled)
				{
					GetComponent<AudioEmitter>().PlayOneShot(_powerupElements[lastIndex].pickupSnd);
					_renderer.enabled = false;
					SetNextPowerup();
				}
			}
			return lastIndex != -1 ? true : false;
		}

		#endregion

		#region Private methods

		private void SetNextPowerup()
		{
			if (!Object.HasStateAuthority) return;
			
			activePowerupIndex = Random.Range(0, _powerupElements.Length);
			respawnTimerFloat = 0;
			isRespawning = true;

			RandomizeAmount();
		}

		private void RandomizeAmount()
        {
			_amount = UnityEngine.Random.Range(_minAmount, _maxAmount);
		}

		public static void OnActivePowerupIndexChanged(Changed<BaseCollectable> changed)
		{
			changed.Behaviour.RefreshColor();
		}

		public static void OnRespawningChanged(Changed<BaseCollectable> changed)
		{
			if (!changed.Behaviour) return;

			changed.Behaviour.OnRespawningChanged();
		}

		private void OnRespawningChanged()
		{
			_renderer.enabled = true;
			_meshFilter.mesh = _powerupElements[activePowerupIndex].powerupSpawnerMesh;
			SetRechargeAmount(0);
		}

		private void RefreshColor()
		{
			if (_rechargeCircle != null)
			{
				Color respawnColor = _mainPowerupColor;
				switch (_powerupElements[activePowerupIndex].weaponInstallationType)
				{
					case WeaponManager.WeaponInstallationType.PRIMARY:
						respawnColor = _mainPowerupColor;
						break;
					case WeaponManager.WeaponInstallationType.SECONDARY:
						respawnColor = _specialPowerupColor;
						break;
					case WeaponManager.WeaponInstallationType.BUFF:
						respawnColor = _buffPowerupColor;
						break;
				}
				_rechargeCircle.material.color = respawnColor;
			}
		}

		public void SetRechargeAmount(float amount)
		{
			_rechargeCircle.material.SetFloat("_Recharge", amount);
		}

		#endregion
	}
}