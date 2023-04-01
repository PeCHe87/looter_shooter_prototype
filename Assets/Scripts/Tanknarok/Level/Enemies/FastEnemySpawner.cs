
using FusionExamples.Tanknarok.Gameplay;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
    public class FastEnemySpawner : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Transform _container = default;
        [SerializeField] private float _minDelay = default;
        [SerializeField] private float _maxDelay = default;
        [SerializeField] private BaseEnemy _prefab = default;
        [SerializeField] private Transform[] _spawnPoints = default;
        [SerializeField] private float _spawnRadius = default;

        #endregion

        private float _remainingTime = 0;
        private bool _enabled = false;
        private int _id = 100;
        private bool _isSpawning = false;

        private void Start()
        {
            StartRemainingTime();

            _enabled = true;
        }

        private void Update()
        {
            if (!_enabled) return;

            if (_isSpawning) return;

            _remainingTime -= Time.deltaTime;

            if (_remainingTime > 0) return;

            Spawn();
        }

        #region Private methods

        private void Spawn()
        {
            _isSpawning = true;

            var index = UnityEngine.Random.Range(0, _spawnPoints.Length);
            var spawnPoint = _spawnPoints[index];
            var position = Utils.GetPositionAroundPoint(spawnPoint.position, _spawnRadius);

            var spawnPosition = new Vector3(position.x, 0, position.y);

            var enemy = Instantiate(_prefab, spawnPosition, Quaternion.identity, _container);
            enemy.SetId($"fast_{_id}");

            _id++;

            StartRemainingTime();

            _isSpawning = false;
        }

        private void StartRemainingTime()
        {
            var delay = UnityEngine.Random.Range(_minDelay, _maxDelay);

            _remainingTime = delay;
        }

        #endregion
    }
}