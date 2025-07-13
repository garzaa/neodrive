using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RaceLogic : MonoBehaviour {
	Ghost recordingGhost;
	Ghost playingGhost;
	bool recording = false;
	bool playing = false;
	float recordStart;
	float playStart;
	Car playerCar;
	GhostCar ghostCar;

	int frameIndex = 0;

	void Start() {
		playerCar = FindObjectOfType<Car>();
		ghostCar = FindObjectOfType<GhostCar>();
	}

	public void StartRecordingGhost() {
		recordingGhost = new();
		recording = true;
		recordStart = Time.time;
	}

	void Update() {
		if (recording) {
			if (Time.timeScale > 0) {
				recordingGhost.frames.Add(new GhostFrame(
					Time.time-recordStart,
					playerCar.GetSnapshot()
				));
			}
		}
		if (playing) {
			if (frameIndex == playingGhost.frames.Count-1) {
				StopPlayingGhost();
				return;
			}

			float playTime = Time.time - playStart;
			while (
				playingGhost.frames[frameIndex].timestamp < playTime
				&& frameIndex < playingGhost.frames.Count-1
			) {
				frameIndex += 1;
			}
			ghostCar.ApplySnapshot(playingGhost.frames[frameIndex].snapshot);
		}
	}

	public Ghost StopRecordingGhost() {
		print("stopped ghost, saving");
		return recordingGhost;
	}

	public void PlayGhost(Ghost g) {
		frameIndex = 0;
		playingGhost = g;
		playing = true;
		playStart = Time.time;
		ghostCar.gameObject.SetActive(true);
	}

	public void StopPlayingGhost() {
		playing = false;
		ghostCar.gameObject.SetActive(false);
	}
}
