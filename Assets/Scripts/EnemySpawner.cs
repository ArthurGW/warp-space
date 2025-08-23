using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    
    public float minTimeBetweenSpawns = 5f;
    public float maxTimeBetweenSpawns = 10f;
    
    private void Start()
    {
        StartCoroutine(nameof(SpawnLoop));
    }

    private IEnumerator SpawnLoop()
    {
        while (!destroyCancellationToken.IsCancellationRequested)
        {
            Instantiate(enemyPrefab, transform, false);
            yield return new WaitForSeconds(Random.Range(minTimeBetweenSpawns, maxTimeBetweenSpawns));
        }
    }

}
