
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FusionExamples.Tanknarok.Gameplay
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class BaseEnemy : MonoBehaviour
    {
        [SerializeField] private float _speedMovement = default;
        [SerializeField] private Transform _target = default;
        [SerializeField] private float _delay = 0.1f;
        [SerializeField] private float _goalOffset = 0.5f;
        [SerializeField] private bool _enabled = false;

        private NavMeshAgent _agent = default;
        private Transform _transform = default;

        private void Awake()
        {
            _transform = transform;

            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _speedMovement;
        }

        private void Start()
        {
            StartCoroutine(Chase());
        }

        private IEnumerator Chase()
        {
            var delay = new WaitForSeconds(_delay);

            while (_enabled)
            {
                var reachGoal = ReachGoal();

                if (reachGoal)
                {
                    _enabled = false;
                }
                else
                {
                    _agent.SetDestination(_target.position);

                    yield return delay;
                }
            }
        }

        [ContextMenu("Enable")]
        private void Enable()
        {
            _enabled = true;

            StartCoroutine(Chase());
        }

        private bool ReachGoal()
        {
            Vector3 directionToTarget = _target.position - _transform.position;

            float dSqrToTarget = directionToTarget.sqrMagnitude;

            var reachDestination = (dSqrToTarget <= _goalOffset);

            return reachDestination;
        }
    }
}