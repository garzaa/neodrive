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
		GetComponent<Canvas>().worldCamera = Camera.current;
		vCam.SetActive(false);
		car = FindObjectOfType<Car>();
		pauseUI = GetComponent<CanvasGroup>();
		HideCanvas();
	}

	void Update() {
		if (InputManager.ButtonDown(Buttons.PAUSE)) {
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
		pauseUI.alpha = 1;
		GetComponentInChildren<Button>().Select();
		pauseUI.interactable = true;
		pauseUI.blocksRaycasts = true;
	}

	void HideCanvas() {
		pauseUI.alpha = 0;
		pauseUI.interactable = false;
		pauseUI.blocksRaycasts = false;
	}

	public void Exit() {
		Application.Quit();
	}

	public void ToggleGhost() {
		bool ghostEnabled = FindObjectOfType<RaceLogic>().ToggleGhost();
		ghostText.text = ghostEnabled ? "Hide Ghost" : "Show Ghost";
	}
}
