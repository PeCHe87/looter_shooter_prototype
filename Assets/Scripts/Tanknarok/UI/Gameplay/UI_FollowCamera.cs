
using UnityEngine;

namespace FusionExamples.Tanknarok.UI
{
    /// <summary>
    /// Logic to make an object follow the camera position and/or rotation
    /// Link: https://forum.unity.com/threads/make-ui-or-image-following-facing-smoothly-to-camera-like-the-unity-vr-splashscreen.835957/
    /// </summary>
    public class UI_FollowCamera : MonoBehaviour
    {
        #region Inspector properties

        #endregion

        #region Private properties

        private bool _initialized = false;
        private Transform _transform = default;
        private Transform _camera = default;
        private float _cameraDistance = 3.0f;
        private float _smoothTime = 0.3f;
        private Vector3 _velocity = Vector3.zero;

        #endregion

        #region Unity methods

        private void Start()
        {
            var levelManager = FindObjectOfType<LevelManager>();

            _camera = levelManager.Camera.transform;

            _transform = transform;

            _initialized = true;
        }

        private void LateUpdate()
        {
            if (!_initialized) return;

            FollowRotation();
        }

        #endregion

        #region Private methods

        private void FollowRotation()
        {
            _transform.LookAt(transform.position + _camera.rotation * Vector3.forward, _camera.rotation * Vector3.up);
        }

        #endregion
    }
}