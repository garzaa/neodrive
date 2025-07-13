using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ghost {
	public List<GhostFrame> frames = new();
	public string playerName = "crane";
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
