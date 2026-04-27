using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public Transform centerPoint;
    public float baseSpeed = 120f;
    public BaseLevelManager levelManager;

    private float currentAngle = 0f;
    private int direction = 1;
    private List<float> clickTimes = new List<float>();

    void Update()
    {
        if (levelManager != null && levelManager is Level_RiskReward rr)
        {
            if (Input.GetKeyDown(KeyCode.Q))
                rr.HandleRingSwitch(true, "Q"); 
            else if (Input.GetKeyDown(KeyCode.E))
                rr.HandleRingSwitch(false, "E"); 
        }

        if (Input.GetMouseButtonDown(0))
        {
            direction *= -1;
            TrackClicks();
            TelemetryLogger.LogEvent("direction_change", new Dictionary<string, string> {
            { "mode", levelManager != null ? levelManager.GetModeName() : "unknown" },
            { "angle", currentAngle.ToString("F2") },
            { "is_panic", (clickTimes.Count >= 3).ToString() },
            { "clicks_last_sec", clickTimes.Count.ToString() }
        });
        }
        float speed = baseSpeed; 
        if (levelManager is Level_RiskReward rr2 && rr2.IsInnerRingActive())
        {
            speed = baseSpeed * 1.3f;
        }
        else if (levelManager is Level_TimePressure tp)
        {
            speed = tp.GetCurrentPlayerSpeed();
        }

        currentAngle += direction * speed * Time.deltaTime;
        float r = levelManager != null ? levelManager.GetCurrentRadius() : 3f;
        float angleRad = currentAngle * Mathf.Deg2Rad;
        transform.position = centerPoint.position + new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0) * r;
    }

    void TrackClicks()
    {
        clickTimes.Add(Time.time);
        clickTimes.RemoveAll(t => Time.time - t > 1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (levelManager == null) return;
        if (other.CompareTag("Collectible"))
        {
            levelManager.OnItemCollected();
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Obstacle"))
        {
            levelManager.OnObstacleHit();
        }
    }
}