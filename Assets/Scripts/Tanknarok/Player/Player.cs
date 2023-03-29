using System.Collections;
using System.Threading.Tasks;
using Fusion;
using FusionExamples.Tanknarok.CharacterAbilities;
using FusionExamples.Tanknarok.UI;
using UnityEngine;
using static FusionExamples.Tanknarok.GameLauncher;

namespace FusionExamples.Tanknarok
{
	/// <summary>
	/// The Player class represent the players avatar - in this case the Tank.
	/// </summary>
	[RequireComponent(typeof(NetworkCharacterControllerPrototype))]
	public class Player : NetworkBehaviour, ICanTakeDamage
	{
        #region Consts

        public const byte MAX_HEALTH = 100;

        #endregion

        #region Inspector

        [Header("Visuals")]
		[SerializeField] private Transform _body;
		[SerializeField] private Transform _hull;
		[SerializeField] private Transform _turret;
		[SerializeField] private Transform _visualParent;
		[SerializeField] private Material[] _playerMaterials;
		[SerializeField] private TankTeleportInEffect _teleportIn;
		[SerializeField] private TankTeleportOutEffect _teleportOut;
		[SerializeField] private PlayerFloatingHud _hud = default;

		[Space(10)] 
		[SerializeField] private GameObject _deathExplosionPrefab;
		[SerializeField] private LayerMask _groundMask;
		[SerializeField] private float _pickupRadius;
		[SerializeField] private float _respawnTime;
		[SerializeField] private LayerMask _pickupMask;
		[SerializeField] private WeaponManager weaponManager;
		[SerializeField] private TargeteableBase _targeteableBase = default;

        #endregion

        #region Networked properties

        [Networked(OnChanged = nameof(OnStateChanged))]
		public State state { get; set; }

		[Networked]
		public byte life { get; set; }

		[Networked]
		public NetworkString<_128> playerName { get; set; }

		[Networked]
		private Vector2 moveDirection { get; set; }

		[Networked]
		private Vector2 aimDirection { get; set; }

		[Networked]
		private TickTimer respawnTimer { get; set; }

		[Networked]
		private TickTimer invulnerabilityTimer { get; set; }

		[Networked]
		public byte lives { get; set; }

		[Networked]
		public byte score { get; set; }

		[Networked]
		public NetworkBool ready { get; set; }
		[Networked(OnChanged = nameof(OnDisplayNameChanged))] 
		public string displayName { get; set; }
		[Networked(OnChanged = nameof(OnTeamChanged))] 
		public TeamEnum team { get; set; }

		#endregion

		#region Enums

		public enum State
		{
			New,
			Despawned,
			Spawning,
			Active,
			Dead
		}

        enum DriveDirection
		{
			FORWARD,
			BACKWARD
		};

        #endregion

        #region Public properties

        public static Player local { get; set; }

		public bool isActivated => (gameObject.activeInHierarchy && (state == State.Active || state == State.Spawning));
		public bool isDead => state == State.Dead;
		public bool isRespawningDone => state == State.Spawning && respawnTimer.Expired(Runner);

		public Material playerMaterial { get; set; }
		public Color playerColor => playerMaterial.GetColor("_EnergyColor");
		public WeaponManager shooter => weaponManager;

		public int playerID { get; private set; }
		public Vector3 velocity => _cc.Velocity;
		public Vector3 turretPosition => _turret.position;

		public Quaternion turretRotation => _turret.rotation;

		public Quaternion hullRotation => _hull.rotation;
		public Vector3 MoveDirection => this.moveDirection;

        #endregion

        #region Private properties

        private DriveDirection _driveDirection = DriveDirection.FORWARD;

		private Transform _transform = default;
		private NetworkCharacterControllerPrototype _cc;
		private Collider[] _overlaps = new Collider[1];
		private Collider _collider;
		private HitboxRoot _hitBoxRoot;
		private LevelManager _levelManager;
		private Vector2 _lastMoveDirection; // Store the previous direction for correct hull rotation
		private GameObject _deathExplosionInstance;
		private TankDamageVisual _damageVisuals;
		private float _respawnInSeconds = -1;
		private float _originalGravity = 0;
		private float _originalMaxSpeed = 0;

        #endregion

        #region Original methods

        public void ToggleReady()
		{
			ready = !ready;
		}

		public void ResetReady()
		{
			ready = false;
		}

		private void Awake()
		{
			_cc = GetComponent<NetworkCharacterControllerPrototype>();
			_collider = GetComponentInChildren<Collider>();
			_hitBoxRoot = GetComponent<HitboxRoot>();
		}

		private LevelManager GetLevelManager()
		{
			if (_levelManager == null)
				_levelManager = FindObjectOfType<LevelManager>();
			return _levelManager;
		}
		
		public void InitNetworkState(byte maxLives)
		{
			state = State.New;
			lives = maxLives;
			life = MAX_HEALTH;
			score = 0;
		}

		public override void Spawned()
		{
			_transform = transform;

			if (Object.HasInputAuthority)
				local = this;

			var isLocal = Object.HasInputAuthority;

			if (isLocal)
            {
				var displayName = PlayerPrefs.GetString("playerDisplayName");
				var team = (TeamEnum)PlayerPrefs.GetInt("playerTeam");

				RPC_SetInformation(displayName, team);
            }

			targetDetector.Init(isLocal);
			
			_targeteableBase.SetId(Id.ToString(), Object.HasInputAuthority);

			_originalGravity = _cc.gravity;
			_originalMaxSpeed = _cc.maxSpeed;

			// Getting this here because it will revert to -1 if the player disconnects, but we still want to remember the Id we were assigned for clean-up purposes
			playerID = Object.InputAuthority;
			ready = false;

			SetMaterial();
			SetupDeathExplosion();

			_teleportIn.Initialize(this);
			_teleportOut.Initialize(this);

			_damageVisuals = GetComponent<TankDamageVisual>();
			_damageVisuals.Initialize(playerMaterial);

			PlayerManager.AddPlayer(this);
			
			// Auto will set proxies to InterpolationDataSources.Snapshots and State/Input authority to InterpolationDataSources.Predicted
			// The NCC must use snapshots on proxies for lag compensated raycasts to work properly against them.
			// The benefit of "Auto" is that it will update automatically if InputAuthority is changed (this is not relevant in this game, but worth keeping in mind)
			GetComponent<NetworkCharacterControllerPrototype>().InterpolationDataSource = InterpolationDataSources.Auto;
		}

		void SetupDeathExplosion()
		{
			_deathExplosionInstance = Instantiate(_deathExplosionPrefab, transform.parent);
			_deathExplosionInstance.SetActive(false);
			ColorChanger.ChangeColor(_deathExplosionInstance.transform, playerColor);
		}

		public override void FixedUpdateNetwork()
		{
			if (Object.HasStateAuthority)
			{
				if (_respawnInSeconds >= 0)
					CheckRespawn();

				if (isRespawningDone)
					ResetPlayer();

				CheckDashFinalization();
			}

			CheckForPowerupPickup();

			CheckForCollectablePickup();

			CheckForDelivery();
		}

		/// <summary>
		/// Render is the Fusion equivalent of Unity's Update() and unlike FixedUpdateNetwork which is very different from FixedUpdate,
		/// Render is in fact exactly the same. It even uses the same Time.deltaTime time steps. The purpose of Render is that
		/// it is always called *after* FixedUpdateNetwork - so to be safe you should use Render over Update if you're on a
		/// SimulationBehaviour.
		///
		/// Here, we use Render to update visual aspects of the Tank that does not involve changing of networked properties.
		/// </summary>
		public override void Render()
		{
			_visualParent.gameObject.SetActive(state == State.Active);
			_collider.enabled = state != State.Dead;
			_hitBoxRoot.HitboxRootActive = state == State.Active;
			_damageVisuals.CheckHealth(life);

			// Add a little visual-only movement to the mesh
			SetMeshOrientation();

			if (moveDirection.magnitude > 0.1f)
				_lastMoveDirection = moveDirection;

			targetDetector.UpdateMovementDirection(new Vector3(moveDirection.x, 0, moveDirection.y));
		}

		private void SetMaterial()
		{
			playerMaterial = Instantiate(_playerMaterials[playerID]);
			TankPartMesh[] tankParts = GetComponentsInChildren<TankPartMesh>();
			foreach (TankPartMesh part in tankParts)
			{
				part.SetMaterial(playerMaterial);
			}
		}

		/// <summary>
		/// Control the rotation of hull and turret
		/// </summary>
		private void SetMeshOrientation()
		{
			// To prevent the tank from making a 180 degree turn every time we reverse the movement direction
			// we define a driving direction that creates a multiplier for the hull.forward. This allows us to
			// drive "backwards" as well as "forwards"
			switch (_driveDirection)
			{
				case DriveDirection.FORWARD:
					if (moveDirection.magnitude > 0.1f && Vector3.Dot(_lastMoveDirection, moveDirection.normalized) < 0f)
						_driveDirection = DriveDirection.BACKWARD;
					break;
				case DriveDirection.BACKWARD:
					if (moveDirection.magnitude > 0.1f && Vector3.Dot(_lastMoveDirection, moveDirection.normalized) < 0f)
						_driveDirection = DriveDirection.FORWARD;
					break;
			}

			float multiplier = _driveDirection == DriveDirection.FORWARD ? 1 : -1;

			//if (moveDirection.magnitude > 0.1f)
			//	_hull.forward = Vector3.Lerp(_hull.forward, new Vector3( moveDirection.x,0,moveDirection.y ) * multiplier, Time.deltaTime * 10f);

			//ProcessRotation();
		}

		/// <summary>
		/// Set the direction of movement and aim
		/// </summary>
		public void SetDirections(Vector2 moveDirection, Vector2 aimDirection)
		{
			this.moveDirection = moveDirection;

			// Following mouse direction
			//this.aimDirection = aimDirection;

			// Following target detection if target was detected
			ProcessAiming();
		}

		public void Move()
		{
			if (!isActivated) return;

			if (DashInProgress())
            {
				ApplyDash();
				return;
            }

			//_cc.Move(new Vector3(moveDirection.x,0,moveDirection.y));

			var processAiming = targetDetector.TargetFound;

			ProcessAiming();

			_cc.MoveWithRotation(new Vector3(moveDirection.x, 0, moveDirection.y), this.aimingDirection, processAiming);
		}

		/// <summary>
		/// Apply an impulse to the Tank - in the absence of a rigidbody and rigidbody physics, we're emulating a physical impact by
		/// adding directly to the Tanks controller velocity. I'm sure Newton is doing a few extra turns in his grave over this, but for a
		/// cartoon style game like this, it's all about how it looks and feels, and not so much about being correct :)...
		/// </summary>
		/// <param name="impulse">Size and direction of the impulse</param>
		public void ApplyImpulse(Vector3 impulse)
		{
			if (!isActivated)
				return;

			if (Object.HasStateAuthority)
			{
				_cc.Velocity += impulse / 10.0f; // Magic constant to compensate for not properly dealing with masses
				_cc.Move(Vector3.zero); // Velocity property is only used by CC when steering, so pretend we are, without actually steering anywhere
			}
		}

		/// <summary>
		/// Apply damage to Tank with an associated impact impulse
		/// </summary>
		/// <param name="impulse"></param>
		/// <param name="damage"></param>
		/// <param name="attacker"></param>
		public void ApplyDamage(Vector3 impulse, byte damage, PlayerRef attacker)
		{
			if (!isActivated || !invulnerabilityTimer.Expired(Runner))
				return;

			//Don't damage yourself
			Player attackingPlayer = PlayerManager.Get(attacker);
			if (attackingPlayer != null && attackingPlayer.playerID == playerID)
				return;

			ApplyImpulse(impulse);

			if (damage >= life)
			{
				life = 0;
				state = State.Dead;
				
				if(GameManager.playState==GameManager.PlayState.LEVEL)
					lives -= 1;

				//if (lives > 0)
					Respawn( _respawnTime );

				RefreshCollectablesOnDeath();

				GameManager.instance.OnTankDeath();
			}
			else
			{
				life -= damage;
				Debug.Log($"Player {playerID} took {damage} damage, life = {life}");
			}

			invulnerabilityTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);

			if (Runner.Stage == SimulationStages.Forward)
				_damageVisuals.OnDamaged(life, isDead);
		}
		
		public void Respawn(float inSeconds)
		{
			_respawnInSeconds = inSeconds;
		}

		private void CheckRespawn()
		{
			if(_respawnInSeconds>0)
				_respawnInSeconds -= Runner.DeltaTime;
			SpawnPoint spawnpt = GetLevelManager().GetPlayerSpawnPoint(playerID);
			if (spawnpt!=null && _respawnInSeconds <= 0)
			{
				Debug.Log($"Respawning player {playerID}, life={life}, lives={lives}, hasAuthority={Object.HasStateAuthority} from state={state}");

				// Make sure we don't get in here again, even if we hit exactly zero
				_respawnInSeconds = -1;

				// Restore health
				life = MAX_HEALTH;

				// Start the respawn timer and trigger the teleport in effect
				respawnTimer = TickTimer.CreateFromSeconds(Runner, 1);
				invulnerabilityTimer = TickTimer.CreateFromSeconds(Runner, 1);

				// Place the tank at its spawn point. This has to be done in FUN() because the transform gets reset otherwise
				Transform spawn = spawnpt.transform;
				transform.position = spawn.position;
				transform.rotation = spawn.rotation;

				// If the player was already here when we joined, it might already be active, in which case we don't want to trigger any spawn FX, so just leave it ACTIVE
				if(state!=State.Active)
					state = State.Spawning;
	
				Debug.Log($"Respawned player {playerID}, tick={Runner.Simulation.Tick}, timer={respawnTimer.IsRunning}:{respawnTimer.TargetTick}, life={life}, lives={lives}, hasAuthority={Object.HasStateAuthority} to state={state}");
			}
		}
		
		public static void OnStateChanged(Changed<Player> changed)
		{
			if(changed.Behaviour)
				changed.Behaviour.OnStateChanged();
		}

		public void OnStateChanged()
		{
			switch (state)
			{
				case State.Spawning:
					_teleportIn.StartTeleport();
					break;
				case State.Active:
					_damageVisuals.CleanUpDebris();
					_teleportIn.EndTeleport();
					break;
				case State.Dead:
					_deathExplosionInstance.transform.position = transform.position;
					_deathExplosionInstance.SetActive(false); // dirty fix to reactivate the death explosion if the particlesystem is still active
					_deathExplosionInstance.SetActive(true);

					_visualParent.gameObject.SetActive(false);
					_damageVisuals.OnDeath();
					break;
				case State.Despawned:
					_teleportOut.StartTeleport();
					break;
			}
		}

		private void ResetPlayer()
		{
			Debug.Log($"Resetting player {playerID}, tick={Runner.Simulation.Tick}, timer={respawnTimer.IsRunning}:{respawnTimer.TargetTick}, life={life}, lives={lives}, hasAuthority={Object.HasStateAuthority} to state={state}");
			shooter.ResetAllWeapons();
			state = State.Active;
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			Destroy(_deathExplosionInstance);
			PlayerManager.RemovePlayer(this);
		}

		public void DespawnTank()
		{
			if (state == State.Dead)
				return;

			state = State.Despawned;
		}

		/// <summary>
		/// Called when a player collides with a powerup.
		/// </summary>
		public void Pickup(PowerupSpawner powerupSpawner)
		{
			if (!powerupSpawner)
				return;

			PowerupElement powerup = powerupSpawner.Pickup();

			if (powerup == null)
				return;

			if (powerup.powerupType == PowerupType.HEALTH)
				life = MAX_HEALTH;
			else
				shooter.InstallWeapon(powerup);
		}

		private void CheckForPowerupPickup()
		{
			// If we run into a powerup, pick it up
			if (isActivated && Runner.GetPhysicsScene().OverlapSphere(transform.position, _pickupRadius, _overlaps, _pickupMask, QueryTriggerInteraction.Collide) > 0)
			{
				Pickup(_overlaps[0].GetComponent<PowerupSpawner>());
			}
		}

		public async void TriggerDespawn()
		{
			DespawnTank();
			PlayerManager.RemovePlayer(this);

			await Task.Delay(300); // wait for effects

			if (Object == null) { return; }

			if (Object.HasStateAuthority)
			{
				Runner.Despawn(Object);
			}
			else if (Runner.IsSharedModeMasterClient)
			{
				Object.RequestStateAuthority();

				while (Object.HasStateAuthority == false)
				{
					await Task.Delay(100); // wait for Auth transfer
				}

				if (Object.HasStateAuthority)
				{
					Runner.Despawn(Object);
				}
			}
		}

		#endregion

		#region Dash

		[Space(10)]
		[Header("Dash configuration")]
		[SerializeField] private float _dashForce = 50;
		[SerializeField] private float _dashDuration = 1;
		[SerializeField] private float _dashMaxSpeed = 1;
		[SerializeField] private float _dashRecoveringDelay = 1;

		[Networked] public NetworkBool isDashing { get; set; }
		[Networked] public NetworkBool isDashingRecovering { get; set; }
		[Networked] public TickTimer dashCooldown { get; set; }

		private Vector2 _dashDirection = default;

		private bool DashInProgress()
        {
			return this.isDashing || this.isDashingRecovering;
        }

		public void StartDash()
        {
			if (this.isDashing) return;

			this.isDashing = true;

			this.dashCooldown = TickTimer.CreateFromSeconds(Runner, _dashDuration);

			Debug.Log("DASH: <color=green>start</color>");

			_dashDirection = _lastMoveDirection;

			var impulse = new Vector3(_dashDirection.x, 0, _dashDirection.y).normalized * _dashForce;

			_cc.maxSpeed = _dashMaxSpeed;
			_cc.gravity = 0;
			_cc.Velocity += impulse;	// / 10.0f; // Magic constant to compensate for not properly dealing with masses
			_cc.Move(Vector3.zero); // Velocity property is only used by CC when steering, so pretend we are, without actually steering anywhere

			_damageVisuals.SetDashing(true);
		}

		private void ApplyDash()
        {
			if (!this.isDashing)
			{
				_cc.Move(Vector3.zero);

				return;
			}

			_cc.Move(new Vector3(_dashDirection.x, 0, _dashDirection.y));
		}

		private void CheckDashFinalization()
        {
			if (!this.isDashing && !this.isDashingRecovering) return;

			if (!this.dashCooldown.ExpiredOrNotRunning(Runner)) return;

			if (this.isDashing)
			{
				this.isDashing = false;
				this.isDashingRecovering = true;
				this.dashCooldown = TickTimer.CreateFromSeconds(Runner, _dashRecoveringDelay);

				// Stop movement
				//_cc.Velocity = Vector3.zero;
				//_cc.Move(Vector3.zero);

				// Recover gravity
				_cc.gravity = _originalGravity;

				// Recover max speed
				_cc.maxSpeed = _originalMaxSpeed;

				return;
			}

			this.isDashingRecovering = false;

			_damageVisuals.SetDashing(false);

			Debug.Log("DASH: <color=orange>finish</color>");
		}

		#endregion

		#region Aiming

		[SerializeField] private AbilityTargetDetector targetDetector = default;

		private Vector3 aimingDirection = default;

		private void ProcessAiming()
        {
			this.aimingDirection = Vector3.zero;

			var playerPosition = transform.localPosition;
			
			if (targetDetector.TargetFound)
            {
				var targetPosition = targetDetector.Target.transform.position;
				targetPosition.y = playerPosition.y;

				this.aimingDirection = (targetPosition - playerPosition).normalized;

				Debug.DrawRay(playerPosition, this.aimingDirection * 10, Color.green);
			}

			Debug.DrawRay(playerPosition, _lastMoveDirection * 20, Color.yellow);
		}

		public bool IsTargetDetected()
        {
			return targetDetector.TargetFound;
        }

		public Vector3 GetTargetDirection()
        {
			var targetDetected = targetDetector.TryGetTargetDirection(out var dir);

			return (targetDetected) ? dir : _transform.forward;
        }

		#endregion

		#region Collecting

		[SerializeField] private LayerMask _collectableMask = default;

		private const int _maxCollectables = 100;

		[Networked(OnChanged = nameof(OnCollectablesChanged))]
		public int amountCollectables { get; set; }

		private void CheckForCollectablePickup()
		{
			if (this.amountCollectables >= _maxCollectables) return;

			// If we run into a collectable, pick it up
			if (isActivated && Runner.GetPhysicsScene().OverlapSphere(transform.position, _pickupRadius, _overlaps, _collectableMask, QueryTriggerInteraction.Collide) > 0)
			{
				PickupCollectable(_overlaps[0].GetComponent<BaseCollectable>());
			}
		}

		/// <summary>
		/// Called when a player collides with a collectable.
		/// </summary>
		public void PickupCollectable(BaseCollectable collectable)
		{
			if (!collectable) return;

			var amount = collectable.Amount;

			var canPickup = collectable.Pickup();

			if (!canPickup) return;

			this.amountCollectables += amount;

			_hud.UpdateCollectables(this.amountCollectables, _maxCollectables);
		}

		public static void OnCollectablesChanged(Changed<Player> changed)
		{
			if (!changed.Behaviour) return;

 		    changed.Behaviour.OnCollectablesChanged();
		}

		private void OnCollectablesChanged()
        {
			if (Object.HasStateAuthority) return;

			_hud.UpdateCollectables(this.amountCollectables, _maxCollectables);
		}

		private void RefreshCollectablesOnDeath()
        {
			this.amountCollectables = 0;

			_hud.UpdateCollectables(this.amountCollectables, _maxCollectables);
		}

		#endregion

		#region Delivery methods

		[SerializeField] private LayerMask _deliveryAreaMask = default;

		private void CheckForDelivery()
		{
			if (this.amountCollectables == 0) return;

			// If we run into a collectable, pick it up
			if (isActivated && Runner.GetPhysicsScene().OverlapSphere(transform.position, _pickupRadius, _overlaps, _deliveryAreaMask, QueryTriggerInteraction.Collide) > 0)
			{
				Deliver(_overlaps[0].GetComponent<BaseDeliveryArea>());
			}
		}

		/// <summary>
		/// Called when player collides with a delivery area.
		/// </summary>
		public void Deliver(BaseDeliveryArea deliveryArea)
		{
			if (!deliveryArea) return;

			var canDeliver = deliveryArea.Interact(this.team, this.amountCollectables);

			if (!canDeliver) return;

			Debug.LogError($"Deliver <color=yellow>{this.amountCollectables}</color>");

			this.amountCollectables = 0;

			_hud.UpdateCollectables(this.amountCollectables, _maxCollectables);
		}

		#endregion

		#region Remote methods

		[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
		public void RPC_SetInformation(string displayName, TeamEnum team, RpcInfo info = default)
		{
			this.displayName = displayName;
			this.team = team;

			_hud.SetDisplayName(this.displayName);
			_hud.SetTeam(this.team);
		}

		public static void OnDisplayNameChanged(Changed<Player> changed)
		{
			if (!changed.Behaviour) return;

			var displayName = changed.Behaviour.displayName;

			changed.Behaviour.OnDisplayNameChanged(displayName);
		}

		private void OnDisplayNameChanged(string displayName)
        {
			_hud.SetDisplayName(this.displayName);

			Debug.LogError($"REMOTE display name from id: <color=yellow>{this.Id}</color> is '<color=cyan>{this.displayName}</color>' assigned ({displayName})");
        }

		public static void OnTeamChanged(Changed<Player> changed)
		{
			if (!changed.Behaviour) return;

			var team = (TeamEnum)changed.Behaviour.team;

			changed.Behaviour.OnTeamChanged(team);
		}

		private void OnTeamChanged(TeamEnum team)
		{
			_hud.SetTeam(this.team);
		}

		#endregion
	}
}