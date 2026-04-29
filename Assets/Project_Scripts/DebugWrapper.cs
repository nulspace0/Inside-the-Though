using UnityEngine;

public static class Log
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Info(string msg) => Debug.Log(msg);

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Warn(string msg) => Debug.LogWarning(msg);

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Error(string msg) => Debug.LogError(msg);
}
