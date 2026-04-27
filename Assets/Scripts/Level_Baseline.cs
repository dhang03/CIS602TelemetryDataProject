using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Level_Baseline : BaseLevelManager
{
    [Header("Lifetimes (seconds)")]
    public float obstacleLifetime = 5f;      
    public float collectibleLifetime = 7f;  

    [Header("Spawn Proximity (degrees)")]
    public float obstacleSafeZoneDegrees = 35f;
    public float collectibleSafeZoneDegrees = 50f;

    [Header("Progression")]
    public string nextSceneName = "RiskRewardScene";

    public override string GetModeName() => "Baseline";
    public override float GetCurrentRadius() => spawnRadius;

    protected override IEnumerator SpawnLoop()
    {
        while (spawnQueue.Count > 0)
        {
            itemCollectedThisSpawn = false;
            string type = spawnQueue[0]; spawnQueue.RemoveAt(0);

            
            float safeZone = (type == "Obstacle") ? obstacleSafeZoneDegrees : collectibleSafeZoneDegrees;
            float angle = GetCustomSafeAngle(safeZone);
            usedAngles.Add(angle);

            Vector3 pos = centerPoint.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnRadius;
            currentObject = Instantiate(type == "Collectible" ? collectiblePrefab : obstaclePrefab, pos, Quaternion.identity);

            
            float lifetime = (type == "Obstacle") ? obstacleLifetime : collectibleLifetime;
            Destroy(currentObject, lifetime);

            yield return new WaitUntil(() => currentObject == null);

            if (type == "Collectible" && !itemCollectedThisSpawn)
                OnItemMissed();

            yield return new WaitForSeconds(spawnDelay);
        }
        EndLevel(true);
        LoadNextScene();
    }

    
    private float GetCustomSafeAngle(float minPlayerDiffDeg)
    {
        float pAngle = Mathf.Atan2(player.position.y - centerPoint.position.y, player.position.x - centerPoint.position.x);
        float angle; bool valid; int tries = 0;
        do
        {
            angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            valid = true;
            foreach (float u in usedAngles) if (AngleDiff(angle, u) < 40f * Mathf.Deg2Rad) { valid = false; break; }
            if (valid && AngleDiff(angle, pAngle) < minPlayerDiffDeg * Mathf.Deg2Rad) valid = false;
            tries++;
        } while (!valid && tries < 50);
        return angle;
    }

    public override void OnItemCollected()
    {
        itemCollectedThisSpawn = true;
        collectedCount++;
        TelemetryLogger.LogEvent("item_collected", new Dictionary<string, string> { { "mode", "Baseline" } });
    }

    public override void OnObstacleHit()
    {
        TelemetryLogger.LogEvent("obstacle_hit", new Dictionary<string, string> { { "mode", "Baseline" } });
        EndLevel(false);
        GameUIController ui = FindObjectOfType<GameUIController>();
        if (ui != null)
        {
            StartCoroutine(DelayedFail(ui));
        }
        else
        {
            LoadNextScene();
        }
    }

    IEnumerator DelayedFail(GameUIController uiController)
    {
        yield return new WaitForSeconds(0.5f);
        uiController.OnLevelFailed();
    }

    public override void OnItemMissed()
    {
        TelemetryLogger.LogEvent("item_missed", new Dictionary<string, string> {
            { "mode", "Baseline" },
            { "type", "collectible" }
        });
    }

    void LoadNextScene() => StartCoroutine(DelayedLoad());
    IEnumerator DelayedLoad() { yield return new WaitForSeconds(1.2f); SceneManager.LoadScene(nextSceneName); }
}