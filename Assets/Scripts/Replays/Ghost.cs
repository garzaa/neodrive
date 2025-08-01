using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class Ghost {
	public List<GhostFrame> frames = new();
	public string playerName;
	public float totalTime;
	public string version;
	public bool isAuthor = false;
	[System.NonSerialized] public Dictionary<string, float> splits;

	[SerializeField] private List<string> splitNames;
	[SerializeField] private List<float> splitVals;

	public Ghost(string version) {
		this.version = version;
	}

	public void OnBeforeSerialize() {
		foreach (GhostFrame gf in frames) {
			gf.snapshot.OnBeforeSerialize();
		}
		splitNames = new();
		splitVals = new();
		foreach (var kv in splits) {
			splitNames.Add(kv.Key);
			splitVals.Add(kv.Value);
		}
	}

	public void OnAfterDeserialize() {
		foreach (GhostFrame gf in frames) {
			gf.snapshot.OnAfterDeserialize();
		}
		splits = new();
		// zip it back up
		for (int i=0; i<splitNames.Count; i++) {
			splits[splitNames[i]] = splitVals[i];
		}
	}
}

[System.Serializable]
public class GhostFrame {
	public float timestamp;
	public CarSnapshot snapshot;

	public GhostFrame(float t, CarSnapshot c) {
		timestamp = t;
		snapshot = c;
	}
}
