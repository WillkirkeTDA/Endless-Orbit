using UnityEngine;

public class AISpawner : MonoBehaviour
{
    // The AI prefab is the GameObject that will be copied
    // whenever the spawner creates a new AI.
    [Header("AI")]
    [SerializeField] private GameObject aiPrefab;

    // How many seconds the spawner waits before creating
    // the first wave or the next wave.
    [Header("Spawning")]
    [SerializeField] private float spawnDelay = 2f;

    // How far from the center of the arena the AI should
    // be placed when it is spawned.
    [Header("Arena")]
    [SerializeField] private float spawnDistanceFromCenter = 5f;

    // These numbers control how many AI are created in
    // each wave.
    // Example: the first wave creates 1 AI.
    [Header("Wave Progression")]
    [SerializeField] private int firstWaveSize = 1;
    [SerializeField] private int secondWaveSize = 2;
    [SerializeField] private int thirdWaveSize = 3;
    [SerializeField] private int finalWaveSize = 4;

    // These numbers determine how many total AI defeats
    // are needed before the wave size increases.
    // 3 defeats = 2 AI.
    // 9 defeats = 3 AI.
    // 18 defeats = 4 AI forever.
    [Header("Wave Thresholds")]
    [SerializeField] private int secondWaveAt = 3;
    [SerializeField] private int thirdWaveAt = 9;
    [SerializeField] private int finalWaveAt = 18;

    // Counts down the time remaining before the next wave
    // is allowed to spawn.
    private float spawnTimer;

    // Keeps track of whether the player has started the game.
    // The spawner will not create anything before this is true.
    private bool gameStarted;

    // Stores the total number of AI that have been defeated
    // during the current game.
    private int totalAIDefeated;

    // Start() is a Unity method that runs once when this
    // GameObject becomes active.
    private void Start()
    {
        spawnTimer = 0f;
        gameStarted = false;
        totalAIDefeated = 0;
    }

    // Update() runs once every frame.
    // It controls when the spawner is allowed to create
    // another wave.
    private void Update()
    {
        // Wait until the player presses any key.
        if (!gameStarted)
        {
            if (Input.anyKeyDown)
            {
                gameStarted = true;
                spawnTimer = spawnDelay;

                Debug.Log("AI SPAWNER: Game started! First AI will spawn in " + spawnDelay + " seconds.");
            }

            return;
        }

        // Find every AI currently active in the scene.
        AIRider[] existingAIs = FindObjectsByType<AIRider>();

        // If at least one AI is still alive, do not spawn
        // another wave.
        if (existingAIs.Length > 0)
        {
            spawnTimer = spawnDelay;
            return;
        }

        // Count down the time before the next wave.
        spawnTimer -= Time.deltaTime;

        // The timer has not finished yet, so wait.
        if (spawnTimer > 0f)
            return;

        // The timer has finished and there are no AI alive,
        // so create the next wave.
        SpawnWave();

        // Reset the timer so there is a delay before another
        // wave can be created after this one is defeated.
        spawnTimer = spawnDelay;
    }

    // Creates all of the AI needed for the current wave.
    private void SpawnWave()
    {
        // Make sure an AI prefab was assigned in the Inspector.
        if (aiPrefab == null)
        {
            Debug.LogError("AI SPAWNER: AI Prefab is missing!", this);
            return;
        }

        // Find out how many AI should be created based on
        // the number of AI defeated so far.
        int waveSize = GetCurrentWaveSize();

        Debug.Log("AI SPAWNER: Spawning wave of " + waveSize + " AI(s). Total defeated: " + totalAIDefeated);

        // Repeat once for every AI that needs to be created.
        for (int i = 0; i < waveSize; i++)
        {
            SpawnAI(i);
        }
    }

    // Decides how large the next wave should be.
    private int GetCurrentWaveSize()
    {
        // Once 18 or more AI have been defeated, always
        // spawn 4 AI from this point onward.
        if (totalAIDefeated >= finalWaveAt)
            return finalWaveSize;

        // Once 9 or more AI have been defeated, spawn 3 AI.
        if (totalAIDefeated >= thirdWaveAt)
            return thirdWaveSize;

        // Once 3 or more AI have been defeated, spawn 2 AI.
        if (totalAIDefeated >= secondWaveAt)
            return secondWaveSize;

        // At the beginning of the game, spawn 1 AI.
        return firstWaveSize;
    }

    // Creates one AI and places it somewhere around the
    // center of the arena.
    private void SpawnAI(int index)
    {
        // Choose a random position for this AI.
        Vector2 spawnPosition = GetSpawnPosition();

        // Instantiate creates a copy of the AI prefab.
        // Quaternion.identity means the AI starts with
        // no rotation.
        GameObject newAI = Instantiate(aiPrefab, spawnPosition, Quaternion.identity);

        Debug.Log("AI SPAWNED: " + newAI.name + " | Wave AI #" + (index + 1));
    }

    // This method is called by Rider when an AI is eliminated.
    // It increases the total defeat count used for wave
    // progression.
    public void RegisterAIDefeated()
    {
        totalAIDefeated++;

        Debug.Log("AI DEFEATED! Total AI defeated: " + totalAIDefeated);
    }

    // Chooses a random point around the arena center where
    // the AI will spawn.
    private Vector2 GetSpawnPosition()
    {
        Vector2 center;

        // If an Arena exists, use the Arena's center.
        if (Arena.Instance != null)
        {
            center = Arena.Instance.GetCenter();
        }
        else
        {
            // If there is no Arena, use the spawner's own
            // position as the center instead.
            center = transform.position;
        }

        // Creates a random direction pointing away from
        // the center.
        Vector2 direction = Random.insideUnitCircle.normalized;

        // Random.insideUnitCircle can very rarely return a
        // vector extremely close to zero.
        // Use Vector2.right as a safe fallback.
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right;

        // Move away from the center by the chosen distance.
        return center + direction * spawnDistanceFromCenter;
    }
}