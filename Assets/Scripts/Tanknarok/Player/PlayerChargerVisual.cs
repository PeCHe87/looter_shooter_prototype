
using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    public class PlayerChargerVisual : MonoBehaviour
    {
        #region Inspector 

        [SerializeField] private Renderer _renderer = default;
        [SerializeField] private Material _material0 = default;
        [SerializeField] private Material _material25 = default;
        [SerializeField] private Material _material50 = default;
        [SerializeField] private Material _material75 = default;
        [SerializeField] private Material _materialFull = default;

        #endregion

        #region Public methods

        public void Refresh(float progress)
        {
            if (progress == 0)
            {
                _renderer.material = _material0;
                return;
            }

            if (progress <= 0.5f)
            {
                _renderer.material = _material25;
                return;
            }

            if (progress <= 0.75f)
            {
                _renderer.material = _material50;
                return;
            }

            if (progress < 1)
            {
                _renderer.material = _material75;
                return;
            }

            _renderer.material = _materialFull;
        }

        #endregion
    }
}