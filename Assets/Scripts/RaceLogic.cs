using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Rewired.Integration.UnityUI;

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
	bool ghostEnabled = true;

	public GameObject resultsCanvas;

	void Start() {
		playerCar = FindObjectOfType<Car>();
		ghostCar = FindObjectOfType<GhostCar>(includeInactive: true);
		ghostCar.gameObject.SetActive(false);
		resultsCanvas.SetActive(false);

		// TODO: load the ghosts
		// player best, author best (which is saved in the application directory?)
		// should I manually load this, damn
		// todo: map of name -> playing ghost ghosts
		// unlock author ghost when getting gold
		// 1.1, 1.2, 1/5x time. ok
	}

	public void StartRecordingGhost() {
		recordingGhost = new(Application.version) {
			// eventually name this after the player
			// or also it can't always be me
			playerName = "crane"
		};
		recording = true;
		recordStart = Time.time;
	}

	void Update() {
		if (recording) {
			if (Time.timeScale == 1) {
				recordingGhost.frames.Add(new GhostFrame(
					Time.time-recordStart,
					playerCar.GetSnapshot()
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
	}

	public bool ToggleGhost() {
		if (ghostEnabled) {
			StopPlayingGhost();
		}
		ghostEnabled = !ghostEnabled;
		return ghostEnabled;
	}

	public Ghost StopRecordingGhost() {
		return recordingGhost;
	}

	public void PlayGhost(Ghost g) {
		if (!ghostEnabled) {
			return;
		}
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

	public void ShowResults() {
		// track medals here, if they got a higher medal then show it
		// also load the track best time and compute gold medals/etc
		// fuugck

		resultsCanvas.SetActive(true);
	}

	public void HideResults() {
		resultsCanvas.SetActive(false);
	}
}
