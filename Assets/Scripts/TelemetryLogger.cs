using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public static class TelemetryLogger
{
    private static string playerId = "anon_" + UnityEngine.Random.Range(1000, 9999);
    private static string sessionId = "s_" + DateTimeOffset.Now.ToUnixTimeSeconds();
    private static string logPath = Path.Combine(Application.persistentDataPath, "telemetry_log.csv");

    static TelemetryLogger()
    {
        File.WriteAllText(logPath, "timestamp,player_id,session_id,event_type,properties\n");
        Debug.Log($"[Telemetry] Logging to: {logPath}");
    }

    public static void LogEvent(string eventType, Dictionary<string, string> properties = null)
    {
        string timestamp = DateTime.UtcNow.ToString("o");
        string propsStr = "";

        if (properties != null)
        {
            List<string> kvPairs = new List<string>();
            foreach (var kvp in properties)
            {
                string val = kvp.Value.Replace("\"", "\"\"").Replace(",", ";");
                kvPairs.Add($"\"{kvp.Key}\":\"{val}\"");
            }
            propsStr = "{" + string.Join(",", kvPairs) + "}";
        }

        string line = $"{timestamp},{playerId},{sessionId},{eventType},\"{propsStr}\"";
        File.AppendAllText(logPath, line + "\n");
    }

    public static void SaveToFile()
    {
        Debug.Log("[Telemetry] Session log finalized.");
    }
}