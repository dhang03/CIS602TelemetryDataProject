using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Level_RiskReward : BaseLevelManager
{
    public float innerRadius = 2f;
    public float outerRadius = 4.5f;
    public bool isInnerRing = false;
    public string nextSceneName = "TimePressureScene";

    [Header("Switch Settings")]
    public float switchCooldown = 0.15f; 
    private float lastSwitchTime = 0f;
    private int totalSwitches = 0;
    private int missedCount = 0;

    [System.Serializable]
    private class SpawnTask { public string type; public string ring; }
    private List<SpawnTask> taskQueue = new List<SpawnTask>();
    private string currentSpawnRing = "";

    public override string GetModeName() => "ForcedSwitching";
    public override float GetCurrentRadius() => isInnerRing ? innerRadius : outerRadius;
    public bool IsInnerRingActive() => isInnerRing;

    protected override void Start()
    {
        collectibleCount = 6; obstacleCount = 4;
        base.Start();
    }

    protected override void BuildQueue()
    {
        taskQueue.Clear();
        for (int i = 0; i < collectibleCount; i++)
            taskQueue.Add(new SpawnTask { type = "Collectible", ring = Random.value > 0.5f ? "Inner" : "Outer" });
        for (int i = 0; i < obstacleCount; i++)
            taskQueue.Add(new SpawnTask { type = "Obstacle", ring = Random.value > 0.5f ? "Inner" : "Outer" });


        for (int i = taskQueue.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (taskQueue[i], taskQueue[j]) = (taskQueue[j], taskQueue[i]);
        }
    }

    protected override IEnumerator SpawnLoop()
    {
        while (taskQueue.Count > 0)
        {
            itemCollectedThisSpawn = false;
            SpawnTask task = taskQueue[0]; taskQueue.RemoveAt(0);
            currentSpawnRing = task.ring;

            float angle = GetSafeAngle(); usedAngles.Add(angle);
            float r = (task.ring == "Inner") ? innerRadius : outerRadius;
            Vector3 pos = centerPoint.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * r;
            currentObject = Instantiate(task.type == "Collectible" ? collectiblePrefab : obstaclePrefab, pos, Quaternion.identity);

            TelemetryLogger.LogEvent("item_spawn", new Dictionary<string, string> {
                { "mode", GetModeName() },
                { "type", task.type },
                { "spawn_ring", task.ring },
                { "player_ring", isInnerRing ? "Inner" : "Outer" }
            });

            Destroy(currentObject, 4.5f);
            yield return new WaitUntil(() => currentObject == null);

            if (task.type == "Collectible" && !itemCollectedThisSpawn)
            {
                missedCount++;
                TelemetryLogger.LogEvent("item_missed", new Dictionary<string, string> {
                    { "mode", GetModeName() },
                    { "total_missed", missedCount.ToString() }
                });
            }
            yield return new WaitForSeconds(spawnDelay);
        }
        EndLevel(true);
        StartCoroutine(DelayedLoad());
    }


    public void HandleRingSwitch(bool toInner, string buttonPressed)
    {
        if (Time.time - lastSwitchTime < switchCooldown) return;
        if (isInnerRing == toInner) return;

        isInnerRing = toInner;
        totalSwitches++;
        float latency = Time.time - lastSwitchTime;
        lastSwitchTime = Time.time;

        TelemetryLogger.LogEvent("ring_switch", new Dictionary<string, string> {
            { "target_ring", isInnerRing ? "inner" : "outer" },
            { "button_pressed", buttonPressed },
            { "switch_latency", latency.ToString("F2") },
            { "total_switches", totalSwitches.ToString() }
        });
    }

    public override void OnItemCollected()
    {
        itemCollectedThisSpawn = true;
        collectedCount++;
        bool matched = (isInnerRing == (currentSpawnRing == "Inner"));
        TelemetryLogger.LogEvent("item_collected", new Dictionary<string, string> {
            { "mode", GetModeName() },
            { "player_ring", isInnerRing ? "Inner" : "Outer" },
            { "item_ring", currentSpawnRing },
            { "rings_matched", matched.ToString() }
        });
    }

    public override void OnObstacleHit()
    {
        TelemetryLogger.LogEvent("obstacle_hit", new Dictionary<string, string> {
        { "mode", GetModeName() }
    });
        EndLevel(false);

        GameUIController ui = FindObjectOfType<GameUIController>();
        if (ui != null)
        {
            StartCoroutine(DelayedFail(ui));
        }
        else
        {
            StartCoroutine(DelayedLoad());
        }
    }

    IEnumerator DelayedFail(GameUIController uiController)
    {
        yield return new WaitForSeconds(0.5f);
        uiController.OnLevelFailed();
    }

    IEnumerator DelayedLoad() { yield return new WaitForSeconds(1.2f); SceneManager.LoadScene(nextSceneName); }
}