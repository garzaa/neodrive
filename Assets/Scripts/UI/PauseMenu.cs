using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine.Audio;

public class PauseMenu : MonoBehaviour {
	public GameObject vCam;
	public CinemachineVirtualCamera chaseCam;
	CanvasGroup pauseUI;
	Car car;
	bool paused = false;
	public Text ghostText;

	Quaternion targetRotation;
	Vector3 targetPosition;

	public AudioMixerSnapshot pausedAudio;
	public AudioMixerSnapshot unpausedAudio;

	void Start() {
		GetComponentInChildren<Canvas>(includeInactive: true).worldCamera = Camera.current;
		vCam.SetActive(false);
		car = FindObjectOfType<Car>();
		pauseUI = GetComponentInChildren<CanvasGroup>(includeInactive: true);
		HideCanvas();
		targetPosition = vCam.transform.localPosition;
		targetRotation = vCam.transform.localRotation;
		chaseCam = FindObjectOfType<CameraRotate>().GetComponentInChildren<CinemachineVirtualCamera>(includeInactive: true);
	}

	void Update() {
		if (paused) {
			vCam.transform.localPosition = Vector3.Slerp(
				vCam.transform.localPosition,
				targetPosition,
				0.05f
			);
			vCam.transform.localRotation = Quaternion.Slerp(
				vCam.transform.localRotation,
				targetRotation,
				0.05f
			);
		}

		if (InputManager.ButtonDown(Buttons.PAUSE)) {
			if (!paused && InputManager.Button(Buttons.CLUTCH)) {
				return;
			}
			if (Time.timeScale == 1 && !paused) {
				Pause();
			} else if (paused) {
				Unpause();
			}
		}
	}

	void Pause() {
		Time.timeScale = 0;
		paused = true;
		vCam.transform.SetPositionAndRotation(chaseCam.transform.position, chaseCam.transform.rotation);
		vCam.SetActive(true);
		car.SetDashboardEnabled(false);
		ShowCanvas();
		pausedAudio.TransitionTo(0.5f);
	}

	public void Unpause() {
		Time.timeScale = 1;
		paused = false;
		vCam.SetActive(false);
		car.SetDashboardEnabled(true);
		HideCanvas();
		unpausedAudio.TransitionTo(0.1f);
	}

	void ShowCanvas() {
		pauseUI.gameObject.SetActive(true);
		GetComponentInChildren<Button>(includeInactive: true).Select();
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

	public void Menu() {
		Time.timeScale = 1f;
		GetComponent<TransitionManager>().LoadScene("MainMenu");
	}
}
