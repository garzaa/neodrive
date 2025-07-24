using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using Cinemachine;
using System;
using NaughtyAttributes;

public class RaceLogic : MonoBehaviour {
	Ghost recordingGhost;
	bool recording = false;
	float recordStart;
	float playStart;
	Car car;
	public GhostCar playerGhostCar, authorGhostCar;

	bool ghostEnabled = true;

	public RaceType raceType = RaceType.ROUTE;
	public GameObject resultsCanvas;

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
		car = FindObjectOfType<Car>();
		playerGhostCar.gameObject.SetActive(false);
		authorGhostCar.gameObject.SetActive(false);
		resultsCanvas.SetActive(false);
		carTrackingCamera = GetComponentInChildren<CinemachineVirtualCamera>();
		carTrackingCamera.m_LookAt = car.transform;
		carTrackingCamera.m_Priority = 100;
		carTrackingCamera.enabled = false;

		finishLine = FindObjectOfType<FinishLine>();
		finishLine.SetRaceType(raceType);
		finishLine.onValidFinish.AddListener(OnValidFinish);
		finishLine.onInvalidFinish.AddListener(OnInvalidFinish);

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
				finishLine.SetBestLap(bestPlayerGhost);
			}
		}

		RenderScoreboard();

		car.onRespawn.AddListener(OnRespawn);
		car.onEngineStart.AddListener(FirstStart);

		if (raceType != RaceType.HOTLAP && !skipCountdown) {
			car.forceClutch = true;
			car.forceBrake = true;
		}

		FindObjectOfType<GameOptions>().Apply.AddListener(OnSettingsApply);
	}

	void OnRespawn() {
		StopCoroutine(nameof(CountdownAndStart));
		StartCoroutine(CountdownAndStart());
		StopPlayingGhosts();
	}

	public void StartRecordingGhost() {
		recordingGhost = new(Application.version) {
			playerName = "player"
		};
		recording = true;
		recordStart = Time.time;
	}

	void Update() {
		if (recording && Time.timeScale == 1) {
			recordingGhost.frames.Add(new GhostFrame(
				Time.time-recordStart,
				car.GetSnapshot()
			));
		}
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

		if (Application.isEditor && Input.GetKeyDown(KeyCode.S)) {
			if (authorGhost != null) {
				authorGhost.isAuthor = true;
				authorGhost.playerName = "author";
				// re-save author ghosts for data migration
				if (bestPlayerGhost != null) {
					if (authorGhost.totalTime < bestPlayerGhost.totalTime) {
						authorGhost.playerName = "author";
						authorGhost.isAuthor = true;
						print("overwrote author ghost in-place");
						saver.SaveGhost(authorGhost);
					} else if (bestPlayerGhost.totalTime < authorGhost.totalTime) {
						bestPlayerGhost.playerName = "author";
						bestPlayerGhost.isAuthor = true;
						saver.SaveGhost(bestPlayerGhost);
						// teehee
						bestPlayerGhost.isAuthor = false;
						bestPlayerGhost.playerName = "player";
						print("overwrote author with current player record");
					}
				} else if (bestPlayerGhost == null) {
					print("overwrote author ghost in-place");
					authorGhost.isAuthor = true;
					authorGhost.playerName = "author";
					saver.SaveGhost(authorGhost);
				} else {
					print("didn't do anything");
				}
			} else if (bestPlayerGhost != null) {
				bestPlayerGhost.playerName = "author";
				bestPlayerGhost.isAuthor = true;
				saver.SaveGhost(bestPlayerGhost);
				// teehee
				bestPlayerGhost.isAuthor = false;
				bestPlayerGhost.playerName = "player";
			} else {
				print("no best lap to save");
			}
		}
	}

	public void OnRaceStart() {
		car.forceClutch = false;
		car.forceBrake = false;
		finishLine.RestartTimers();
		PlayLoadedGhosts();
		StartRecordingGhost();
	}

	public void OnInvalidFinish() {
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
			p.splits = finishLine.GetBestLapSplits();
			bestPlayerGhost = p;
			player.time = p.totalTime;
			finishLine.SetBestLap(bestPlayerGhost);
			saver.SaveGhost(bestPlayerGhost);
		}
		if (raceType != RaceType.HOTLAP) {
			carTrackingCamera.enabled = true;
			car.forceClutch = true;
			car.forceBrake = true;
			StartCoroutine(ShowResults());
		} else {
			StartRecordingGhost();
			PlayLoadedGhosts();
		}
		RenderScoreboard();
	}

	public void PlayLoadedGhosts() {
		playStart = Time.time;
		if (bestPlayerGhost != null) {
			if (GameOptions.PlayerGhost) PlayGhost(bestPlayerGhost);
			if (authorGhost != null && bestPlayerGhost.totalTime < gold.time) {
				if (GameOptions.AuthorGhost) PlayGhost(authorGhost);
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

	void PlayGhost(Ghost g) {
		if (!ghostEnabled) {
			return;
		}
		// can only have two ghost cars right now
		playingGhosts[g.playerName] = new PlayingGhost(g, g.isAuthor ? authorGhostCar : playerGhostCar);
		if (g.isAuthor) {
			print("playing author ghost car");
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
		if (raceType == RaceType.HOTLAP || skipCountdown) {
			OnRaceStart();
			skipCountdown = false;
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
		countdown.text = "GO";
		countdownAnimator.SetTrigger("Animate");
		OnRaceStart();
	}

	IEnumerator ShowResults() {
		yield return new WaitForSeconds(0.5f);
		resultsCanvas.SetActive(true);
		// write the last time
		// then the best time
		// then display the medals for that time
		// (if the author medal exists)
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
}

public enum RaceType {
	ROUTE = 0,
	MULTILAP = 1,
	HOTLAP = 2
}
