using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour {
	// step 1 start car
	// step 2 put car in gear
	// step 3 shift
	// step 4 boost when meter is full

	public GameObject startPrompt, paddleStartPrompt;

	public GameObject shiftPrompt;
	public GameObject paddleShiftPrompt;
	public GameObject respawnPrompt, paddleRespawnPrompt;
	public GameObject boostPrompt;
	public CinemachineVirtualCamera vcam;

	public GameObject driveModeSelector;

	bool carStarted = false;
	bool shifted = false;
	Car car;
	NitroxMeter nitroxMeter;
	bool nitroxReady = false;

	GameOptions gameOptions;

	void Start() {
		car = FindObjectOfType<Car>();
		nitroxMeter = FindObjectOfType<NitroxMeter>();
		car.forceBrake = true;
		car.onRespawn.AddListener(() => vcam.enabled = false);
		shiftPrompt.SetActive(false);
		respawnPrompt.SetActive(false);
		paddleRespawnPrompt.SetActive(false);
		paddleShiftPrompt.SetActive(false);
		boostPrompt.SetActive(false);
		vcam.gameObject.SetActive(true);
		gameOptions = FindObjectOfType<GameOptions>();

		startPrompt.SetActive(false);
		paddleStartPrompt.SetActive(false);
		car.SetDashboardEnabled(false);
		StartCoroutine(SelectFirstChild());
	}

	IEnumerator SelectFirstChild() {
		yield return new WaitForEndOfFrame();
		GetComponentInChildren<Selectable>().Select();
	}

	void Update() {
		if (!carStarted && InputManager.ButtonDownWithManualClutch(Buttons.STARTENGINE)) {
			vcam.gameObject.SetActive(false);
		}

		if (car.engineRunning && !carStarted) {
			carStarted = true;
			if (GameOptions.PaddleShift) paddleShiftPrompt.SetActive(true);
			else shiftPrompt.SetActive(true);
		}

		if (carStarted) {
			if (!shifted && car.currentGear == 1) {
				shifted = true;
			}
		}

		if (shifted) {
			if (GameOptions.PaddleShift) paddleRespawnPrompt.SetActive(true);
			else respawnPrompt.SetActive(true);
			car.forceBrake = false;
		} else {
			car.forceBrake = true;
		}

		if (nitroxMeter.Ready()) {
			nitroxReady = true;
		}
		if (nitroxReady) {
			boostPrompt.SetActive(true);
		}
	}

	public void SetClutchShifting() {
		gameOptions.SetClutchShifting();
		startPrompt.SetActive(true);
		OnDriveModeSelect();
	}

	public void SetPaddleShifting() {
		gameOptions.SetPaddleShifting(); 
		paddleStartPrompt.SetActive(true);
		OnDriveModeSelect();
	}

	void OnDriveModeSelect() {
		driveModeSelector.SetActive(false);
		car.SetDashboardEnabled(true);
	}
}
