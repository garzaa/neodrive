using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using Cinemachine;
using System;
using NaughtyAttributes;
using UnityEngine.Events;
using NaughtyAttributes.Test;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
public class RaceLogic : MonoBehaviour {
	Ghost recordingGhost;
	bool recording = false;
	float recordStart;
	float playStart;
	Car car;
	public GhostCar playerGhostCar, authorGhostCar;

	bool ghostEnabled = true;

	public RaceType raceType = RaceType.HOTLAP;

	public Text medalText;
	public GameObject medal3DContainer;
	public GameObject medalTexture;
	public GameObject quitButtons;

	// this should be info about a playing ghost, not the ghost itself
	// need timestamps and all that
	// or...keep the current playing timestamp actually
	// ghosts wouldn't get out of sync, they all start when the player starts the race
	// or actually you do need which ghost car it's linked to, damn
	// then you might also need a list of which materials to replace with the ghost texture?
	readonly Dictionary<string, PlayingGhost> playingGhosts = new();

	BinarySaver saver;

	NameTimePair bronze, silver, gold, author;
	public Sprite bronzeSprite, silverSprite, goldSprite, authorSprite;

	Ghost bestPlayerGhost;
	Ghost authorGhost;

	public Transform scoreContainer;

	NameTimePair player = new("player", 0, null);

	public Animator countdownAnimator;
	public Text countdown;
	public bool skipCountdown;

	FinishLine finishLine;

	CinemachineVirtualCamera carTrackingCamera;

	readonly List<string> expiredGhosts = new();

	public Achievement firstAuthor;

	IEnumerator startRoutine;
	IEnumerator resultsRoutine;

	public UnityEvent onValidFinish, onInvalidFinish;

	Timer raceTimer, lapTimer;
	TimerAlert timerAlert;
	LapTime bestLap = null;
	LapTime currentLap;
	AudioSource checkpointSound;
	public Text lapRecord;
	bool finishedOnce = false;

	List<Checkpoint> allCheckpoints = new();
	readonly HashSet<Checkpoint> checkpointsCrossed = new();

	public List<GameObject> photoModeDisable;

	struct NameTimePair {
		public string name;
		public float time;
		public Sprite sprite;

		public NameTimePair(string n, float t, Sprite s) {
			name = n;
			time = t;
			sprite = s;
		}
	}

	void Start() {
		finishLine = FindObjectOfType<FinishLine>();
		FindObjectOfType<GameOptions>().Apply.AddListener(OnSettingsApply);
		car = FindObjectOfType<Car>();
		
		if (quitButtons != null) quitButtons.SetActive(false);
		if (medalText != null) medalText.gameObject.SetActive(false);
		
		if (finishLine == null) return;
		
		raceTimer = transform.Find("RaceTimer").GetComponent<Timer>();
		lapTimer = transform.Find("LapTimer").GetComponent<Timer>();
		FindObjectOfType<PauseMenu>(includeInactive: true).OnPause.AddListener(OnPause);
		timerAlert = FindObjectOfType<TimerAlert>();
		currentLap = new();
		checkpointSound = GetComponent<AudioSource>();

		startRoutine = CountdownAndStart();
		resultsRoutine = ShowResults();
		playerGhostCar.gameObject.SetActive(false);
		authorGhostCar.gameObject.SetActive(false);
		carTrackingCamera = finishLine.GetComponentInChildren<CinemachineVirtualCamera>();
		carTrackingCamera.m_LookAt = car.transform;
		carTrackingCamera.m_Priority = 100;
		carTrackingCamera.enabled = false;


		saver = new BinarySaver(SceneManager.GetActiveScene().name);
		authorGhost = saver.GetAuthorGhost();
		if (authorGhost != null) {
			print("found author ghost");
			author = new("author", authorGhost.totalTime, authorSprite);
			gold = new("gold", authorGhost.totalTime * 1.1f, goldSprite);
			silver = new("silver", authorGhost.totalTime * 1.2f, silverSprite);
			bronze = new("bronze", authorGhost.totalTime * 1.5f, bronzeSprite);
		}

		if (saver.GetGhosts().Count > 0) {
			Ghost player = saver.GetGhosts()[0];
			if (player != null) {
				bestPlayerGhost = player;
				this.player.time = bestPlayerGhost.totalTime;
				SetBestLap(bestPlayerGhost);
			}
		}

		if (bestPlayerGhost != null &&
			authorGhost != null &&
			bestPlayerGhost.totalTime < authorGhost.totalTime) {
			firstAuthor.Get();
		}

		RenderScoreboard();

		car.onRespawn.AddListener(OnRespawn);
		car.onEngineStart.AddListener(FirstStart);
		onValidFinish.AddListener(OnValidFinish);
		onInvalidFinish.AddListener(OnInvalidFinish);
		finishLine.onFinishCross.AddListener(OnFinishCross);

		if (raceType != RaceType.HOTLAP && !skipCountdown) {
			car.forceClutch = true;
			car.forceBrake = true;
		}

		raceTimer.gameObject.SetActive(raceType != RaceType.ROUTE);
		StartCoroutine(WaitForSpawn());
		HideResults();
	}

	IEnumerator WaitForSpawn() {
		yield return new WaitForEndOfFrame();
		allCheckpoints = FindObjectsOfType<Checkpoint>().ToList();
		foreach (Checkpoint c in allCheckpoints) {
			c.onPlayerEnter.AddListener(() => OnCheckpointCrossed(c));
		}
	}

	public void SetBestLap(Ghost ghost) {
		bestLap = new LapTime(ghost);
		lapRecord.text = lapTimer.FormattedTime(ghost.totalTime);
	}

	void OnCheckpointCrossed(Checkpoint c) {
		float t = lapTimer.GetTime();
		string tx = lapTimer.FormattedTime(t);
		// don't alert if you haven't crossed the finish yet on a hot lap
		// like if you're passing through checkpoints after spawning before starting an actual lap
		if (!checkpointsCrossed.Contains(c) || (raceType==RaceType.HOTLAP && !finishedOnce)) {
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

	void OnRespawn() {
		StopCoroutine(startRoutine);
		startRoutine = CountdownAndStart();
		StartCoroutine(startRoutine);
		StopPlayingGhosts();
		medalText.gameObject.SetActive(false);
		medal3DContainer.SetActive(false);
		medalTexture.SetActive(false);
		raceTimer.Restart();
		lapTimer.Restart();
		checkpointsCrossed.Clear();
		finishedOnce = false;
	}

	public void OnPhotoModeChange(bool photoMode) {
		foreach (GameObject g in photoModeDisable) {
			if (photoMode) g.SetActive(false);
			else g.SetActive(true);
		}
	}

	public void StartRecordingGhost() {
		recordingGhost = new(Application.version) {
			playerName = "player"
		};
		recording = true;
		recordStart = Time.time;
	}

	void OnPause() {
		HideResults();
	}

	void Update() {
		if (recording && Time.timeScale == 1) {
			recordingGhost.frames.Add(new GhostFrame(
				Time.time-recordStart,
				car.GetSnapshot()
			));
		}
		// if (EventSystem.current.IsPointerOverGameObject()) {
		// 	Debug.Log($"pointer over {GetEventSystemRaycastResults()[0].gameObject.name}");
		// }
		if (Time.timeScale > 0) {
			foreach (string ghostName in playingGhosts.Keys) {
				PlayingGhost pg = playingGhosts[ghostName];
				float playTime = Time.time - playStart;
				while (
					pg.ghost.frames[pg.frameIndex].timestamp < playTime
					&& pg.frameIndex < pg.ghost.frames.Count-1
				) {
					pg.frameIndex += 1;
				}
				pg.car.ApplySnapshot(pg.ghost.frames[pg.frameIndex].snapshot);
				
				if (pg.frameIndex >= pg.ghost.frames.Count-2) {
					expiredGhosts.Add(ghostName);
					continue;
				}
			}
		}
		foreach (string n in expiredGhosts) {
			StopPlayingGhost(playingGhosts[n].ghost);
			playingGhosts.Remove(n);
		}
		expiredGhosts.Clear();

		if (Application.isEditor && Input.GetKeyDown(KeyCode.Y)) {
			if (authorGhost != null) {
				authorGhost.playerName = "author";
				// re-save author ghosts for data migration
				if (bestPlayerGhost != null) {
					if (authorGhost.totalTime < bestPlayerGhost.totalTime) {
						authorGhost.playerName = "author";
						print("overwrote author ghost in-place");
						saver.SaveGhost(authorGhost);
					} else if (bestPlayerGhost.totalTime < authorGhost.totalTime) {
						bestPlayerGhost.playerName = "author";
						saver.SaveGhost(bestPlayerGhost);
						// teehee
						bestPlayerGhost.playerName = "player";
						print("overwrote author with current player record");
					}
				} else if (bestPlayerGhost == null) {
					print("overwrote author ghost in-place");
					authorGhost.playerName = "author";
					saver.SaveGhost(authorGhost);
				} else {
					print("didn't do anything");
				}
			} else if (bestPlayerGhost != null) {
				bestPlayerGhost.playerName = "author";
				saver.SaveGhost(bestPlayerGhost);
				// teehee
				bestPlayerGhost.playerName = "player";
			} else {
				print("no best lap to save");
			}
		}
	}

	public void OnRaceStart() {
		car.forceClutch = false;
		car.forceBrake = false;
		RestartTimers();
		PlayLoadedGhosts();
		StartRecordingGhost();
	}

	public void RestartTimers() {
		raceTimer.Restart();
		lapTimer.Restart();
	}

	public void OnFinishCross() {
		finishedOnce = true;
		checkpointSound.Play();
		if (checkpointsCrossed.Count == allCheckpoints.Count) {
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
			
			if (raceType == RaceType.ROUTE) {
				onValidFinish.Invoke();
				lapTimer.Pause();
				raceTimer.Pause();
				currentLap = new();
			} else {
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

	void OnInvalidFinish() {
		// should still stop and do a new ghost if you're starting a new lap
		if (raceType == RaceType.HOTLAP) {
			StopRecordingGhost();
			StartRecordingGhost();
			PlayLoadedGhosts();
		}
	}

	void OnValidFinish() {
		Ghost p = StopRecordingGhost();
		if (bestPlayerGhost == null || p.totalTime < bestPlayerGhost.totalTime) {
			player.time = p.totalTime;
			p.splits = GetBestLapSplits();
			bestPlayerGhost = p;
			player.time = p.totalTime;
			SetBestLap(bestPlayerGhost);
			saver.SaveGhost(bestPlayerGhost);
		}
		if (raceType != RaceType.HOTLAP) {
			carTrackingCamera.enabled = true;
			car.forceClutch = true;
			car.forceBrake = true;
			resultsRoutine = ShowResults();
			StartCoroutine(resultsRoutine);
		} else {
			StartRecordingGhost();
			PlayLoadedGhosts();
		}
		RenderScoreboard();

		Tuple<string, Sprite> resultData = GetBestMedal(p.totalTime);
		if (raceType == RaceType.ROUTE) {
			medalText.text = resultData.Item1;
			if (resultData.Item2 != null) {
				// medalcontainer > medal > gold/silver/bronze
				foreach (Transform medal in medal3DContainer.transform.GetChild(0)) {
					medal.gameObject.SetActive(medal.name == resultData.Item1);
				}
			}
			if (string.IsNullOrEmpty(resultData.Item1)) {
				medalText.enabled = false;
			} else {
				medalText.enabled = true;
			}
		}
	}

	Dictionary<string, float> GetBestLapSplits() {
		// best lap is reset due to weird logic chaining between this
		// and racelogic. racelogic should handle it all
		// but until then, do this lol
		if (bestLap != null) return bestLap.splits;
		return currentLap.splits;
	}

	public Tuple<string, Sprite> GetBestMedal(float playerTime) {
		if (playerTime <= author.time) {
			firstAuthor.Get();
			return new Tuple<string, Sprite>("Author Medal", author.sprite);
		}
		if (playerTime <= gold.time) {
			return new Tuple<string, Sprite>("Gold Medal", gold.sprite);
		}
		if (playerTime <= silver.time) {
			return new Tuple<string, Sprite>("Silver Medal", silver.sprite);
		}
		if (playerTime <= bronze.time) {
			return new Tuple<string, Sprite>("Bronze Medal", bronze.sprite);
		}

		if (author.time == 0) return new Tuple<string, Sprite>("", null);

		return new Tuple<string, Sprite>("No Medal", null);
	}

	public void PlayLoadedGhosts() {
		playStart = Time.time;
		if (authorGhost != null) authorGhost.playerName = "author";
		if (bestPlayerGhost != null) {
			if (GameOptions.PlayerGhost) PlayGhost(bestPlayerGhost, isAuthor: false);
			if (authorGhost != null && bestPlayerGhost.totalTime < gold.time) {
				if (GameOptions.AuthorGhost) PlayGhost(authorGhost, isAuthor: true);
			}
		}
	}

	void RenderScoreboard() {
		if (authorGhost == null) {
			scoreContainer.gameObject.SetActive(false);
			return;
		}
		scoreContainer.gameObject.SetActive(true);
		// arrange the player/medal times by time
		List<NameTimePair> pairs = new(){
			player,
			author,
			gold,
			silver,
			bronze
		};

		pairs = pairs.OrderBy(x => {
			if (x.time <= 0) {
				return float.PositiveInfinity;
			} else {
				return x.time;
			}
		}).ToList();
		Text[] texts = scoreContainer.GetComponentsInChildren<Text>(includeInactive: true);
		for (int i = 0; i < pairs.Count; i++) {
			if (pairs[i].time < player.time || (pairs[i].name != player.name && player.time == 0)) {
				// only show author medal if player's gotten a gold medal
				if (player.time != 0 && player.time < gold.time && pairs[i].name == "author") {
					texts[i].transform.parent.gameObject.SetActive(true);
				} else {
					texts[i].transform.parent.gameObject.SetActive(false);
				}
			} else {
				texts[i].transform.parent.gameObject.SetActive(true);
			}
			texts[i].text = pairs[i].name + "\n" + TimeSpan.FromSeconds(pairs[i].time).ToString(@"mm\:ss\.ff");
			Image s = texts[i].transform.parent.GetComponentsInChildren<Image>()[1];
			s.sprite = pairs[i].sprite;
			s.enabled = pairs[i].sprite != null;
		}
	}

	public bool ToggleGhost() {
		if (ghostEnabled) {
			StopPlayingGhosts();
		}
		ghostEnabled = !ghostEnabled;
		return ghostEnabled;
	}

	public Ghost StopRecordingGhost() {
		recording = false;
		recordingGhost.totalTime = Time.time - recordStart;
		return recordingGhost;
	}

	void PlayGhost(Ghost g, bool isAuthor) {
		if (!ghostEnabled) {
			return;
		}
		// can only have two ghost cars right now
		playingGhosts[g.playerName] = new PlayingGhost(g, isAuthor ? authorGhostCar : playerGhostCar);
		if (isAuthor) {
			authorGhostCar.gameObject.SetActive(true);
		} else {
			playerGhostCar.gameObject.SetActive(true);
		}
	}

	void StopPlayingGhosts() {
		foreach (PlayingGhost pg in playingGhosts.Values) {
			HaltGhost(pg.car);
		}
		playingGhosts.Clear();
	}

	void StopPlayingGhost(Ghost g) {
		if (g == null) return;
		if (!playingGhosts.ContainsKey(g.playerName)) return;
		HaltGhost(playingGhosts[g.playerName].car);
	}

	void HaltGhost(GhostCar gc) {
		gc.ApplySnapshot(new CarSnapshot(
			Vector3.zero,
			Quaternion.identity,
			0,
			0,
			0,
			false,
			false,
			true
		));
		gc.gameObject.SetActive(false);
	}

	public void HideResults() {
		StopCoroutine(resultsRoutine);
		medalText.gameObject.SetActive(false);
		medal3DContainer.SetActive(false);
		medalTexture.SetActive(false);
		quitButtons.SetActive(false);
	}

	public void FirstStart() {
		StartCoroutine(FirstStartRoutine());
	}

	IEnumerator FirstStartRoutine() {
		car.onEngineStart.RemoveListener(FirstStart);
		yield return new WaitForSeconds(1);
		StartCoroutine(startRoutine);
	}

	IEnumerator CountdownAndStart() {
		carTrackingCamera.enabled = false;
		HideResults();
		StopCoroutine(resultsRoutine);
		if (raceType == RaceType.HOTLAP || skipCountdown) {
			OnRaceStart();
			skipCountdown = false;
			yield break;
		}
		car.forceBrake = true;
		car.forceClutch = true;
		countdownAnimator.gameObject.SetActive(true);
		car.ChangeGear(1);
		yield return new WaitForSeconds(0.25f);
		countdown.text = "3";
		countdownAnimator.SetTrigger("Animate");
		yield return new WaitForSeconds(0.5f);
		countdown.text = "2";
		countdownAnimator.SetTrigger("Animate");
		yield return new WaitForSeconds(0.5f);
		countdown.text = "1";
		countdownAnimator.SetTrigger("Animate");
		yield return new WaitForSeconds(0.5f);
		countdown.text = "GO";
		countdownAnimator.SetTrigger("Animate");
		OnRaceStart();
		yield return new WaitForSecondsRealtime(0.5f);
		countdownAnimator.gameObject.SetActive(false);
	}

	IEnumerator ShowResults() {
		yield return new WaitForSeconds(2f);
		medalText.gameObject.SetActive(true);
		if (medalText.text != "" && medalText.text.ToLower() != "no medal") {
			medalTexture.SetActive(true);
			medal3DContainer.SetActive(true);
		}
		medalText.text = SceneManager.GetActiveScene().name + "\n" + medalText.text;
		quitButtons.SetActive(true);
	}

	[Button("Invalidate Times")]
	void InvalidateTime() {
		saver ??= new BinarySaver(SceneManager.GetActiveScene().name);
		saver.DeleteAuthorGhost();
		saver.DeletePlayerGhost();
	}

	void OnSettingsApply() {
		if (!GameOptions.AuthorGhost) {
			StopPlayingGhost(authorGhost);
		}
		if (!GameOptions.PlayerGhost) {
			StopPlayingGhost(bestPlayerGhost);
		}
	}
	
	public void ApplyWheel(CustomWheel wheel) {
		if (playerGhostCar == null) return;
		foreach (Wheel w in playerGhostCar.GetComponentsInChildren<Wheel>()) {
			w.ApplyCustomWheel(wheel);
		}
	}

	List<RaycastResult> GetEventSystemRaycastResults() {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        return raycastResults;
    }

	public void MenuButtonClick() {
		FindObjectOfType<PauseMenu>(includeInactive: true).Menu();
	}

	public void RestartButtonClick() {
		car.Respawn();
	}
}

public enum RaceType {
	ROUTE = 0,
	MULTILAP = 1,
	HOTLAP = 2
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
