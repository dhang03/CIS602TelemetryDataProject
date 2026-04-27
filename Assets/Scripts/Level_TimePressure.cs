using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Level_TimePressure : BaseLevelManager
{
    [Header("Escalation Settings")]
    public float basePlayerSpeed = 140f;
    public float speedIncrementPerItem = 15f;
    public float baseItemLifetime = 2.8f;
    public float lifetimeDecrementPerItem = 0.10f;

    [Header("Safety Caps")]
    public float maxPlayerSpeed = 350f;
    public float minItemLifetime = 0.6f;

    [Header("Counts")]
    public int obstacleCount = 6;

    private int itemsCollected = 0;
    private float currentSpeed;
    private float currentLifetime;
    private int missedCount = 0;
    private int totalItemsSpawned = 0;

    public override string GetModeName() => "AcceleratingPressure";
    public override float GetCurrentRadius() => spawnRadius;
    public float GetCurrentPlayerSpeed() => currentSpeed;

    protected override void Start()
    {
        collectibleCount = 20;
        obstacleCount = 6;
        currentSpeed = basePlayerSpeed;
        currentLifetime = baseItemLifetime;
        base.Start();
    }

    protected override void BuildQueue()
    {
        spawnQueue.Clear();
        for (int i = 0; i < collectibleCount; i++) spawnQueue.Add("Collectible");
        for (int i = 0; i < obstacleCount; i++) spawnQueue.Add("Obstacle");
        spawnQueue.Shuffle();
    }

    protected override IEnumerator SpawnLoop()
    {
        while (spawnQueue.Count > 0)
        {
            itemCollectedThisSpawn = false;
            string type = spawnQueue[0]; spawnQueue.RemoveAt(0);
            totalItemsSpawned++;

            float angle = GetSafeAngle(); usedAngles.Add(angle);
            Vector3 pos = centerPoint.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnRadius;
            currentObject = Instantiate(type == "Collectible" ? collectiblePrefab : obstaclePrefab, pos, Quaternion.identity);

            float lifetime = (type == "Collectible") ? currentLifetime : currentLifetime * 1.5f;
            Destroy(currentObject, lifetime);

            TelemetryLogger.LogEvent("item_spawn", new Dictionary<string, string> {
                { "mode", GetModeName() },
                { "spawn_index", totalItemsSpawned.ToString() },
                { "type", type },
                { "player_speed", currentSpeed.ToString("F1") },
                { "time_limit", lifetime.ToString("F2") }
            });

            yield return new WaitUntil(() => currentObject == null);

            if (type == "Collectible" && !itemCollectedThisSpawn)
            {
                OnItemMissed();
            }
            yield return new WaitForSeconds(spawnDelay);
        }

        EndLevel(true);
        EndSession(); 
    }

    public override void OnItemCollected()
    {
        itemCollectedThisSpawn = true;

        itemsCollected++;   
        collectedCount++;    


        currentSpeed = Mathf.Min(basePlayerSpeed + (itemsCollected * speedIncrementPerItem), maxPlayerSpeed);
        currentLifetime = Mathf.Max(baseItemLifetime - (itemsCollected * lifetimeDecrementPerItem), minItemLifetime);

        TelemetryLogger.LogEvent("item_collected", new Dictionary<string, string> {
            { "mode", GetModeName() },
            { "item_index", itemsCollected.ToString() },
            { "new_speed", currentSpeed.ToString("F1") },
            { "new_time_limit", currentLifetime.ToString("F2") },
            { "missed_so_far", missedCount.ToString() }
        });
    }

    public override void OnObstacleHit()
    {
        TelemetryLogger.LogEvent("obstacle_hit", new Dictionary<string, string> {
            { "mode", GetModeName() },
            { "hit_index", totalItemsSpawned.ToString() },
            { "speed_at_hit", currentSpeed.ToString("F1") }
        });
        EndLevel(false);

        GameUIController ui = FindObjectOfType<GameUIController>();
        if (ui != null) StartCoroutine(DelayedFail(ui));
    }

    public override void OnItemMissed()
    {
        missedCount++;
        TelemetryLogger.LogEvent("item_missed", new Dictionary<string, string> {
            { "mode", GetModeName() },
            { "item_index", itemsCollected.ToString() },
            { "total_missed", missedCount.ToString() },
            { "speed_at_miss", currentSpeed.ToString("F1") },
            { "lifetime_at_miss", currentLifetime.ToString("F2") }
        });
    }

    void EndSession()
    {
        TelemetryLogger.LogEvent("session_end", new Dictionary<string, string> {
            { "final_mode", "AcceleratingPressure" },
            { "total_collected", itemsCollected.ToString() },
            { "total_missed", missedCount.ToString() }
        });
        TelemetryLogger.SaveToFile();


        GameUIController ui = FindObjectOfType<GameUIController>();
        if (ui != null) StartCoroutine(DelayedShowGameOver(ui));
    }

    IEnumerator DelayedFail(GameUIController ui)
    {
        yield return new WaitForSeconds(0.5f);
        ui.OnLevelFailed();
    }

    IEnumerator DelayedShowGameOver(GameUIController ui)
    {
        yield return new WaitForSeconds(0.8f); 
        ui.OnLevelComplete(); 
    }
}