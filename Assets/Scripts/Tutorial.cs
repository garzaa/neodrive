using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tutorial : MonoBehaviour {
	// step 1 start car
	// step 2 put car in gear
	// step 3 shift
	// step 4 boost when meter is full

	public GameObject shiftPrompt;

	bool carStarted = false;
	bool shifted = false;
	Car car;

	void Start() {
		car = FindObjectOfType<Car>();
		car.forceBrake = true;
		shiftPrompt.SetActive(false);
	}

	void Update() {
		if (car.engineRunning && !carStarted) {
			carStarted = true;
			shiftPrompt.SetActive(true);
		}

		if (carStarted) {
			if (!shifted && car.currentGear == 1) {
				shifted = true;
			}
		}
	}
}
