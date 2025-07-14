using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Ghost {
	public List<GhostFrame> frames = new();
	public string playerName;
	public string version;

	public Ghost(string version) {
		this.version = version;
	}
}

[System.Serializable]
public struct GhostFrame {
	public float timestamp;
	public CarSnapshot snapshot;

	public GhostFrame(float t, CarSnapshot c) {
		timestamp = t;
		snapshot = c;
	}
}
