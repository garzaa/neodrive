using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PauseMenu : MonoBehaviour {
	public GameObject vCam;
	CanvasGroup pauseUI;
	Car car;
	bool paused = false;
	public Text ghostText;

	void Start() {
		GetComponentInChildren<Canvas>().worldCamera = Camera.current;
		vCam.SetActive(false);
		car = FindObjectOfType<Car>();
		pauseUI = GetComponentInChildren<CanvasGroup>();
		HideCanvas();
	}

	void Update() {
		if (InputManager.ButtonDown(Buttons.PAUSE)) {
			if (!paused && InputManager.Button(Buttons.CLUTCH)) {
				return;
			}
			if (Time.timeScale > 0 && !paused) {
				Time.timeScale = 0;
				paused = true;
				vCam.SetActive(true);
				car.SetDashboardEnabled(false);
				ShowCanvas();
			} else if (Time.timeScale == 0 && paused) {
				Unpause();
			}
		}
	}

	public void Unpause() {
		Time.timeScale = 1;
		paused = false;
		vCam.SetActive(false);
		car.SetDashboardEnabled(true);
		HideCanvas();
	}

	void ShowCanvas() {
		pauseUI.gameObject.SetActive(true);
		GetComponentInChildren<Button>().Select();
	}

	void HideCanvas() {
		pauseUI.gameObject.SetActive(false);
	}

	public void Exit() {
		Application.Quit();
	}

	public void ToggleGhost() {
		bool ghostEnabled = FindObjectOfType<RaceLogic>().ToggleGhost();
		ghostText.text = ghostEnabled ? "Hide Ghost" : "Show Ghost";
	}
}
