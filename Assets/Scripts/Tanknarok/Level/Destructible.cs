using Fusion;
using UnityEngine;

namespace FusionExamples.Tanknarok.Gameplay
{
	/// Simple destructible that can be destroyed by either getting run over by a tank or taking damage from an explosion.
	/// This is not a network object but relies on local physics triggers. This is not recommended for any behaviour that
	/// has actual gameplay impact, since it is possible for these triggers to be slightly off between different clients.
	/// In this case, destructibles are just visual decorations and the triggers (tanks and explosions) are sufficiently slow moving, that it works.
	
	public class Destructible : NetworkBehaviour, ICanTakeDamage
	{
		public System.Action OnDestroyed;

        #region Inspector

        [SerializeField] private ParticleSystem _destroyedParticlePrefab;
		[SerializeField] private GameObject _visual;
		[SerializeField] private Collider _trigger;
		[SerializeField] private GameObject _debrisPrefab;
		[SerializeField] private LayerMask _destroyedByLayers;
		[SerializeField] private bool _enabled = true;
		[SerializeField] private AudioEmitter _audioEmitter;

        #endregion

        #region Private properties 

        private ParticleSystem _destroyedParticle;
		private GameObject _debris;
		private bool _isDestroyed = false;
		private Hitbox _hitBox = default;
		private HitboxRoot _hitBoxRoot = default;

        #endregion

        #region Private methods

        private void Start()
		{
			if (_destroyedParticlePrefab != null)
				_destroyedParticle = Instantiate(_destroyedParticlePrefab, transform.position, transform.rotation, transform.parent);

		}

		// Using OnEnable to make the destructible recyclable
		private void OnEnable()
		{
			if (_debris != null)
				Destroy(_debris);

			_trigger.enabled = true;
			_visual.SetActive(true);
		}

		private void OnTriggerEnter(Collider other)
		{
			/*
			if (!_enabled) return;

			if ( ((1<<other.gameObject.layer) & _destroyedByLayers) !=0 )
			{
				DestroyObject();
			}
			*/
		}

		private void DestroyObject()
		{
			if (_audioEmitter != null)
				_audioEmitter.PlayOneShot();

			_destroyedParticle?.Play();

			_trigger.enabled = false;
			_visual.SetActive(false);

			if (_debrisPrefab != null)
				_debris = Instantiate(_debrisPrefab, transform.parent);

			// Disable hit box
			if (TryGetComponent(out _hitBox))
			{
				Destroy(_hitBox);
			}

			// Disable hit box root
			if (TryGetComponent(out _hitBoxRoot))
			{
				Destroy(_hitBoxRoot);
			}

			OnDestroyed?.Invoke();
		}

        #endregion

        #region Networked methods

        public override void Spawned()
        {
			if (!Object.HasStateAuthority) return;
			
			_netHealth = (byte)UnityEngine.Random.Range(_minHp, _maxHp + 1);
		}

		#endregion

		#region Health

		[SerializeField] private int _minHp = default;
		[SerializeField] private int _maxHp = default;

		[Networked(OnChanged = nameof(OnHealthChanged))] public byte _netHealth { get; set; }

		public void ApplyDamage(Vector3 impulse, byte damage, PlayerRef source, Player attacker, GameObject hitVfx = null)
		{
			if (_isDestroyed) return;

			_netHealth = (byte)Mathf.Clamp(_netHealth - damage, 0, _maxHp);

			// DebugAttacker(_netHealth, _maxHp, damage, attacker);

			ShowHitVfx_Local(hitVfx);

			if (_netHealth > 0) return;

			Death_Local();
		}

		private void DebugAttacker(byte netHealth, int maxHp, byte damage, Player attacker)
		{
			if (attacker == null) return;

			Debug.LogError($"Destructible::ApplyDamage -> hp: <color=yellow>{netHealth}/{maxHp}</color>, damage: <color=cyan>{damage}</color>, is dead: <color=yellow>{netHealth == 0}</color>, attacker: <color=orange>{attacker.displayName}</color>");
		}

		private void ShowHitVfx_Local(GameObject prefabVfx)
        {
			if (prefabVfx == null) return;

			var vfx = Instantiate(prefabVfx, transform);

			vfx.transform.position = transform.position;
			vfx.transform.localRotation = Quaternion.identity;
			vfx.transform.localScale = Vector3.one * 2;

			if (!vfx.TryGetComponent<ParticleSystem>(out var particle)) return;

			particle.Play();

			Destroy(vfx, 2);
        }

		private void Death_Local()
        {
			_isDestroyed = true;

			_trigger.enabled = false;

			DestroyObject();
		}

		private void ShowHitVfx_Remote()
		{
			// TODO
		}

		private void Death_Remote()
        {
			if (HasStateAuthority) return;

			_isDestroyed = true;

			_trigger.enabled = false;

			DestroyObject();
		}

		public static void OnHealthChanged(Changed<Destructible> changed)
		{
			if (!changed.Behaviour) return;

			var health = changed.Behaviour._netHealth;

			changed.Behaviour.ShowHitVfx_Remote();

			if (health > 0) return;

			changed.Behaviour.Death_Remote();
		}

		#endregion
	}
}