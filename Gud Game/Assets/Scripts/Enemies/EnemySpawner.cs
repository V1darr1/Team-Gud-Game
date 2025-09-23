using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] GameObject objectToSpawn;
    [SerializeField] int numToSpawn;
    [SerializeField] float spawnRate;
    [SerializeField] Transform[] spawnPos;

    float spawnTime;
    int spawnCount;
    bool startSpawning;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager.instance.updateGameGoal(numToSpawn);
    }

    // Update is called once per frame
    void Update()
    {
        if(startSpawning)
        {
            spawnTime += Time.deltaTime;

            if(spawnCount < numToSpawn && spawnTime >= spawnRate)
            {
                spawn();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            startSpawning = true;
        }
    }

    void spawn()
    {
        int arrayPos = Random.Range(0, spawnPos.Length);
        Instantiate(objectToSpawn, spawnPos[arrayPos].position, spawnPos[arrayPos].rotation);
        spawnCount++;
        spawnTime = 0;
    }
}
