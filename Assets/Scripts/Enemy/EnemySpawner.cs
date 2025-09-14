using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
    
        public float minTimeBetweenSpawns = 5f;
        public float maxTimeBetweenSpawns = 10f;

        public float enemySpeed;
        public float enemyAccel;
        public float enemyAngularSpeed;
    
        private void Start()
        {
            StartCoroutine(nameof(SpawnLoop));
        }

        private IEnumerator SpawnLoop()
        {
            while (!destroyCancellationToken.IsCancellationRequested)
            {
                var enemy = Instantiate(enemyPrefab, transform, false);
                var agent = enemy.GetComponent<NavMeshAgent>();
                agent.speed = enemySpeed;
                agent.acceleration = enemyAccel;
                agent.angularSpeed = enemyAngularSpeed;
                yield return new WaitForSeconds(Random.Range(minTimeBetweenSpawns, maxTimeBetweenSpawns));
            }
        }

        public void SetEnemyCharacteristics(float speed, float accel, float angularSpeed, float minSpawnTime,
            float maxSpawnTime)
        {
            minTimeBetweenSpawns = minSpawnTime;
            maxTimeBetweenSpawns = maxSpawnTime;
            enemySpeed = speed;
            enemyAccel = accel;
            enemyAngularSpeed = angularSpeed;
        }
    }
}
