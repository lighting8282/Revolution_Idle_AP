using System;
using System.Collections.Generic;
using UnityEngine;

namespace RevolutionIdleAP;

// Thread-safe ring buffer of recent AP messages for the on-screen feed overlay. Written from the AP
// network thread (MessageLog / connection events), read on the main thread by RevApTicker.OnGUI.
public static class ApFeed
{
    public struct Entry
    {
        public string Text;
        public DateTime Time;
        public Color Color;
    }

    private const int Max = 80;
    private static readonly List<Entry> _entries = new();
    private static readonly object _lock = new();

    public static void Add(string text, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;
        lock (_lock)
        {
            _entries.Add(new Entry { Text = text, Time = DateTime.UtcNow, Color = color });
            if (_entries.Count > Max) _entries.RemoveRange(0, _entries.Count - Max);
        }
    }

    public static List<Entry> Snapshot()
    {
        lock (_lock) { return new List<Entry>(_entries); }
    }
}
