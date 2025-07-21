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
	public UnityEvent onInvalidFinish;

	RaceLogic raceLogic;

	RaceType raceType = RaceType.HOTLAP;
	bool finishedOnce = false;

	void Start() {
		StartCoroutine(WaitForSpawn());
		raceTimer = GameObject.Find("RaceTimer").GetComponent<Timer>();
		lapTimer = GameObject.Find("LapTimer").GetComponent<Timer>();
		timerAlert = FindObjectOfType<TimerAlert>();
		currentLap = new();
		checkpointSound = GetComponent<AudioSource>();
		raceLogic = GameObject.FindObjectOfType<RaceLogic>();
		FindObjectOfType<Car>().onRespawn.AddListener(OnRespawn);
	}

	public void RestartTimers() {
		raceTimer.Restart();
		lapTimer.Restart();
	}

	public void SetBestLap(Ghost ghost) {
		bestLap = new LapTime(ghost);
		lapRecord.text = lapTimer.FormattedTime(ghost.totalTime);
	}

	public void OnRespawn() {
		if (raceType == RaceType.ROUTE) {
			raceTimer.Restart();
			lapTimer.Restart();
		}
		checkpointsCrossed.Clear();
		finishedOnce = false;
	}

	public void SetRaceType(RaceType raceType) {
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
		// don't alert if you haven't crossed the finish yet on a hot lap
		// like if you're passing through checkpoints after spawning before starting an actual lap
		if (!checkpointsCrossed.Contains(c) && (raceType!=RaceType.HOTLAP || finishedOnce)) {
			currentLap.splits[c.name] = lapTimer.GetTime();
			if (bestLap != null) {
				if (!bestLap.splits.ContainsKey(c.name)) {
					// best lap can be invalid due to changing the map between editor runs
					bestLap = null;
				} else {
					float diff = t - bestLap.splits[c.name];
					string color = (diff > 0) ? "red" : "blue";
					tx += $"\n<color={color}>" + lapTimer.FormattedTime(diff, keepSign: true)+"</color>";
				}
			}
		}
		checkpointsCrossed.Add(c);
		timerAlert.Alert(tx);
	}

	void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Player")) {
			finishedOnce = true;
			checkpointSound.Play();
			onFinishCross.Invoke();
			if (checkpointsCrossed.Count == allCheckpoints.Count) {
				if (raceType == RaceType.ROUTE) {
					onValidFinish.Invoke();
					lapTimer.Pause();
					raceTimer.Pause();
					currentLap = new();
				} else {
					currentLap.totalTime = lapTimer.GetTime();
					if (bestLap == null || currentLap.totalTime < bestLap.totalTime) {
						bestLap = currentLap;
						currentLap = new();
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
				onInvalidFinish.Invoke();
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
