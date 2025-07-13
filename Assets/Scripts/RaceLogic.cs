using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RaceLogic : MonoBehaviour {
	Ghost currentGhost;
	bool recording = false;
	float startTimestamp;
	Car playerCar;

	void Start() {
		playerCar = FindObjectOfType<Car>();
	}

	public void StartSavingGhost() {
		currentGhost = new();
		recording = true;
		startTimestamp = Time.time;
	}

	void Update() {
		if (Time.timeScale > 0) {
			currentGhost.frames.Add(new GhostFrame(
				Time.time-startTimestamp,
				playerCar.GetSnapshot()
			));
		}
	}

	public void StopSavingGhost() {
		recording = false;
	}
}
