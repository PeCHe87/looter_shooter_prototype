using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using FusionExamples.Tanknarok.CharacterAbilities;
using FusionExamples.Tanknarok.Items;
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
        [SerializeField] private MeshRenderer _bodyColorTeam;
        [SerializeField] private Transform _hull;
        [SerializeField] private Transform _turret;
        [SerializeField] private Transform _visualParent;
        [SerializeField] private Material[] _playerMaterials;
        [SerializeField] private TankTeleportInEffect _teleportIn;
        [SerializeField] private TankTeleportOutEffect _teleportOut;
        [SerializeField] private PlayerFloatingHud _hud = default;
        [SerializeField] private UI_PlayerInfoPanel _playerInfoPanel = default;
        [SerializeField] private PlayerChargerVisual _chargerVisual = default;

        [Space(10)]
        [SerializeField] private GameObject _deathExplosionPrefab;
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private float _pickupRadius;
        [SerializeField] private float _respawnTime;
        [SerializeField] private LayerMask _pickupMask;
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private TargeteableBase _targeteableBase = default;

        [Header("UI")]
        [SerializeField] private UI_PlayerWeaponInfo _weaponInformation = default;

        #endregion

        #region Networked properties

        [Networked(OnChanged = nameof(OnStateChanged))]
        public State state { get; set; }

        [Networked(OnChanged = nameof(OnLifeChanged))]
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
        [Networked] string lastKilledPlayer { get; set; }
        [Networked(OnChanged = nameof(OnKillsChanged))] public byte kills { get; set; }

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
        public bool IsLocal => _isLocal;
        public Transform Body => _body;

        #endregion

        #region Private properties

        private DriveDirection _driveDirection = DriveDirection.FORWARD;

        private bool _isLocal = false;
        private bool _localDashStarted = false;
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

        public LevelManager GetLevelManager()
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

            GetLevelManager();

            var isLocal = Object.HasInputAuthority;

            if (isLocal)
            {
                _isLocal = isLocal;
                var displayName = PlayerPrefs.GetString("playerDisplayName");
                var team = (TeamEnum)PlayerPrefs.GetInt("playerTeam");

                _weaponInformation = FindObjectOfType<UI_PlayerWeaponInfo>();

                weaponManager.ResetAllWeapons();

                _weaponInformation.Init();

                _dashInfo = FindObjectOfType<UI_PlayerDashInfo>();

                SetMapIndicator(this.team);
                SetLocal();

                InitializePlayerInfoPanel();

                RPC_SetInformation(displayName, team);

                //InitializeInventory();
            }
            else
            {
                SetMapIndicator(this.team);
            }

            InitLootboxInteraction();

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

            RefreshHealthBar();

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

                // Process dash when corresponding
                if (DashInProgress())
                {
                    ApplyDash();
                }
            }

            CheckForPowerupPickup();

            CheckForCollectablePickup();

            CheckForDelivery();

            CheckForLootboxInteraction();
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

            if (DashInProgress()) return;

            /*if (DashInProgress())
            {
                ApplyDash();
                return;
            }*/

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
        public void ApplyDamage(Vector3 impulse, byte damage, PlayerRef attacker, Player owner, GameObject hitVfx = null)
        {
            if (!isActivated || !invulnerabilityTimer.Expired(Runner))
                return;

            //Don't damage yourself
            Player attackingPlayer = PlayerManager.Get(attacker);
            if (attackingPlayer != null && attackingPlayer.playerID == playerID) return;

            ApplyImpulse(impulse);

            if (damage >= life)
            {
                life = 0;

                state = State.Dead;

                if (GameManager.playState == GameManager.PlayState.LEVEL)
                    lives -= 1;

                RefreshHealthBar();

                Respawn(_respawnTime);

                RefreshCollectablesOnDeath();

                CheckPlayerKiller(owner);

                var inventoryItems = GetInventoryItems();

                RPC_SpawnLootWhenDying(this.playerID, inventoryItems);

                RPC_EmptyInventoryOnDeath(this.playerID);

                GameManager.instance.OnTankDeath();
            }
            else
            {
                life -= damage;

                RefreshHealthBar();

                Debug.Log($"Player {playerID} took {damage} damage, life = {life}");
            }

            if (_isLocal)
            {
                _playerInfoPanel.UpdateHealth(this.life, MAX_HEALTH, "APPLY DAMAGE");
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
            if (_respawnInSeconds > 0)
                _respawnInSeconds -= Runner.DeltaTime;

            var spawnPoint = GetLevelManager().GetPlayerSpawnPoint(this.team);

            if (_respawnInSeconds <= 0)
            {
                // Make sure we don't get in here again, even if we hit exactly zero
                _respawnInSeconds = -1;

                // Restore health
                life = MAX_HEALTH;

                RefreshHealthBar();

                if (_isLocal)
                {
                    _playerInfoPanel.UpdateHealth(this.life, MAX_HEALTH, "CHECK RESPAWN");
                }

                // Start the respawn timer and trigger the teleport in effect
                respawnTimer = TickTimer.CreateFromSeconds(Runner, 1);
                invulnerabilityTimer = TickTimer.CreateFromSeconds(Runner, 1);

                // Place the tank at its spawn point. This has to be done in FUN() because the transform gets reset otherwise
                //Transform spawn = spawnpt.transform;
                //transform.position = spawn.position;
                //transform.rotation = spawn.rotation;

                transform.position = spawnPoint;

                // If the player was already here when we joined, it might already be active, in which case we don't want to trigger any spawn FX, so just leave it ACTIVE
                if (state != State.Active)
                    state = State.Spawning;

                Debug.Log($"Respawned player {playerID}, tick={Runner.Simulation.Tick}, timer={respawnTimer.IsRunning}:{respawnTimer.TargetTick}, life={life}, lives={lives}, hasAuthority={Object.HasStateAuthority} to state={state}");
            }
        }

        public static void OnStateChanged(Changed<Player> changed)
        {
            if (changed.Behaviour)
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
            weaponManager.ResetAllWeapons();
            state = State.Active;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            TeardownLootboxInteraction();

            TeardownInventory();

            Destroy(_deathExplosionInstance);
            
            PlayerManager.RemovePlayer(this);
        }

        public void DespawnTank()
        {
            if (state == State.Dead) return;

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
            {
                life = MAX_HEALTH;

                RefreshHealthBar();
            }
            else
                weaponManager.InstallWeapon(powerup);
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

        private void CheckPlayerKiller(Player projectileOwner)
        {
            if (projectileOwner == null) return;

            projectileOwner.IncrementKills(this);
        }

        public static void OnLifeChanged(Changed<Player> changed)
        {
            if (!changed.Behaviour) return;

            changed.Behaviour.RefreshHealthBar_Remote();
        }

        #endregion

        #region Dash

        [Space(10)]
        [Header("Dash configuration")]
        [SerializeField] private float _dashForce = 50;
        [SerializeField] private float _dashDuration = 1;
        [SerializeField] private float _dashMaxSpeed = 1;
        [SerializeField] private float _dashRecoveringDelay = 1;
        [SerializeField] private float _dashFireRate = 2;
        [SerializeField] private UI_PlayerDashInfo _dashInfo = default;

        [Networked(OnChanged = nameof(OnDashStateChanged))] public NetworkBool isDashing { get; set; }
        [Networked(OnChanged = nameof(OnDashRecoveringStateChanged))] public NetworkBool isDashingRecovering { get; set; }
        [Networked] public TickTimer dashCooldown { get; set; }
        [Networked] public TickTimer dashFireRateCooldown { get; set; }

        [Networked] public float _dashDirectionX {get; set;}

        [Networked] public float _dashDirectionY { get; set; }

        private Vector2 _dashDirection = default;

        private bool DashInProgress()
        {
            return this.isDashing || this.isDashingRecovering;
        }

        public void StartDash()
        {
            /*
            // Check dash fire rate
            if (!this.dashFireRateCooldown.ExpiredOrNotRunning(Runner)) return;

            if (this.isDashing) return;

            _dashDirectionX = _lastMoveDirection.x;
            _dashDirectionY = _lastMoveDirection.y;

            this.dashCooldown = TickTimer.CreateFromSeconds(Runner, _dashDuration);

            this.dashFireRateCooldown = TickTimer.CreateFromSeconds(Runner, _dashFireRate);

            _cc.maxSpeed = _dashMaxSpeed;
            _cc.acceleration = 200;
            _cc.gravity = 0;
            _cc.Velocity = Vector3.zero;
            _cc.Move(Vector3.zero);

            _damageVisuals.SetDashing(true);

            _localDashStarted = true;

            this.isDashing = true;

            if (!_isLocal) return;

            _dashInfo.StartReloading(_dashFireRate, this.dashFireRateCooldown, Runner);
            */

            // Check dash fire rate
            if (!this.dashFireRateCooldown.ExpiredOrNotRunning(Runner)) return;

            // Check if is already dashing
            if (this.isDashing) return;

            RPC_StartDash(this.playerID);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        private void RPC_StartDash(int playerId, RpcInfo info = default)
		{
            if (playerId != this.playerID) return;

            _dashDirectionX = _lastMoveDirection.x;
            _dashDirectionY = _lastMoveDirection.y;

            this.dashCooldown = TickTimer.CreateFromSeconds(Runner, _dashDuration);

            this.dashFireRateCooldown = TickTimer.CreateFromSeconds(Runner, _dashFireRate);

            _cc.maxSpeed = _dashMaxSpeed;
            _cc.acceleration = 200;
            _cc.gravity = 0;
            //_cc.Velocity = Vector3.zero;
            //_cc.Move(Vector3.zero);

            _damageVisuals.SetDashing(true);

            _localDashStarted = true;

            this.isDashing = true;

            if (!_isLocal) return;

            _dashInfo.StartReloading(_dashFireRate, this.dashFireRateCooldown, Runner);
        }

        private void ApplyDash()
        {
            //if (!Object.HasStateAuthority) return;

            if (!this.isDashing)
            {
                _cc.Move(Vector3.zero);

                return;
            }

            var direction = new Vector3(_dashDirectionX, 0, _dashDirectionY);

            //this.moveDirection = direction;

            _cc.Move(direction);
        }

        private void CheckDashFinalization()
        {
            if (!this.isDashing && !this.isDashingRecovering) return;

            if (!this.dashCooldown.ExpiredOrNotRunning(Runner)) return;

            if (this.isDashing)
            {
                this.dashCooldown = TickTimer.CreateFromSeconds(Runner, _dashRecoveringDelay);

                // Recover gravity
                _cc.gravity = _originalGravity;

                // Recover max speed
                _cc.maxSpeed = _originalMaxSpeed;
                _cc.acceleration = 100;


                this.isDashing = false;
                this.isDashingRecovering = true;

                return;
            }

            _localDashStarted = false;

            this.isDashingRecovering = false;
            
            _cc.Velocity = Vector3.zero;

            _damageVisuals.SetDashing(false);

            Debug.Log("DASH: <color=orange>finish</color>");
        }

        public static void OnDashStateChanged(Changed<Player> changed)
        {
            if (!changed.Behaviour) return;

            var isDashing = changed.Behaviour.isDashing;

            if (!isDashing) return;

            changed.Behaviour.StartDashing_Remote();
        }

        public static void OnDashRecoveringStateChanged(Changed<Player> changed)
        {
            if (!changed.Behaviour) return;

            var isDashRecovering = changed.Behaviour.isDashingRecovering;

            if (isDashRecovering)
            {
                changed.Behaviour.StartDashRecovering_Remote();

                return;
            }

            changed.Behaviour.StopDashRecovering_Remote();
        }

        private void StartDashing_Remote()
        {
            //if (_localDashStarted) return;

            /*if (Object.HasInputAuthority) return;

            _cc.maxSpeed = _dashMaxSpeed;
            _cc.gravity = 0;
            _cc.Move(Vector3.zero);

            */

            _damageVisuals.SetDashing(true);
        }

        private void StartDashRecovering_Remote()
        {
            //if (_localDashStarted) return;

            // Recover gravity
            _cc.gravity = _originalGravity;

            // Recover max speed
            _cc.maxSpeed = _originalMaxSpeed;
            _cc.acceleration = 100;
        }

        private void StopDashRecovering_Remote()
        {
            //if (_localDashStarted) return;

            _cc.Velocity = Vector3.zero;
            
            // Recover gravity
            _cc.gravity = _originalGravity;

            // Recover max speed
            _cc.maxSpeed = _originalMaxSpeed;
            _cc.acceleration = 100;

            _damageVisuals.SetDashing(false);
        }

        #endregion

        #region Aiming

        [SerializeField] private AbilityTargetDetector targetDetector = default;

        private Vector3 aimingDirection = default;

        private void ProcessAiming()
        {
            this.aimingDirection = Vector3.zero;

            var playerPosition = transform.localPosition;
            
            if (targetDetector.TargetFound && targetDetector.Target != null)
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

            if (collectable.IsRespawning) return;

            var amount = collectable.Amount;

            var canPickup = collectable.Pickup();

            if (!canPickup) return;

            this.amountCollectables += amount;

            if (_playerInfoPanel != null)
            {
                _playerInfoPanel.UpdateCollectables(this.amountCollectables, _maxCollectables, "PICKUP");
            }

            var progress = Mathf.Clamp((float)this.amountCollectables / (float)_maxCollectables, 0, 1);

            _chargerVisual.Refresh(progress);
        }

        public static void OnCollectablesChanged(Changed<Player> changed)
        {
            if (!changed.Behaviour) return;

            changed.Behaviour.RefreshCollectables_Remote();
        }

        private void RefreshCollectables_Remote()
        {
            if (Object.HasStateAuthority) return;

            if (_playerInfoPanel != null)
            {
                _playerInfoPanel.UpdateCollectables(this.amountCollectables, _maxCollectables, "PICKUP");
            }

            var progress = Mathf.Clamp((float)this.amountCollectables / (float)_maxCollectables, 0, 1);

            _chargerVisual.Refresh(progress);
        }

        private void RefreshCollectablesOnDeath()
        {
            this.amountCollectables = 0;

            if (_playerInfoPanel != null)
            {
                _playerInfoPanel.UpdateCollectables(this.amountCollectables, _maxCollectables, "DEATH");
            }

            _chargerVisual.Refresh(0);
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

            this.amountCollectables = 0;

            if (_playerInfoPanel != null)
            {
                _playerInfoPanel.UpdateCollectables(this.amountCollectables, _maxCollectables, "DELIVER");
            }

            _chargerVisual.Refresh(0);
        }

        #endregion

        #region Remote methods

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        public void RPC_SetInformation(string displayName, TeamEnum team, RpcInfo info = default)
        {
            this.displayName = displayName;
            this.team = team;

            _bodyColorTeam.material = _levelManager.GetPlayerTeamMaterial(this.team);

            _hud.SetDisplayName(this.displayName);
            //_hud.SetTeam(this.team);

            if (_isLocal)
            {
                _playerInfoPanel.SetDisplayName(this.displayName);
                _playerInfoPanel.UpdateHealth(this.life, MAX_HEALTH, "RPC SET INFORMATION");
            }
            
            InitializeInventory();

            targetDetector.SetTeam(this.team);

            if (_isLocal) return;

            SetMapIndicator(team);
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

            _bodyColorTeam.material = _levelManager.GetPlayerTeamMaterial(this.team);

            targetDetector.SetTeam(this.team);
        }

        #endregion

        #region Weapon actions

        public void RefreshWeaponInformation(int ammo, int magazine)
        {
            if (!_isLocal) return;

            _weaponInformation.Refresh(ammo, magazine);
        }

        public void StartReloadingWeapon(float reloadingTime, TickTimer cooldown)
        {
            if (!_isLocal) return;

            _weaponInformation.StartReloading(reloadingTime, cooldown, Runner);
        }

        public void StopReloadingWeapon(int ammo, int magazine)
        {
            if (!_isLocal) return;

            _weaponInformation.StopReloading(ammo, magazine);
        }

        public void UseWeapon()
        {
            var weaponType = weaponManager.GetWeaponType();

            // NONE
            if (weaponType == ItemWeaponType.NONE) return;

            // ASSAULT
            if (weaponType == ItemWeaponType.ASSAULT)
            {
                weaponManager.FireWeapon(WeaponManager.WeaponInstallationType.PRIMARY);

                if (_isLocal)
                {
                    _weaponInformation.StartUsing(weaponManager.EquippedWeapon.delay, weaponManager.primaryFireDelay, Runner);
                }

                return;
            }

            // MELEE
            if (weaponType == ItemWeaponType.MELEE)
            {
                weaponManager.MeleeAttack();

                if (_isLocal)
                {
                    _weaponInformation.StartUsing(weaponManager.EquippedWeapon.delay, weaponManager.primaryFireDelay, Runner);
                }
            }
        }

        #endregion

        #region Minimap indicator

        [SerializeField] private SpriteRenderer _minimapIndicator = default;
        [SerializeField] private Sprite _minimapLocalIndicator = default;
        [SerializeField] private Color _colorTeamBlue = default;
        [SerializeField] private Color _colorTeamRed = default;

        private void SetLocal()
        {
            _minimapIndicator.sprite = _minimapLocalIndicator;
            _minimapIndicator.color = Color.white;
        }

        private void SetMapIndicator(TeamEnum team)
        {
            _minimapIndicator.color = (team == TeamEnum.BLUE) ? _colorTeamBlue : _colorTeamRed;
        }

        #endregion

        #region UI methods

        public void InitFloatingHud(UI_PlayerFloatingHud hud)
        {
            if (!hud.TryGetComponent<PlayerFloatingHud>(out var floatingHud)) return;

            _hud = floatingHud;

            _hud.SetDisplayName(this.displayName);

            RefreshHealthBar();
        }

        private void InitializePlayerInfoPanel()
        {
            _playerInfoPanel = FindObjectOfType<UI_PlayerInfoPanel>();
            _playerInfoPanel.UpdateHealth(this.life, MAX_HEALTH, "INITIALIZATION");
            _playerInfoPanel.UpdateCollectables(0, _maxCollectables, "INITIALIZATION");
        }

        private void RefreshHealthBar()
        {
            _hud.RefreshHealth((float)life / MAX_HEALTH);
        }

        private void RefreshHealthBar_Remote()
        {
  
/*
            // If dead empty the inventory
            if (life <= 0)
            {
                EmptyInventoryOnDeath_Remote();
            }
*/

            if (Object.HasStateAuthority) return;

            _hud.RefreshHealth((float)life / MAX_HEALTH);
        }

        #endregion

        #region Lootboxes

        [SerializeField] private LayerMask _lootboxMask = default;
        [SerializeField] private float _lootboxInteractionRadius = default;

        [Networked] private LootboxBase _lootbox { get; set; }
        
        private UI_LootInGamePanel _lootInGamePanel = default;

        private void InitLootboxInteraction()
        {
            LootboxBase.OnOpen += OpenLootbox;

            if (!_isLocal) return;

            _lootInGamePanel = FindObjectOfType<UI_LootInGamePanel>();
            _lootInGamePanel?.Init(this, TakeItemFromLoot);
        }

        private void TeardownLootboxInteraction()
        {
            LootboxBase.OnOpen -= OpenLootbox;

            _lootInGamePanel?.Teardown();
        }

        private void OpenLootbox(LootData data, string playerId)
        {
            if (!_isLocal) return;

            var playerIdParsed = GetPlayerId();

            if (!playerIdParsed.Equals(playerId)) return;

            _lootInGamePanel?.Show(data);
        }

        private void CheckForLootboxInteraction()
        {
            // If we run into a lootbox, try to open it
            if (isActivated && Runner.GetPhysicsScene().OverlapSphere(transform.position, _lootboxInteractionRadius, _overlaps, _lootboxMask, QueryTriggerInteraction.Collide) > 0)
            {
                _lootbox = _overlaps[0].GetComponent<LootboxBase>();

                TryToOpenLootbox();

                return;
            }

            // Check if there is a lootbox to stop opening
            if (_lootbox == null) return;

            _lootbox.StopInteracting(GetPlayerId());

            _lootbox = null;

            _lootInGamePanel?.Close();
        }

        private void TryToOpenLootbox()
        {
            if (_lootbox == null) return;

            _lootbox.StartInteracting(GetPlayerId(), this.team);
        }

        public string GetPlayerId()
        {
            return this.playerID.ToString();
        }

        /// <summary>
        /// Takes an item from the lootbox:
        ///		- Remove item from loot
        ///		- Add item to the player's inventory
        /// </summary>
        /// <param name="id"></param>
        public void TakeItemFromLoot(int id, int amount)
        {
            if (_lootbox == null) return;

            RPC_TakeItemFromLoot(this.playerID, id, amount);

            _lootInGamePanel?.Remove(id);
        }

        /// <summary>
        /// Updates the loot's content for each proxy
        /// </summary>
        /// <param name="id"></param>
        /// <param name="info"></param>
        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        public void RPC_TakeItemFromLoot(int playerId, int id, int amount, RpcInfo info = default)
        {
            if (playerId != this.playerID) return;

            _lootbox.Take(id);

            var itemIsStackable = false;
            
            if (_levelManager.Catalog.TryGetItem(id, out var itemCatalog))
            {
                itemIsStackable = !itemCatalog.IsEquipable();
            }

            // Player's inventory should be updated
            AddItemToInventory(id, amount, itemIsStackable);
        }

        #endregion

        #region Inventory

        [SerializeField] private UI_InventoryPanel _inventoryPanel = default;

        [Networked] public ref PlayerInventoryData _inventoryData => ref MakeRef<PlayerInventoryData>();

        public void InitializeInventory()
        {
            _inventoryPanel = FindObjectOfType<UI_InventoryPanel>();

            // Mark as locked the last slots
            var lockedItem = new PlayerInventoryItemData()
            {
                id = -1,
                amount = 0,
                locked = true,
                isStackable = false
            };

            for (int i = 5; i < _inventoryData.items.Length; i++)
            {
                _inventoryData.items.Set(i, lockedItem);
            }

            if (!_isLocal) return;

            // Update UI
            _inventoryPanel.Init(_inventoryData, this);
        }

        public void TeardownInventory()
        {
            _inventoryPanel?.Teardown();
        }

        private void AddItemToInventory(int id, int amount, bool itemIsStackable)
        {
            var itemAlreadyExit = _inventoryData.AlreadyExist(id, out var slotIndex);

            if (!itemAlreadyExit)
            {
                // Get first free slot
                slotIndex = _inventoryData.GetFreeSlotIndex();
            }

            if (slotIndex == -1) return;

            // Add the amount of already existing item
            if (itemAlreadyExit)
            {
                var existingItem = _inventoryData.items.Get(slotIndex);
                amount += existingItem.amount;
            }

            var item = new PlayerInventoryItemData()
            {
                id = id,
                amount = amount,
                isStackable = itemIsStackable
            };

            _inventoryData.items.Set(slotIndex, item);

            // Only update visuals if it is the local player
            if (!_isLocal) return;

            _inventoryPanel?.Refresh(_inventoryData);
        }

        public void ConsumeInventorySlot(int slotIndex)
        {
            RPC_ConsumeItem(this.playerID, slotIndex);
        }

        public void EquipInventorySlot(int slotIndex)
        {
            RPC_EquipItem(this.playerID, slotIndex);
        }

        public bool IsInventoryFull()
        {
            return _inventoryData.IsFull();
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        public void RPC_ConsumeItem(int playerId, int slotIndex, RpcInfo info = default)
        {
            if (playerId != this.playerID) return;

            var item = _inventoryData.items.Get(slotIndex);

            item.amount--;

            // Check if it is empty
            if (item.amount == 0)
            {
                item.id = 0;
            }

            _inventoryData.items.Set(slotIndex, item);

            if (!_isLocal) return;

            Debug.LogError($"Player::<color=magenta>ConsumeItem</color> -> item: <color=yellow>{item.id}</color> from slot: <color=yellow>{slotIndex}</color>");

            _inventoryPanel.Refresh(_inventoryData);
        }

        /*
                [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
                public void RPC_SpawnLootWhenDying(int playerId, RpcInfo info = default)
                {
                    Debug.LogError($"Player::<color=magenta>SpawnDeathLoot</color> -> player <color=yellow>{this.displayName}</color> ({playerId} / {this.playerID})");

                    if (playerId != this.playerID) return;

                    var amount = _inventoryData.items.Length;

                    var items = new ItemLootData[amount];

                    for (int i = 0; i < amount; i++)
                    {
                        var playerItem = _inventoryData.items.Get(i);
                        var lootItem = new ItemLootData()
                        {
                            id = playerItem.id,
                            amount = playerItem.amount
                        };

                        items[i] = lootItem;
                    }

                    if (Object.HasStateAuthority)
                    {
                        // Create death body loot
                        _levelManager.PlayerDeathLoot.SpawnLoot(transform.position, items);

                        _levelManager.EnemiesSpawnerService.SpawnEnemies(transform.position);
                    }

                    // Empty inventory
                    _inventoryData.SetEmpty();

                    if (!_isLocal) return;

                    // Update inventory panel
                    _inventoryPanel.Refresh(_inventoryData);
                }
        */

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_SpawnLootWhenDying(int playerId, ItemLootData[] items, RpcInfo info = default)
        {
            if (items.Length == 0) return;

            if (playerId != this.playerID) return;

            Debug.LogError($"Player::<color=magenta>SpawnDeathLoot</color>");

            var levelManager = GetLevelManager();

            // Create death body loot
            levelManager.PlayerDeathLoot.SpawnLoot(transform.position, items);

            // Spawn enemies around the dead body
            levelManager.EnemiesSpawnerService.SpawnEnemies(transform.position);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        public void RPC_EquipItem(int playerId, int slotIndex, RpcInfo info = default)
        {
            if (playerId != this.playerID) return;

            var item = _inventoryData.items.Get(slotIndex);

            var itemId = item.id;

            var couldGetItem = GetLevelManager().Catalog.TryGetItem(itemId, out var itemCatalog);

            if (!couldGetItem) return;

            // Equip weapon
            weaponManager.Equip(itemId, (EquipableItemCatalogData)itemCatalog.data, out var previousWeaponId);

            // Move equipped weapon to the already used inventory slot
            if (previousWeaponId == -1)
            {
                item.amount = 0;
                item.id = 0;
            }
            else
            {
                item.amount = 1;
                item.id = previousWeaponId;
            }

            _inventoryData.items.Set(slotIndex, item);

            // Update area detection radius based on weapon configuration
            this.targetDetector.RefreshRadius(weaponManager.EquippedWeapon.RadiusDetection);

            if (!_isLocal) return;

            _inventoryPanel.Refresh(_inventoryData);

            // Refresh weapon information based on weapon type
            _weaponInformation.RefreshWeaponType(((EquipableItemCatalogData)itemCatalog.data).WeaponData.Type);

            // Refresh player information panel with new equipped weapon
            _playerInfoPanel.RefreshWeapon(itemCatalog.data.icon);
        }

        private ItemLootData[] GetInventoryItems()
        {
            var amount = _inventoryData.items.Length;

            var items = new List<ItemLootData>();

            for (int i = 0; i < amount; i++)
            {
                var playerItem = _inventoryData.items.Get(i);

                if (playerItem.IsEmpty()) continue;

                if (playerItem.locked) continue;

                var lootItem = new ItemLootData()
                {
                    id = playerItem.id,
                    amount = playerItem.amount
                };

                Debug.LogError($"item: <color=yellow>{lootItem.id}</color>, amount: <color=cyan>{lootItem.amount}</color>");

                items.Add(lootItem);
            }

            return items.ToArray();
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_EmptyInventoryOnDeath(int playerId, RpcInfo info = default)
        {
            if (playerId != this.playerID) return;

            Debug.LogError($"LOCAL - Empty Inventory at Death for player '<color=yellow>{displayName}</color>' (<color=cyan>{this.playerID}</color>), is local: {_isLocal}");

            // Empty inventory
            _inventoryData.SetEmpty();

            if (_inventoryPanel == null) return;

            // Update inventory panel only if local
            if (!_isLocal) return;
            
            _inventoryPanel.Refresh(_inventoryData);
        }

        #endregion

        #region Health

        public void Heal(int amount)
        {
            life = (byte)Mathf.Clamp(life + amount, 0, MAX_HEALTH);

            RefreshHealthBar();

            if (_isLocal)
            {
                _playerInfoPanel.UpdateHealth(this.life, MAX_HEALTH, "HEAL");
            }

            if (Runner.Stage == SimulationStages.Forward)
            {
                _damageVisuals.OnDamaged(life, isDead);
            }
        }

        #endregion

        #region Kills

        public void IncrementKills(Player killed)
        {
            this.lastKilledPlayer = killed.displayName;
            this.kills++;

            // TODO: check if it is the player with higher kills amount

            Debug.LogError($"Player {this.displayName} killed {killed.displayName}! New Kills amount <color=yellow>{this.kills}</color>");

            //_levelManager.RefreshPlayerKills(this.displayName, killed.displayName);
        }

        public static void OnKillsChanged(Changed<Player> changed)
        {
            if (!changed.Behaviour) return;

            var kills = changed.Behaviour.kills;

            changed.LoadOld();

            var oldKills = changed.Behaviour.kills;

            if (kills <= oldKills) return;

            changed.Behaviour.OnKillsChanged(changed.Behaviour.lastKilledPlayer);
        }

        private void OnKillsChanged(string killed)
        {
            //if (Object.HasStateAuthority) return;

            _levelManager.RefreshPlayerKills(this.displayName, killed, (this.team == TeamEnum.BLUE) ? _colorTeamBlue : _colorTeamRed);
        }

        #endregion
    }
}