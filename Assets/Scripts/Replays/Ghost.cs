using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Ghost {
	public List<GhostFrame> frames = new();
	public string playerName;
	public float totalTime;
	public string version;
	public Dictionary<string, float> splits;

	public Ghost(string version) {
		this.version = version;
	}

	public void OnBeforeSerialize() {
		foreach (GhostFrame gf in frames) {
			gf.snapshot.OnBeforeSerialize();
		}
	}

	public void OnAfterDeserialize() {
		foreach (GhostFrame gf in frames) {
			gf.snapshot.OnAfterDeserialize();
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
