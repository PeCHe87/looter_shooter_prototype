using Fusion;
using FusionExamples.Tanknarok.UI;
using UnityEngine;

namespace FusionExamples.Tanknarok.Gameplay
{
    public class EnemyAttackAbility : NetworkBehaviour
    {
        [SerializeField] private EnemyWeapon _equippedWeapon;

        private Transform _transform = default;
        private float _attackRate = default;
        private float _attackDistance = default;
        private float _remainingTime = 0;
        private bool _isAttacking = false;
        private bool _initialized = false;
        private string _id = default;
        private VisualTargetAttackHelper _visualHelper = default;
        private NetworkRunner _runner = default;
        private PlayerRef _playerRef = default;

        public void Init(NetworkRunner runner, string id, float rate, float distance)
        {
            _id = id;

            _runner = runner;

            _equippedWeapon.InitRunner(runner);

            _playerRef = (Object != null) ? Object.InputAuthority : new PlayerRef();

            _attackRate = rate;
            _attackDistance = distance;

            _transform = transform;

            _visualHelper = GetComponent<VisualTargetAttackHelper>();
            _visualHelper.Init(90, _attackDistance);

            _initialized = true;
        }

        public void StartAttacking()
        {
            _isAttacking = true;
        }

        public void StopAttacking()
        {
            _isAttacking = false;
        }

        public void Tick(float deltaTime, Vector3 targetPosition)
        {
            if (!_initialized) return;

            _visualHelper.Tick(_transform.forward);

            if (_remainingTime > 0)
            {
                _remainingTime -= deltaTime;
            }

            if (!_isAttacking) return;
            
            LookAtTarget(targetPosition);

            if (_remainingTime > 0) return;

            ProcessAttack();
        }

        private void LookAtTarget(Vector3 targetPosition)
        {
            targetPosition.y = _transform.position.y;

            _transform.LookAt(targetPosition);
        }

        private void ProcessAttack()
        {
            _remainingTime = _attackRate;

            // Process attack
            _equippedWeapon.Fire(_playerRef, _transform.forward);
        }
    }
}