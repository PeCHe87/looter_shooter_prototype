using Fusion;
using FusionExamples.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
    /// <summary>
    /// The Weapon class controls how fast a weapon fires, which projectiles it uses
    /// and the start position and direction of projectiles.
    /// </summary>
    public class Weapon : NetworkBehaviour
	{
        #region Inspector

        [SerializeField] private int _itemId = -1;
		[SerializeField] private Items.ItemWeaponType _weaponType = Items.ItemWeaponType.NONE;
		[SerializeField] private Transform[] _gunExits;
		[SerializeField] private Projectile _projectilePrefab; // Networked projectile
		[SerializeField] private float _rateOfFire;
		[SerializeField] private byte _initialAmmo;
		[SerializeField] private byte _ammo;
		[SerializeField] private bool _infiniteAmmo;
		[SerializeField] private AudioEmitter _audioEmitter;
		[SerializeField] private LaserSightLine _laserSight;
		[SerializeField] private PowerupType _powerupType = PowerupType.DEFAULT;
		[SerializeField] private ParticleSystem _muzzleFlashPrefab;
		[SerializeField] private float _reloadingTime = 2;

		[Header("Melee settings")]
		[SerializeField] private LayerMask _meleeHitMask = default;
		[SerializeField] private float _meleeAreaRadius = default;
		[SerializeField] private float _meleeAttackAngle = default;
		[SerializeField] private float _meleeAreaImpulse = default;
		[SerializeField] private byte _meleeDamage = default;
		[SerializeField] private bool _canDebugMelee = false;
		[SerializeField] private bool _drawMelee = false;

		#endregion

		#region Networked properties

		[Networked(OnChanged = nameof(OnFireTickChanged))]
		private int fireTick { get; set; }

		[Networked] private NetworkBool applyMeleeAttack { get; set; }
		[Networked] private Vector3 impactHitPoint { get; set; }
		[Networked] private Player player { get; set; }

		#endregion

		#region Private properties

		private int _gunExit;
		private float _visible;
		private bool _active;
		private List<ParticleSystem> _muzzleFlashList = new List<ParticleSystem>();

        #endregion

        #region Public properties

        public float delay => _rateOfFire;
		public bool isShowing => _visible >= 1.0f;
		public byte ammo => _ammo;
		public bool infiniteAmmo => _infiniteAmmo;
		public float ReloadingTime => _reloadingTime;
		public byte InitialAmmo => _initialAmmo;
		public int ItemId => _itemId;
		public Items.ItemWeaponType WeaponType => _weaponType;
		public PowerupType powerupType => _powerupType;

        #endregion

        private void Awake()
		{
			// Create a muzzle flash for each gun exit point the weapon has
			if (_muzzleFlashPrefab != null)
			{
				foreach (Transform gunExit in _gunExits)
				{
					_muzzleFlashList.Add(Instantiate(_muzzleFlashPrefab, gunExit.position, gunExit.rotation, transform));
				}
			}

			_laserSight?.Deactivate();
		}

		/// <summary>
		/// Control the visual appearance of the weapon. This is controlled by the Player based
		/// on the currently selected weapon, so the boolean parameter is entirely derived from a
		/// networked property (which is why nothing in this class is sync'ed).
		/// </summary>
		/// <param name="show">True if this weapon is currently active and should be visible</param>
		public void Show(bool show)
		{
			if (_active && !show)
			{
				ToggleActive(false);
			}
			else if (!_active && show)
			{
				ToggleActive(true);
			}

			_visible = Mathf.Clamp(_visible + (show ? Time.deltaTime : -Time.deltaTime) * 5f, 0, 1);

			if (show)
				transform.localScale = Tween.easeOutElastic(0, 1, _visible) * Vector3.one;
			else
				transform.localScale = Tween.easeInExpo(0, 1, _visible) * Vector3.one;
		}

		private void ToggleActive(bool value)
		{
			_active = value;

			/*
			if (_laserSight != null)
			{
				if (_active)
				{
					_laserSight.SetDuration(0.5f);
					_laserSight.Activate();
				}
				else
					_laserSight.Deactivate();
			}
			*/
		}

		/// <summary>
		/// Fire a weapon, spawning the bullet or, in the case of the hitscan, the visual
		/// effect that will indicate that a shot was fired.
		/// This is called in direct response to player input, but only on the server
		/// (It's filtered at the source in Player)
		/// </summary>
		/// <param name="runner"></param>
		/// <param name="owner"></param>
		/// <param name="ownerVelocity"></param>
		public void Fire(NetworkRunner runner, PlayerRef owner, Vector3 ownerVelocity, Player player)
		{
			if (powerupType == PowerupType.EMPTY || _gunExits.Length == 0)
				return;
			
			var exit = GetExitPoint();
			SpawnNetworkShot(runner, owner, exit, ownerVelocity, player);

			//exit = GetExitPoint();
			//SpawnNetworkShot(runner, owner, exit, ownerVelocity);

			//exit = GetExitPoint();
			//SpawnNetworkShot(runner, owner, exit, ownerVelocity);

			fireTick = Runner.Simulation.Tick;
		}

		public void MeleeAttack(NetworkRunner runner, PlayerRef owner, Vector3 ownerVelocity, Player player)
        {
			this.player = player;

			this.applyMeleeAttack = true;
		}

		public override void FixedUpdateNetwork()
        {
			if (!this.applyMeleeAttack) return;

			this.applyMeleeAttack = false;

			ApplyMeleeDamage();
		}

		public static void OnFireTickChanged(Changed<Weapon> changed)
		{
			changed.Behaviour.FireFx();
		}

		private void FireFx()
		{
			// Recharge the laser sight if this weapon has it
			//if (_laserSight != null)
			//	_laserSight.Recharge();

			if(_gunExit<_muzzleFlashList.Count)
				_muzzleFlashList[_gunExit].Play();
			_audioEmitter.PlayOneShot();
		}

		/// <summary>
		/// Spawn a bullet prefab with prediction.
		/// On the authoritative instance this is just a regular spawn (host in hosted mode or weapon owner in shared mode).
		/// In hosted mode, the client with Input Authority will spawn a local predicted instance that will be linked to
		/// the hosts network object when it arrives. This provides instant client-side feedback and seamless transition
		/// to the consolidated state.
		/// </summary>
		private void SpawnNetworkShot(NetworkRunner runner, PlayerRef owner, Transform exit, Vector3 ownerVelocity, Player player)
		{
			//Debug.Log($"Spawning Shot in tick {Runner.Simulation.Tick} stage={Runner.Simulation.Stage}");
			
			// Create a key that is unique to this shot on this client so that when we receive the actual NetworkObject
			// Fusion can match it against the predicted local bullet.
			var rawEncoded = (owner == null) ? -1 : owner.RawEncoded;

			var key = new NetworkObjectPredictionKey {Byte0 = (byte) rawEncoded, Byte1 = (byte) runner.Simulation.Tick};
			runner.Spawn(_projectilePrefab, exit.position, exit.rotation, owner, (runner, obj) =>
			{
				var entityType = (player.team == GameLauncher.TeamEnum.BLUE) ? EntityType.PLAYER_TEAM_BLUE : EntityType.PLAYER_TEAM_RED;

				var projectile = obj.GetComponent<Projectile>();

				projectile.SetOwner(player);
				projectile.InitNetworkState(ownerVelocity, entityType);

			}, key );
		}

		private Transform GetExitPoint()
		{
			// NOTE: use this line if you want variable exit points
			// TODO:_gunExit = (_gunExit + 1) % _gunExits.Length;
			
			_gunExit = 0;

			Transform exit = _gunExits[_gunExit];
			return exit;
		}

		public void OverrideConfiguration(int id, Items.EquipableItemCatalogData data)
        {
			_itemId = id;
			_weaponType = data.WeaponData.Type;

			if (data.WeaponData.Type == Items.ItemWeaponType.ASSAULT)
			{
				// Configure assault weapon values
				var assaultData = (Items.ItemWeaponAssaultData)data.WeaponData;

				_projectilePrefab = assaultData.ProjectilePrefab;
				_rateOfFire = assaultData.RateOfFire;
				_initialAmmo = assaultData.InitialAmmo;
				_ammo = assaultData.Ammo;
				_infiniteAmmo = assaultData.InfiniteAmmo;
				_powerupType = assaultData.PowerupType;
				_reloadingTime = assaultData.reloadingTime;
				_muzzleFlashPrefab = assaultData.MuzzleFlashPrefab;

				_muzzleFlashList.Clear();

				foreach (Transform gunExit in _gunExits)
				{
					_muzzleFlashList.Add(Instantiate(_muzzleFlashPrefab, gunExit.position, gunExit.rotation, transform));
				}

				return;
			}
            
			if (data.WeaponData.Type == Items.ItemWeaponType.MELEE)
			{
				// Configure melee weapon values
				var meleeData = (Items.ItemWeaponMeleeData)data.WeaponData;

				_meleeHitMask = meleeData.HitMask;
				_meleeAreaRadius = meleeData.Radius;
				_meleeAreaImpulse = meleeData.Impulse;
				_meleeDamage = (byte) meleeData.Damage;
				_meleeAttackAngle = meleeData.Angle;
			}
		}

		private void ApplyMeleeDamage()
        {
            Debug.LogError("<color=magenta>Weapon</color>::ApplyMeleeDamage");

			var areaHits = new List<LagCompensatedHit>();
			HitboxManager hbm = Runner.LagCompensation;
			int cnt = hbm.OverlapSphere(transform.position, _meleeAreaRadius, Object.InputAuthority, areaHits, _meleeHitMask, HitOptions.IncludePhysX);

			if (cnt <= 0) return;

			for (int i = 0; i < cnt; i++)
			{
				var targetObject = areaHits[i].GameObject;

				var targetPlayer = targetObject.GetComponent<Player>();

				// Avoid it self
				if (targetPlayer == this.player) continue;

				// Check if it is another player from the same team to skip it
				if (targetPlayer != null && targetPlayer.team == this.player.team) continue;

				// Check if target can take damage
				ICanTakeDamage target = targetObject.GetComponent<ICanTakeDamage>();

				if (target == null) continue;

				// TODO: check if it is inside the Attack angle area

				Vector3 impulse = targetObject.transform.position - transform.position;
				float l = Mathf.Clamp(_meleeAreaRadius - impulse.magnitude, 0, _meleeAreaRadius);
				impulse = _meleeAreaImpulse * l * impulse.normalized;
				target.ApplyDamage(impulse, _meleeDamage, Object.InputAuthority, this.player);

				Debug.LogError($"ApplyMeleeDamage to <color=yellow>{targetObject.name}</color>, damage: <color=cyan>{_meleeDamage}</color>");
			}
		}

		#region Debug
        
		[Header("Debug")]
		[SerializeField] private MeshFilter viewMeshFilter;
		[SerializeField] private float meshResolution;
		[SerializeField] private int edgeResolveIterations;
		[SerializeField] private float edgeDstThreshold;

		private Mesh viewMesh;

		public struct ViewCastInfo
		{
			public bool hit;
			public Vector3 point;
			public float dst;
			public float angle;

			public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
			{
				hit = _hit;
				point = _point;
				dst = _dst;
				angle = _angle;
			}
		}

		public struct EdgeInfo
		{
			public Vector3 pointA;
			public Vector3 pointB;

			public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
			{
				pointA = _pointA;
				pointB = _pointB;
			}
		}

		private void Start()
        {
			viewMesh = new Mesh();
			viewMesh.name = "View Mesh";

			if (viewMeshFilter == null) return;

			viewMeshFilter.mesh = viewMesh;
		}

        private void Update()
        {
			if (!_canDebugMelee) return;

			DrawFieldOfView();
		}

		private void DrawFieldOfView()
		{
			int stepCount = Mathf.RoundToInt(_meleeAttackAngle * meshResolution);
			float stepAngleSize = _meleeAttackAngle / stepCount;
			List<Vector3> viewPoints = new List<Vector3>();
			ViewCastInfo oldViewCast = new ViewCastInfo();
			for (int i = 0; i <= stepCount; i++)
			{
				float angle = transform.eulerAngles.y - _meleeAttackAngle / 2 + stepAngleSize * i;
				ViewCastInfo newViewCast = ViewCast(angle);

				if (i > 0)
				{
					bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
					if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
					{
						EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
						if (edge.pointA != Vector3.zero)
						{
							viewPoints.Add(edge.pointA);
						}
						if (edge.pointB != Vector3.zero)
						{
							viewPoints.Add(edge.pointB);
						}
					}

				}


				viewPoints.Add(newViewCast.point);
				oldViewCast = newViewCast;
			}

			int vertexCount = viewPoints.Count + 1;
			Vector3[] vertices = new Vector3[vertexCount];
			int[] triangles = new int[(vertexCount - 2) * 3];

			vertices[0] = Vector3.zero;
			for (int i = 0; i < vertexCount - 1; i++)
			{
				vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

				if (i < vertexCount - 2)
				{
					triangles[i * 3] = 0;
					triangles[i * 3 + 1] = i + 1;
					triangles[i * 3 + 2] = i + 2;
				}
			}

			viewMesh.Clear();

			viewMesh.vertices = vertices;
			viewMesh.triangles = triangles;
			viewMesh.RecalculateNormals();
		}

		EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
		{
			float minAngle = minViewCast.angle;
			float maxAngle = maxViewCast.angle;
			Vector3 minPoint = Vector3.zero;
			Vector3 maxPoint = Vector3.zero;

			for (int i = 0; i < edgeResolveIterations; i++)
			{
				float angle = (minAngle + maxAngle) / 2;
				ViewCastInfo newViewCast = ViewCast(angle);

				bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;
				if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
				{
					minAngle = angle;
					minPoint = newViewCast.point;
				}
				else
				{
					maxAngle = angle;
					maxPoint = newViewCast.point;
				}
			}

			return new EdgeInfo(minPoint, maxPoint);
		}

		ViewCastInfo ViewCast(float globalAngle)
		{
			Vector3 dir = DirFromAngle(globalAngle, true);
			RaycastHit hit;

			/*
			if (Physics.Raycast(transform.position, dir, out hit, _meleeAreaRadius, obstacleMask))
			{
				return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
			}
			else
			{
				return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);
			}
			*/

			return new ViewCastInfo(false, transform.position + dir * _meleeAreaRadius, _meleeAreaRadius, globalAngle);
		}

		public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
		{
			if (!angleIsGlobal)
			{
				angleInDegrees += transform.eulerAngles.y;
			}
			return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
		}

        #endregion
    }
}