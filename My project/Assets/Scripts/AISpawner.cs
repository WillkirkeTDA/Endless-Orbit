using UnityEngine;

public class AISpawner : MonoBehaviour
{
    [Header("AI")]
    [SerializeField] private GameObject aiPrefab;

    [Header("Spawning")]
    [SerializeField] private float spawnDelay = 2f;

    [Header("Arena")]
    [SerializeField] private float spawnDistanceFromCenter = 5f;

    private float spawnTimer;

    private void Start()
    {
        spawnTimer = 0f;
    }

    private void Update()
    {
        // -------------------------------------------------
        // CHECK IF AN AI ALREADY EXISTS
        // -------------------------------------------------

        AIRider existingAI =
            FindAnyObjectByType<AIRider>();

        if (existingAI != null)
        {
            spawnTimer = spawnDelay;
            return;
        }

        // -------------------------------------------------
        // WAIT BEFORE SPAWNING
        // -------------------------------------------------

        spawnTimer -= Time.deltaTime;

        if (spawnTimer > 0f)
            return;

        SpawnAI();

        spawnTimer = spawnDelay;
    }

    private void SpawnAI()
    {
        if (aiPrefab == null)
        {
            Debug.LogError(
                "AI SPAWNER: AI Prefab is missing!",
                this
            );

            return;
        }

        Vector2 spawnPosition =
            GetSpawnPosition();

        GameObject newAI =
            Instantiate(
                aiPrefab,
                spawnPosition,
                Quaternion.identity
            );

        Debug.Log(
            "AI SPAWNED: " +
            newAI.name
        );
    }

    private Vector2 GetSpawnPosition()
    {
        Vector2 center;

        if (Arena.Instance != null)
        {
            center =
                Arena.Instance.GetCenter();
        }
        else
        {
            center =
                transform.position;
        }

        Vector2 direction =
            Random.insideUnitCircle.normalized;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector2.right;
        }

        return center +
               direction *
               spawnDistanceFromCenter;
    }
}