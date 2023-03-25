
using UnityEngine;

namespace FusionExamples.Tanknarok.UI
{
    /// <summary>
    /// Helper used to visualize the detection cone from an entity
    /// </summary>
    public class VisualTargetDetectorHelper : MonoBehaviour
    {
        private bool _initialized = false;
        private float _angle = default;
        private float _radius = default;
        private Transform _transform = default;
        private Vector3 _forward = default;

        public void Init(float angle, float radius)
        {
            _transform = transform;

            Refresh(angle, radius);

            _initialized = true;
        }

        public void Refresh(float angle, float radius)
        {
            _angle = angle;
            _radius = radius;
        }

        public void Tick(Vector3 forward)
        {
            _forward = forward;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_initialized) return;

            Color c = new Color(0.8f, 0, 0, 0.4f);
            UnityEditor.Handles.color = c;

            var forward = (_forward == Vector3.zero) ? _transform.forward : _forward;

            Vector3 rotatedForward = Quaternion.Euler(0, -_angle * 0.5f, 0) * forward;

            UnityEditor.Handles.DrawSolidArc(_transform.position, Vector3.up, rotatedForward, _angle, _radius);
        }
#endif
    }
}