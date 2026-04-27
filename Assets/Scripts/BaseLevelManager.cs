using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseLevelManager : MonoBehaviour
{
    public Transform centerPoint;
    public Transform player;
    public GameObject collectiblePrefab;
    public GameObject obstaclePrefab;
    public float spawnRadius = 3f;
    public int collectibleCount = 4;
    public int obstacleCount = 2;
    public float spawnDelay = 0.6f;

    protected List<string> spawnQueue = new List<string>();
    protected List<float> usedAngles = new List<float>();
    protected GameObject currentObject;
    public int collectedCount = 0;
    protected float levelStartTime;
    protected bool itemCollectedThisSpawn = false;

    public abstract string GetModeName();
    public abstract float GetCurrentRadius();
    public abstract void OnItemCollected();
    public abstract void OnObstacleHit();

    protected virtual void Start()
    {
        usedAngles.Clear();
        spawnQueue.Clear();
        BuildQueue();
    }

    public void StartLevel()
    {
        levelStartTime = Time.time;
        TelemetryLogger.LogEvent("level_start", new Dictionary<string, string> {
        { "mode", GetModeName() },
        { "collectibles_target", collectibleCount.ToString() },
        { "obstacles_target", obstacleCount.ToString() }
    });
        StartCoroutine(SpawnLoop());
    }

    protected virtual void BuildQueue()
    {
        spawnQueue.Clear();
        for (int i = 0; i < collectibleCount; i++) spawnQueue.Add("Collectible");
        for (int i = 0; i < obstacleCount; i++) spawnQueue.Add("Obstacle");
        spawnQueue.Shuffle();
    }

    protected virtual IEnumerator SpawnLoop()
    {
        while (spawnQueue.Count > 0)
        {
            itemCollectedThisSpawn = false;
            string type = spawnQueue[0]; spawnQueue.RemoveAt(0);
            float angle = GetSafeAngle(); usedAngles.Add(angle);
            Vector3 pos = centerPoint.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnRadius;
            currentObject = Instantiate(type == "Collectible" ? collectiblePrefab : obstaclePrefab, pos, Quaternion.identity);

            yield return new WaitUntil(() => currentObject == null);

            if (type == "Collectible" && !itemCollectedThisSpawn)
                OnItemMissed();

            yield return new WaitForSeconds(spawnDelay);
        }
        EndLevel(true);
    }

    protected float GetSafeAngle()
    {
        float pAngle = Mathf.Atan2(player.position.y - centerPoint.position.y, player.position.x - centerPoint.position.x);
        float angle; bool valid; int tries = 0;
        do
        {
            angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            valid = true;
            foreach (float u in usedAngles) if (AngleDiff(angle, u) < 40f * Mathf.Deg2Rad) { valid = false; break; }
            if (valid && AngleDiff(angle, pAngle) < 60f * Mathf.Deg2Rad) valid = false;
            tries++;
        } while (!valid && tries < 50);
        return angle;
    }

    protected float AngleDiff(float a, float b)
    {
        float d = Mathf.Abs(a - b);
        return (d > Mathf.PI) ? (2 * Mathf.PI - d) : d;
    }

    protected void EndLevel(bool completed)
    {
        TelemetryLogger.LogEvent("level_end", new Dictionary<string, string> {
            { "mode", GetModeName() },
            { "completed", completed.ToString() },
            { "collected", collectedCount.ToString() },
            { "time_seconds", (Time.time - levelStartTime).ToString("F2") }
        });
    }

    public virtual void OnItemMissed() { }
}