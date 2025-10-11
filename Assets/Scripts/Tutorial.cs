using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;

public class Tutorial : MonoBehaviour {
	// step 1 start car
	// step 2 put car in gear
	// step 3 shift
	// step 4 boost when meter is full

	public GameObject shiftPrompt;
	public GameObject respawnPrompt;
	public GameObject boostPrompt;
	public CinemachineVirtualCamera vcam;

	bool carStarted = false;
	bool shifted = false;
	Car car;
	NitroxMeter nitroxMeter;
	bool nitroxReady = false;

	void Start() {
		car = FindObjectOfType<Car>();
		nitroxMeter = FindObjectOfType<NitroxMeter>();
		car.forceBrake = true;
		car.onRespawn.AddListener(() => vcam.enabled = false);
		shiftPrompt.SetActive(false);
		respawnPrompt.SetActive(false);
		boostPrompt.SetActive(false);
		vcam.gameObject.SetActive(true);
	}

	void Update() {
		if (!carStarted && InputManager.ButtonDown(Buttons.STARTENGINE) && InputManager.Clutch()) {
			vcam.gameObject.SetActive(false);
		}

		if (car.engineRunning && !carStarted) {
			carStarted = true;
			shiftPrompt.SetActive(true);
		}

		if (carStarted) {
			if (!shifted && car.currentGear == 1) {
				shifted = true;
			}
		}

		if (shifted) {
			respawnPrompt.SetActive(true);
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
}
