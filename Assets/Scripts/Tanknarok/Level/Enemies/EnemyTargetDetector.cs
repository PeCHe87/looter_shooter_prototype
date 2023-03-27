
using FusionExamples.Tanknarok.UI;
using System;
using UnityEngine;

namespace FusionExamples.Tanknarok.Gameplay
{
    public class EnemyTargetDetector : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private float _radiusForward = default;
        [SerializeField] private float _radiusDistance = default;
        [SerializeField] private float _angle = default;
        [SerializeField] private float _detectionDelay = default;
        [SerializeField] private LayerMask _layer = default;
        [SerializeField] private LayerMask _obstacleLayer = default;
        [SerializeField] private VisualTargetDetectorHelper _visualHelper = default;

        #endregion

        #region Private properties

        private Transform _transform = default;
        private float _remainingTime = 0;
        private Collider[] _targets = default;
        private TargeteableBase _target = default;
        private bool _initialized = false;
        private string _targetId = NO_TARGET_ID;
        private const string NO_TARGET_ID = "none";

        #endregion

        #region Public properties

        public bool TargetFound => !_targetId.Equals(NO_TARGET_ID);
        public TargeteableBase Target => _target;

        #endregion

        #region Public mehtods

        public void Init(float radiusDetection) 
        {
            _radiusDistance = radiusDetection;

            _targets = new Collider[0];

            _transform = transform;

            InitVisualHelper();

            _initialized = true;
        }

        #endregion

        #region Unity events

        public void Tick(float deltaTime)
        {
            if (!_initialized) return;

            DrawVisualHelper();

            _remainingTime -= deltaTime;

            if (_remainingTime > 0) return;

            GetClosestTarget();

            _remainingTime = _detectionDelay;
        }

        #endregion

        #region Private methods

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

                // Check if there isn't any collider between target and detector
                var targetPosition = hit.transform.position;

                Vector3 directionToTarget = targetPosition - currentPosition;

                var somethingBlocking = Physics.Raycast(currentPosition, directionToTarget, _obstacleLayer);

                if (somethingBlocking) continue;

                // Skip itself
                if (hit.transform.parent == transform) continue;

                // Check if it is a player
                if (!hit.transform.parent.TryGetComponent<Player>(out var player)) continue;

                // Check if it is targeteable
                if (!hit.transform.parent.TryGetComponent<TargeteableBase>(out var targeteable)) continue;

                

                float dSqrToTarget = directionToTarget.sqrMagnitude;

                if (dSqrToTarget >= closestDistanceSqr) continue;

                closestDistanceSqr = dSqrToTarget;
                closestTarget = hit;

                found = true;
            }

            return found;
        }

        private bool CheckConeArea(out GameObject closestTarget)
        {
            var found = false;
            closestTarget = null;

            Vector3 currentPosition = _transform.position;
            currentPosition.y = 0;

            var cos = Mathf.Cos(_angle * 0.5f * Mathf.Deg2Rad);

            var forwardDirection = _transform.forward;

            _targets = Physics.OverlapSphere(currentPosition, _radiusForward, _layer);

            float closestDistanceSqr = Mathf.Infinity;

            foreach (Collider potentialTarget in _targets)
            {
                var hit = potentialTarget.gameObject;

                var targetPosition = hit.transform.position;
                targetPosition.y = 0;

                Vector3 directionToTarget = targetPosition - currentPosition;

                // Check obstacle in between
                var somethingBlocking = Physics.Raycast(currentPosition, directionToTarget, _obstacleLayer);

                // Skip itself
                if (hit.transform.parent == transform) continue;

                // Check if it is a player
                if (!hit.transform.parent.TryGetComponent<Player>(out var player)) continue;

                // Skip if it is not targeteable
                if (!hit.transform.parent.TryGetComponent<TargeteableBase>(out var targeteable)) continue;

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

        private void StopDetection()
        {
            if (_targetId.Equals(NO_TARGET_ID)) return;

            _target = null;
            _targetId = NO_TARGET_ID;
        }

        private void StartDetection(GameObject target)
        {
            var targetFound = target.GetComponentInParent<TargeteableBase>();

            if (targetFound.Id.Equals(_targetId)) return;

            _target = targetFound;

            _targetId = _target.Id;
        }

        #endregion

        #region Visual helper

        private void InitVisualHelper()
        {
            _visualHelper.Init(_angle, _radiusForward);
        }

        private void DrawVisualHelper()
        {
            _visualHelper.Tick(_transform.forward);
        }

        #endregion
    }
}