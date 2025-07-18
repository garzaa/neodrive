using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using Cinemachine;
using System;

public class RaceLogic : MonoBehaviour {
	Ghost recordingGhost;
	Ghost playingGhost;
	bool recording = false;
	bool playing = false;
	float recordStart;
	float playStart;
	Car car;
	GhostCar ghostCar;

	int frameIndex = 0;
	bool ghostEnabled = true;


	public RaceType raceType = RaceType.ROUTE;
	public GameObject resultsCanvas;

	// this should be info about a playing ghost, not the ghost itself
	// need timestamps and all that
	// or...keep the current playing timestamp actually
	// ghosts wouldn't get out of sync, they all start when the player starts the race
	// or actually you do need which ghost car it's linked to, damn
	// then you might also need a list of which materials to replace with the ghost texture?
	Dictionary<string, PlayingGhost> playingGhosts;

	BinarySaver saver;

	NameTimePair bronze, silver, gold, author;

	Ghost bestPlayerGhost;
	Ghost currentPlayerGhost;
	Ghost authorGhost;

	public Transform scoreContainer;

	NameTimePair player = new("player", 0);

	public Animator countdownAnimator;
	public Text countdown;

	FinishLine finishLine;

	CinemachineVirtualCamera carTrackingCamera;

	struct NameTimePair {
		public string name;
		public float time;

		public NameTimePair(string n, float t) {
			name = n;
			time = t;
		}
	}

	void Start() {
		car = FindObjectOfType<Car>();
		ghostCar = FindObjectOfType<GhostCar>(includeInactive: true);
		ghostCar.gameObject.SetActive(false);
		resultsCanvas.SetActive(false);
		carTrackingCamera = GetComponentInChildren<CinemachineVirtualCamera>();
		carTrackingCamera.m_LookAt = car.transform;
		carTrackingCamera.m_Priority = 100;
		carTrackingCamera.enabled = false;

		finishLine = FindObjectOfType<FinishLine>();
		finishLine.SetRaceType(raceType);
		finishLine.onValidFinish.AddListener(OnRaceFinish);

		saver = new BinarySaver(SceneManager.GetActiveScene().name);
		authorGhost = saver.GetAuthorGhost();
		if (authorGhost != null) {
			print("found author ghost");
			author = new("author", authorGhost.totalTime);
			gold = new("gold", authorGhost.totalTime * 1.1f);
			silver = new("silver", authorGhost.totalTime * 1.2f);
			bronze = new("bronze", authorGhost.totalTime * 1.5f);
		}

		if (saver.GetGhosts().Count > 0) {
			Ghost player = saver.GetGhosts()[0];
			if (player != null) {
				bestPlayerGhost = player;
				this.player.time = bestPlayerGhost.totalTime;
				print("found player ghost with time " +this.player.time);
				finishLine.SetBestLap(bestPlayerGhost);
			}
		}

		RenderScoreboard();

		car.onRespawn.AddListener(() => StartCoroutine(CountdownAndStart()));
		car.onEngineStart.AddListener(FirstStart);

		if (raceType != RaceType.HOTLAP) {
			car.forceClutch = true;
			car.forceBrake = true;
		}
	}

	public void StartRecordingGhost() {
		print("started recording ghost");
		recordingGhost = new(Application.version) {
			// eventually name this after the player
			playerName = "player"
		};
		recording = true;
		recordStart = Time.time;
	}

	void Update() {
		if (recording) {
			if (Time.timeScale == 1) {
				recordingGhost.frames.Add(new GhostFrame(
					Time.time-recordStart,
					car.GetSnapshot()
				));
			}
		}
		if (playing && Time.timeScale == 1) {
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

		if (Application.isEditor && Input.GetKeyDown(KeyCode.S)) {
			if (bestPlayerGhost != null) {
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
		finishLine.RestartTimers();
		if (bestPlayerGhost != null) PlayGhost(bestPlayerGhost);
		StartRecordingGhost();
	}

	public void OnRaceFinish() {
		Ghost p = StopRecordingGhost();
		if (bestPlayerGhost == null || p.totalTime < bestPlayerGhost.totalTime) {
			player.time = p.totalTime;
			p.splits = finishLine.GetBestLapSplits();
			bestPlayerGhost = p;
			finishLine.SetBestLap(bestPlayerGhost);
			saver.SaveGhost(bestPlayerGhost);
		}
		if (raceType != RaceType.HOTLAP) {
			carTrackingCamera.enabled = true;
			print("race finish");
			// stop the car (force ebrake and neutral), show results, quit button
			car.forceClutch = true;
			car.forceBrake = true;
			car.ShutoffEngine();
			StartCoroutine(ShowResults());
			RenderScoreboard();
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
		Text[] texts = scoreContainer.GetComponentsInChildren<Text>();
		for (int i=0; i<pairs.Count; i++) {
			texts[i].text = pairs[i].name + "\n" + TimeSpan.FromSeconds(pairs[i].time).ToString(@"mm\:ss\.ff");
		}
	}

	public bool ToggleGhost() {
		if (ghostEnabled) {
			StopPlayingGhost();
		}
		ghostEnabled = !ghostEnabled;
		return ghostEnabled;
	}

	public Ghost StopRecordingGhost() {
		print("stopped recording ghost");
		recording = false;
		recordingGhost.totalTime = Time.time - recordStart;
		return recordingGhost;
	}

	public void PlayGhost(Ghost g) {
		if (!ghostEnabled) {
			return;
		}
		print("playiing ghost");
		frameIndex = 0;
		playingGhost = g;
		playing = true;
		playStart = Time.time;
		ghostCar.gameObject.SetActive(true);
	}

	public void StopPlayingGhost() {
		print("stopped playing ghost");
		playing = false;
		ghostCar.gameObject.SetActive(false);
	}

	public void HideResults() {
		resultsCanvas.SetActive(false);
	}

		public void FirstStart() {
		StartCoroutine(FirstStartRoutine());
	}

	IEnumerator FirstStartRoutine() {
		car.onEngineStart.RemoveListener(FirstStart);
		yield return new WaitForSeconds(1);
		StartCoroutine(CountdownAndStart());
	}

	IEnumerator CountdownAndStart() {
		carTrackingCamera.enabled = false;
		HideResults();
		StopCoroutine(ShowResults());
		if (raceType == RaceType.HOTLAP) {
			OnRaceStart();
			yield break;
		}
		car.forceBrake = true;
		car.forceClutch = true;
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
		car.forceClutch = false;
		car.forceBrake = false;
		countdown.text = "GO";
		countdownAnimator.SetTrigger("Animate");
		OnRaceStart();
	}

	IEnumerator ShowResults() {
		yield return new WaitForSeconds(0.5f);
		resultsCanvas.SetActive(true);
	}
}

public enum RaceType {
	ROUTE = 0,
	LAP = 1,
	HOTLAP = 2
}
