using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;

public class FinishLine : MonoBehaviour {
	List<Checkpoint> allCheckpoints = new();
	readonly HashSet<Checkpoint> checkpointsCrossed = new();

	Timer raceTimer, lapTimer;
	TimerAlert timerAlert;

	LapTime bestLap = null;
	LapTime currentLap;

	AudioSource checkpointSound;
	bool crossedOnce = false;

	public Text lapRecord;

	public UnityEvent onFinishCross;

	RaceLogic raceLogic;
	Ghost bestLapGhost;

	void Start() {
		allCheckpoints = FindObjectsOfType<Checkpoint>().ToList();
		foreach (Checkpoint c in allCheckpoints) {
			c.onPlayerEnter.AddListener(() => OnCheckpointCrossed(c));
		}
		raceTimer = GameObject.Find("RaceTimer").GetComponent<Timer>();
		lapTimer = GameObject.Find("LapTimer").GetComponent<Timer>();
		timerAlert = FindObjectOfType<TimerAlert>();
		currentLap = new();
		checkpointSound = GetComponent<AudioSource>();
		raceLogic = GameObject.FindObjectOfType<RaceLogic>();
	}

	void OnCheckpointCrossed(Checkpoint c) {
		float t = lapTimer.GetTime();
		string tx = lapTimer.FormattedTime(t);
		if (!checkpointsCrossed.Contains(c)) {
			currentLap.splits[c] = lapTimer.GetTime();
			if (bestLap != null) {
				float diff = t - bestLap.splits[c];
				string color = (diff > 0) ? "red" : "blue";
				tx += $"\n<color={color}>" + lapTimer.FormattedTime(diff, keepSign: true)+"</color>";
			}
		}
		checkpointsCrossed.Add(c);
		timerAlert.Alert(tx);
	}

	void OnTriggerEnter(Collider other) {
		if (other.tag == "Player") {
			checkpointSound.Play();
			onFinishCross.Invoke();
			if (crossedOnce) {
				if (checkpointsCrossed.Count == allCheckpoints.Count) {
					// save the lap's ghost
					Ghost g = raceLogic.StopRecordingGhost();
					currentLap.totalTime = lapTimer.GetTime();
					if (bestLap == null || currentLap.totalTime < bestLap.totalTime) {
						bestLap = currentLap;
						currentLap = new();
						bestLapGhost = g;
						timerAlert.Alert("lap record "+lapTimer.GetFormattedTime());
						lapRecord.text = lapTimer.GetFormattedTime();
					} else {
						string tx = lapTimer.FormattedTime(lapTimer.GetTime());
						if (bestLap != null) {
							float diff = lapTimer.GetTime() - bestLap.totalTime;
							string color = (diff > 0) ? "red" : "blue";
							tx += $"\n<color={color}>" + lapTimer.FormattedTime(diff, keepSign: true)+"</color>";
						}
						timerAlert.Alert(tx);
					}
				}
				raceLogic.PlayGhost(bestLapGhost);
			} else {
				crossedOnce = true;
				raceTimer.Restart();
			}
			raceLogic.StartRecordingGhost();
			currentLap = new();
			checkpointsCrossed.Clear();
			lapTimer.Restart();
		}
	}
}

[System.Serializable]
public class LapTime {
	public float totalTime;
	public Dictionary<Checkpoint, float> splits = new();
}
