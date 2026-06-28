using System;
using UnityEngine;

namespace RevolutionIdleAP;

// Injected MonoBehaviour that drives main-thread work (~1/sec): achievement resync and goal detection.
public class RevApTicker : MonoBehaviour
{
    public RevApTicker(IntPtr ptr) : base(ptr) { }

    private float _timer;

    public void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < 1f) return;
        _timer = 0f;
        try { Plugin.Tick(); }
        catch (Exception e) { Plugin.Logger.LogError("[AP] tick error: " + e.Message); }
    }
}
