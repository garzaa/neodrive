using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class FinishLine : MonoBehaviour {
	List<Checkpoint> allCheckpoints = new();
	readonly HashSet<Checkpoint> checkpointsCrossed = new();

	Timer raceTimer, lapTimer;
	TimerAlert timerAlert;

	LapTime bestLap = null;
	LapTime currentLap;

	AudioSource checkpointSound;

	public Text lapRecord;

	public UnityEvent onFinishCross;
	public UnityEvent onValidFinish;

	RaceLogic raceLogic;
	Ghost bestLapGhost = null;
	BinarySaver saver;

	RaceType raceType = RaceType.HOTLAP;

	void Start() {
		StartCoroutine(WaitForSpawn());
		raceTimer = GameObject.Find("RaceTimer").GetComponent<Timer>();
		lapTimer = GameObject.Find("LapTimer").GetComponent<Timer>();
		timerAlert = FindObjectOfType<TimerAlert>();
		currentLap = new();
		checkpointSound = GetComponent<AudioSource>();
		raceLogic = GameObject.FindObjectOfType<RaceLogic>();
		saver = new BinarySaver(SceneManager.GetActiveScene().name);
		FindObjectOfType<Car>().onRespawn.AddListener(OnRespawn);
	}

	public void RestartTimers() {
		raceTimer.Restart();
		lapTimer.Restart();
	}

	public void SetBestLap(Ghost ghost) {
		bestLapGhost = ghost;
		print("Setting new best lasp by ghost " + ghost.playerName);
		bestLap = new LapTime(ghost);
		lapRecord.text = lapTimer.FormattedTime(ghost.totalTime);
	}

	public void OnRespawn() {
		if (raceType == RaceType.ROUTE) {
			raceTimer.Restart();
			lapTimer.Restart();
		}
	}

	public void SetRaceType(RaceType raceType) {
		print("Setting race type to "+raceType);
		this.raceType = raceType;
		raceTimer.gameObject.SetActive(this.raceType != RaceType.ROUTE);
	}

	IEnumerator WaitForSpawn() {
		yield return new WaitForEndOfFrame();
		allCheckpoints = FindObjectsOfType<Checkpoint>().ToList();
		foreach (Checkpoint c in allCheckpoints) {
			c.onPlayerEnter.AddListener(() => OnCheckpointCrossed(c));
		}
	}

	void OnCheckpointCrossed(Checkpoint c) {
		float t = lapTimer.GetTime();
		string tx = lapTimer.FormattedTime(t);
		if (!checkpointsCrossed.Contains(c)) {
			currentLap.splits[c.name] = lapTimer.GetTime();
			if (bestLap != null) {
				float diff = t - bestLap.splits[c.name];
				string color = (diff > 0) ? "red" : "blue";
				tx += $"\n<color={color}>" + lapTimer.FormattedTime(diff, keepSign: true)+"</color>";
			}
		}
		checkpointsCrossed.Add(c);
		timerAlert.Alert(tx);
	}

	void OnTriggerEnter(Collider other) {
		if (other.tag == "Player") {
			print("finish crossed");
			checkpointSound.Play();
			onFinishCross.Invoke();
			if (checkpointsCrossed.Count == allCheckpoints.Count) {
				if (raceType == RaceType.ROUTE) {
					print("valid finish");
					onValidFinish.Invoke();
					lapTimer.Pause();
					raceTimer.Pause();
					currentLap = new();
				} else {
					// save the lap's ghost
					// racelogic should eventually take care of this
					// the splits thing is scary
					Ghost g = raceLogic.StopRecordingGhost();
					currentLap.totalTime = lapTimer.GetTime();
					if (bestLap == null || currentLap.totalTime < bestLap.totalTime) {
						print("new best lap: ");
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
					onValidFinish.Invoke();
				}
			} else {
				Debug.Log("missed checkpoints, invalid finish");
			}

			if (raceType == RaceType.HOTLAP) {
				if (bestLapGhost != null) raceLogic.PlayGhost(bestLapGhost);
				raceLogic.StartRecordingGhost();
			}
			if (raceType != RaceType.ROUTE) {
				lapTimer.Restart();
			}
			currentLap = new();
			checkpointsCrossed.Clear();
		}
	}

	public Dictionary<string, float> GetBestLapSplits() {
		// best lap is reset due to weird logic chaining between this
		// and racelogic. racelogic should handle it all
		// but until then, do this lol
		if (bestLap != null) return bestLap.splits;
		return currentLap.splits;
	}
}

public class LapTime {
	public float totalTime;
	public Dictionary<string, float> splits = new();

	public LapTime() {}

	public LapTime(Ghost g) {
		totalTime = g.totalTime;
		splits = g.splits;
	}
}
