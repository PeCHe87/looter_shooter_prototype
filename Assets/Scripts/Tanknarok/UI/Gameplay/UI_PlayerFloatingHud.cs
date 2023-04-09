
using UnityEngine;

namespace FusionExamples.Tanknarok
{
    public class UI_PlayerFloatingHud : MonoBehaviour
    {
        #region Private properties

        private Transform _transform = default;
        private Camera _camera = default;
        private Transform _target = default;
        private bool _hasTarget = false;

        #endregion

        #region Unity events

        private void Awake()
        {
            _transform = transform;
        }

        private void LateUpdate()
        {
            if (!_hasTarget) return;

            _transform.position = _camera.WorldToScreenPoint(_target.position);
        }

        #endregion

        #region Public methods

        public void Refresh(Player followPlayer)
        {
            _camera = Camera.main;

            _target = followPlayer.Body;

            _hasTarget = true;
        }

        #endregion
    }
}