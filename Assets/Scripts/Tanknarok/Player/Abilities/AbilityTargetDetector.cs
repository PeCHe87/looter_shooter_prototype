
using FusionExamples.Tanknarok.UI;
using UnityEngine;
using static FusionExamples.Tanknarok.GameLauncher;

namespace FusionExamples.Tanknarok.CharacterAbilities
{
    public class AbilityTargetDetector : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private float _radiusForward = default;
        [SerializeField] private float _radiusDistance = default;
        [SerializeField] private float _angle = default;
        [SerializeField] private float _detectionDelay = default;
        [SerializeField] private LayerMask _layer = default;
        [SerializeField] private VisualTargetDetectorHelper _visualHelper = default;
        [SerializeField] private bool _canDebug = false;

        #endregion

        #region Private properties

        private Transform _transform = default;
        private float _remainingTime = 0;
        private Collider[] _targets = default;
        private TargeteableBase _target = default;
        private Vector3 _moveDirection = default;
        private bool _isLocal = false;
        private TeamEnum _playerTeam = TeamEnum.NONE;

        #endregion

        #region Public properties

        public bool TargetFound => _target != null;  //!_targetId.Equals(NO_TARGET_ID);
        public TargeteableBase Target => _target;

        #endregion

        #region Unity events

        private void Start()
        {
            _targets = new Collider[0];

            _transform = transform;

            if (_canDebug)
            {
                InitVisualHelper();
            }
        }

        private void Update()
        {
            DrawVisualHelper();

            _remainingTime -= Time.deltaTime;

            if (_remainingTime > 0) return;

            GetClosestTarget();

            _remainingTime = _detectionDelay;
        }

        #endregion

        #region Public methods

        public void Init(bool isLocal)
        {
            _isLocal = isLocal;
        }

        public void SetTeam(TeamEnum team)
        {
            _playerTeam = team;
        }

        public void UpdateMovementDirection(Vector3 direction)
        {
            _moveDirection = direction.normalized;
        }

        public bool TryGetTargetDirection(out Vector3 direction)
        {
            direction = Vector3.zero;

            if (!TargetFound) return false;

            direction = _target.transform.position - _transform.position;

            return true;
        }

        public void RefreshRadius(float radius)
        {
            _radiusForward = radius;
        }

        #endregion

        #region Private methods

        [ContextMenu("Refresh visual helper")]
        private void RefreshVisualHelper()
        {
            _visualHelper.Refresh(_angle, _radiusForward);
        }

        [ContextMenu("Init visual helper")]
        private void InitVisualHelper()
        {
            _visualHelper.Init(_angle, _radiusForward);
        }

        private void DrawVisualHelper()
        {
            if (!_canDebug) return;

            _visualHelper.Tick(_moveDirection);
        }

        private void GetClosestTarget()
        {
            GameObject closestTarget = null;

            // Check closest by inner circle
            var found = CheckCircleArea(out closestTarget, _radiusDistance);

            // Check closest by moving direction 
            if (!found)
            {
                found = CheckConeArea(out closestTarget);
            }

            // Check closest by outter circle
            if (!found)
            {
                found = CheckCircleArea(out closestTarget, _radiusForward);
            }

            // If anything is found stop detection
            if (!found)
            {
                StopDetection();

                return;
            }

            // If something was found, start detection
            StartDetection(closestTarget);
        }

        private bool CheckConeArea(out GameObject closestTarget)
        {
            var found = false;
            closestTarget = null;

            Vector3 currentPosition = _transform.position;
            currentPosition.y = 0;

            var cos = Mathf.Cos(_angle * 0.5f * Mathf.Deg2Rad);

            var forwardDirection = (_moveDirection == Vector3.zero) ? _transform.forward : _moveDirection;

            _targets = Physics.OverlapSphere(currentPosition, _radiusForward, _layer);

            float closestDistanceSqr = Mathf.Infinity;

            foreach (Collider potentialTarget in _targets)
            {
                var hit = potentialTarget.gameObject;

                // Skip itself
                if (hit.transform.parent == transform) continue;

                if (!hit.transform.parent.TryGetComponent<TargeteableBase>(out var targeteable)) continue;

                // Check player's team, skip same team
                if (hit.transform.parent.TryGetComponent<Player>(out var player))
                {
                    if (player.team == _playerTeam) continue;
                }

                var targetPosition = hit.transform.position;
                targetPosition.y = 0;

                Vector3 directionToTarget = targetPosition - currentPosition;

                if (directionToTarget.magnitude > _radiusForward) continue;

                var dot = Vector3.Dot(directionToTarget.normalized, forwardDirection);

                if (dot <= cos) continue;

                float dSqrToTarget = directionToTarget.sqrMagnitude;

                if (dSqrToTarget >= closestDistanceSqr) continue;

                closestDistanceSqr = dSqrToTarget;
                closestTarget = hit;

                found = true;
            }

            return found;
        }

        private bool CheckCircleArea(out GameObject closestTarget, float radius)
        {
            closestTarget = null;
            Vector3 currentPosition = _transform.position;

            _targets = Physics.OverlapSphere(currentPosition, radius, _layer);

            var found = false;

            float closestDistanceSqr = Mathf.Infinity;

            foreach (Collider potentialTarget in _targets)
            {
                var hit = potentialTarget.gameObject;

                // Skip itself
                if (hit.transform.parent == transform) continue;

                if (!hit.transform.parent.TryGetComponent<TargeteableBase>(out var targeteable)) continue;

                // Check player's team, skip same team
                if (hit.transform.parent.TryGetComponent<Player>(out var player))
                {
                    if (player.team == _playerTeam) continue;
                }

                var targetPosition = hit.transform.position;

                Vector3 directionToTarget = targetPosition - currentPosition;

                //Debug.DrawRay(currentPosition, directionToTarget * 8, Color.red);

                float dSqrToTarget = directionToTarget.sqrMagnitude;

                if (dSqrToTarget >= closestDistanceSqr) continue;

                closestDistanceSqr = dSqrToTarget;
                closestTarget = hit;

                found = true;
            }

            return found;
        }

        private void StopDetection()
        {
            if (!TargetFound) return;

            if (_isLocal && TargetFound)
            {
                _target.HideIndicator();
            }

            _target = null;
        }

        private void StartDetection(GameObject target)
        {
            var targetFound = target.GetComponentInParent<TargeteableBase>();

            if (TargetFound && _target == target) return;

            if (TargetFound)
            {
                if (_isLocal)
                {
                    _target.HideIndicator();
                }
            }

            _target = targetFound;

            if (_isLocal)
            {
                _target.ShowIndicator();
            }
        }

        #endregion
    }
}