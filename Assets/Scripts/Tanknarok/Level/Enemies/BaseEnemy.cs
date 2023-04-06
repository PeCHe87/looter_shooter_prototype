
using Fusion;
using UnityEngine;
using UnityEngine.AI;

namespace FusionExamples.Tanknarok.Gameplay
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class BaseEnemy : NetworkBehaviour, ICanTakeDamage
    {
        #region Inspector

        [SerializeField] private LevelManager _levelManager = default;
        [SerializeField] private string _id = default;
        [SerializeField] private EnemyStatus _status = EnemyStatus.NONE;
        [SerializeField] private float _speedMovement = default;
        [SerializeField] private Transform _target = default;
        [SerializeField] private float _delay = 0.1f;
        [SerializeField] private float _goalOffset = 0.5f;
        [SerializeField] private bool _enabled = false;
        [SerializeField] private float _radiusDetection = default;
        [SerializeField] private float _attackDistance = 2f;
        [SerializeField] private float _attackRate = 1;
        [SerializeField] private GameObject _art = default;
        [SerializeField] private int _minHp = default;
        [SerializeField] private int _maxHp = default;

        #endregion

        #region Private properties

        private NavMeshAgent _agent = default;
        private Transform _transform = default;
        private bool _initialized = false;
        private EnemyTargetDetector _detectionAbility = default;
        private EnemyAttackAbility _attackAbility = default;
        private TargeteableBase _targeteable = default;
        private float _attackDistanceSqr = default;

        #endregion

        #region Unity events

        private void Update()
        {
            if (!_initialized) return;

            // Process detection
            _detectionAbility.Tick(Time.deltaTime);

            // Process attack
            var targetPosition = (_detectionAbility.TargetFound) ? _detectionAbility.Target.transform.position : _transform.forward;
            _attackAbility.Tick(Time.deltaTime, targetPosition);

            CheckChaseStatus();

            CheckAttackStatus();
        }

        #endregion

        #region Network methods

        public override void Spawned()
        {
            _transform = transform;

            _targeteable = GetComponent<TargeteableBase>();
            _targeteable.SetId(_id, false);

            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _speedMovement;

            if (TryGetComponent<Destructible>(out var destructible))
            {
                destructible.OnDestroyed += Death;
            }

            _levelManager = FindObjectOfType<LevelManager>();

            _attackDistanceSqr = Mathf.Pow(_attackDistance, 2);

            _detectionAbility = GetComponent<EnemyTargetDetector>();
            _detectionAbility.Init(_radiusDetection);

            _attackAbility = GetComponent<EnemyAttackAbility>();
            _attackAbility.Init(_levelManager.Runner, _id, _attackRate, _attackDistance);

            _status = EnemyStatus.IDLE;

            if (Object.HasStateAuthority)
            {
                _netHealth = (byte)UnityEngine.Random.Range(_minHp, _maxHp + 1);
            }

            _initialized = true;
        }

        #endregion

        #region Chase methods

        private void CheckChaseStatus()
        {
            if (_detectionAbility.TargetFound)
            {
                _target = _detectionAbility.Target.transform;

                StartChasing(_target);

                // Check reaching goal while chasing
                if (_status == EnemyStatus.CHASE)
                {
                    var reachGoal = ReachGoal();

                    if (reachGoal)
                    {
                        StopChasing();
                    }
                }
            }
            else
            {
                StopChasing();
            }
        }

        private bool ReachGoal()
        {
            Vector3 directionToTarget = _target.position - _transform.position;

            float dSqrToTarget = directionToTarget.sqrMagnitude;

            var reachDestination = (dSqrToTarget <= _goalOffset);

            return reachDestination;
        }

        private void StartChasing(Transform target)
        {
            _target = target;

            _agent.isStopped = false;
            _agent.SetDestination(_target.position);

            _status = EnemyStatus.CHASE;

        }

        private void StopChasing()
        {
            _agent.isStopped = true;

            _status = EnemyStatus.IDLE;
        }

        #endregion

        #region Attack methods

        private void CheckAttackStatus()
        {
            // Check if it is in the attack area
            var onAttackArea = CheckAttackDistance();

            if (onAttackArea && _status != EnemyStatus.ATTACK)
            {
                StartAttacking();

                return;
            }

            if (!onAttackArea)
            {
                StopAttacking();
                return;
            }
        }

        private bool CheckAttackDistance()
        {
            if (!_detectionAbility.TargetFound) return false;

            Vector3 directionToTarget = _detectionAbility.Target.transform.position - _transform.position;

            float dSqrToTarget = directionToTarget.sqrMagnitude;

            var reachDestination = (dSqrToTarget <= _attackDistanceSqr);

            return reachDestination;
        }

        private void StartAttacking()
        {
            _agent.isStopped = true;

            _attackAbility.StartAttacking();

            _status = EnemyStatus.ATTACK;
        }

        private void StopAttacking()
        {
            _attackAbility.StopAttacking();

            _status = EnemyStatus.IDLE;
        }

        #endregion

        #region Health methods

        [Networked] public byte _netHealth { get; set; }

        public void ApplyDamage(Vector3 impulse, byte damage, PlayerRef source)
        {
            if (_status == EnemyStatus.DEAD) return;

            _netHealth = (byte)Mathf.Clamp(_netHealth - damage, 0, _maxHp);

            //Debug.LogError($"BaseEnemy::ApplyDamage -> hp: <color=yellow>{_netHealth}</color>, damage: <color=cyan>{damage}</color>, is dead: <color=yellow>{_netHealth == 0}</color>");

            if (_netHealth == 0)
            {
                Death();
            }
        }

        private void Death()
        {
            _initialized = false;

            StopAttacking();

            StopChasing();

            _status = EnemyStatus.DEAD;

            _art.Toggle(false);

            Destroy(gameObject, 1);
        }

        #endregion

        #region Public methods

        public void SetId(string id)
        {
            _id = id;
        }

        #endregion
    }
}