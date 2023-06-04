
using Fusion;
using UnityEngine;

namespace FusionExamples.Tanknarok.Gameplay
{
    public class EnemiesSpawnerService : NetworkBehaviour
    {
        #region Inspector

        [SerializeField] private BaseEnemy _enemyPrefab = default;
        [SerializeField] private LevelManager _levelManager = default;
        [SerializeField] private Transform _testPivot = default;
        [SerializeField] private int _minAmount = 1;
        [SerializeField] private int _maxAmount = 5;
        [SerializeField] private float _radius = 3;

        #endregion

        #region Private properties

        private int _id = 0;
        private const string ENEMY_ID = "dynamic_enemy_";

        #endregion

        #region Unity Events

        #endregion

        #region Public methods

        public void SpawnEnemies(Vector3 playerPosition)
        {
            var pivot = new Vector2(playerPosition.x, playerPosition.z);

            var amount = UnityEngine.Random.Range(_minAmount, _maxAmount+1);

            for (int i = 0; i < amount; i++)
            {
                var point = Utils.GetPositionAroundPoint(pivot, _radius);
                var spawnPosition = new Vector3(point.x, 0, point.y);

                SpawnEnemy(spawnPosition);
            }
        }

        #endregion

        #region Private methods

        private void SpawnEnemy(Vector3 position)
        {
            var spawnPosition = new Vector3(position.x, 0, position.z);

            var enemy = _levelManager.Runner.Spawn(_enemyPrefab, spawnPosition, Quaternion.identity);

            enemy.SetId($"{ENEMY_ID}{_id}");

            _id++;

            enemy.transform.SetParent(_levelManager.LevelBehavior.EnemiesContainer);
        }

        #endregion

        [ContextMenu("TEST")]
        public void Test()
        {
            SpawnEnemy(_testPivot.position);
        }
    }
}