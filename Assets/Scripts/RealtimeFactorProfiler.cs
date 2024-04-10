using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

public class RealtimeFactorProfiler : MonoBehaviour
{
    static readonly ProfilerCounterValue<double> k_RealtimeFactor = new(ProfilerCategory.Scripts, "Realtime Factor",
        ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

    double last_real_time = 0.0;
    double last_unity_time = 0.0;
    double last_console_time = 0.0;

    void Start()
    {
        last_real_time = Time.realtimeSinceStartup;
        last_unity_time = Time.time;
        last_console_time = last_unity_time;
    }

    void FixedUpdate()
    {
        double real_time = Time.realtimeSinceStartup;
        double unity_time = Time.time;
        double realtime_factor = (unity_time - last_unity_time) / (real_time - last_real_time);
        last_real_time = real_time;
        last_unity_time = unity_time;
        k_RealtimeFactor.Value = realtime_factor;
        if (last_console_time + 1.0 < unity_time) {
            Debug.Log("Realtime Factor: " + realtime_factor);
            last_console_time = unity_time;
        }
    }
}
