
using UnityEngine;

namespace FusionExamples.Tanknarok.CharacterAbilities
{
    public class AbilityTargetDetector : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private float _radius = default;
        [SerializeField] private float _detectionDelay = default;
        [SerializeField] private LayerMask _layer = default;

        #endregion

        #region Private properties

        private Transform _transform = default;
        private float _remainingTime = 0;
        private Collider[] _targets = default;
        private TargeteableBase _target = default;
        private int _targetId = -1;
        private Vector3 _moveDirection = default;

        #endregion

        #region Public properties

        public bool TargetFound => _targetId != -1;
        public TargeteableBase Target => _target;

        #endregion

        #region Unity events

        private void Start()
        {
            _targets = new Collider[0];

            _transform = transform;
        }

        private void Update()
        {
            _remainingTime -= Time.deltaTime;

            if (_remainingTime > 0) return;

            GetClosestTarget();

            _remainingTime = _detectionDelay;
        }

        #endregion

        #region Public methods

        public void UpdateMovementDirection(Vector3 direction)
        {
            _moveDirection = direction;
        }

        #endregion

        #region Private methods

        private void GetClosestTarget()
        {
            Vector3 currentPosition = _transform.position;
            
            _targets = Physics.OverlapSphere(currentPosition, _radius, _layer);

            GameObject distanceClosestTarget = null;
            GameObject angleClosestTarget = null;

            var foundDistanceClosest = false;
            var foundAngleClosest = false;

            float closestDistanceSqr = Mathf.Infinity;

            foreach (Collider potentialTarget in _targets)
            {
                var hit = potentialTarget.gameObject;

                // Skip itself
                if (hit.transform.parent == transform) continue;

                if (!hit.transform.parent.TryGetComponent<TargeteableBase>(out var targeteable)) continue;

                var targetPosition = hit.transform.position;

                Vector3 directionToTarget = targetPosition - currentPosition;

                Debug.DrawRay(currentPosition, directionToTarget * 8, Color.red);

                float dSqrToTarget = directionToTarget.sqrMagnitude;

                /*
                var angleWithTarget = Vector3.Angle(_moveDirection.normalized, targetPosition);

                if (angleWithTarget < minAngle)
                {
                    angleClosestTarget = hit;
                    minAngle = angleWithTarget;

                    foundAngleClosest = true;
                }

                Debug.Log($" <color=yellow>{hit.transform.parent.name}</color> angle: <color=magenta>{angleWithTarget}</color>");
                */

                if (dSqrToTarget >= closestDistanceSqr) continue;

                closestDistanceSqr = dSqrToTarget;
                distanceClosestTarget = hit;

                foundDistanceClosest = true;
            }

            if (!foundDistanceClosest && !foundAngleClosest)
            {
                StopDetection();

                return;
            }

            // Priority is in the same direction it is moving
            var target = (foundAngleClosest) ? angleClosestTarget : distanceClosestTarget;

            StartDetection(target);
        }

        private void StopDetection()
        {
            if (_targetId == -1) return;
            
            _target.HideIndicator();

            _target = null;
            _targetId = -1;
        }

        private void StartDetection(GameObject target)
        {
            var targetFound = target.GetComponentInParent<TargeteableBase>();

            if (targetFound.Id == _targetId) return;

            if (_targetId != -1)
            {
                _target.HideIndicator();
            }

            _target = targetFound;

            _target.ShowIndicator();

            _targetId = _target.Id;
        }

        #endregion
    }
}