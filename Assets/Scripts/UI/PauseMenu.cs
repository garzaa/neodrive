using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine.Audio;
using UnityEngine.Events;

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

	public GameObject settingsMenu;
	bool pausedThisFrame = false;
	
	public MainMenu mainMenu;

	public UnityEvent OnPause;

	void Start() {
		settingsMenu.SetActive(false);
		GetComponentInChildren<Canvas>(includeInactive: true).worldCamera = Camera.current;
		vCam.SetActive(false);
		car = FindObjectOfType<Car>();
		pauseUI = GetComponentInChildren<CanvasGroup>(includeInactive: true);
		HideCanvas();
		targetPosition = vCam.transform.localPosition;
		targetRotation = vCam.transform.localRotation;
		chaseCam = FindObjectOfType<CameraRotate>().GetComponentInChildren<CinemachineVirtualCamera>(includeInactive: true);
		FindObjectOfType<GameOptions>(includeInactive: true).Awake();
		GameOptions.Load();
		foreach (SettingsSlider slider in GetComponentsInChildren<SettingsSlider>()) {
			slider.OnEnable();
		}
	}

	void Update() {
		if (paused) {
			vCam.transform.SetLocalPositionAndRotation(Vector3.Slerp(
				vCam.transform.localPosition,
				targetPosition,
				0.05f
			), Quaternion.Slerp(
				vCam.transform.localRotation,
				targetRotation,
				0.05f
			));
		}

		if (InputManager.ButtonDown(Buttons.PAUSE)) {
			if (!paused && InputManager.Button(Buttons.CLUTCH)) {
				return;
			}
			if (Time.timeScale == 1 && !paused) {
				Pause();
			} else if (paused && !settingsMenu.activeInHierarchy) {
				Unpause();
			}
		}

		if (InputManager.ButtonDown(Buttons.UICANCEL)) {
			if (paused && !pausedThisFrame) {
				if (settingsMenu.activeInHierarchy) {
					HideSettings();
				} else {
					Unpause();
				}
			}
		}
		pausedThisFrame = false;
	}

	void Pause() {
		pausedThisFrame = true;
		Time.timeScale = 0;
		paused = true;
		vCam.transform.SetPositionAndRotation(chaseCam.transform.position, chaseCam.transform.rotation);
		vCam.SetActive(true);
		car.SetDashboardEnabled(false);
		ShowCanvas();
		pausedAudio.TransitionTo(0.5f);
		StartCoroutine(SelectNextFrame(gameObject));
		OnPause.Invoke();
	}

	public void Unpause() {
		Time.timeScale = 1;
		paused = false;
		vCam.SetActive(false);
		car.SetDashboardEnabled(true);
		HideSettings();
		HideCanvas();
		unpausedAudio.TransitionTo(0.1f);
	}

	void ShowCanvas() {
		pauseUI.gameObject.SetActive(true);
		StartCoroutine(SelectNextFrame(gameObject));
	}

	void HideCanvas() {
		pauseUI.gameObject.SetActive(false);
	}

	public void Exit() {
		mainMenu.UnsetFromTrack();
		SaveManager.WriteEternalSave();
		Application.Quit();
	}

	public void ShowSettings() {
		HideCanvas();
		settingsMenu.SetActive(true);
		StartCoroutine(SelectNextFrame(settingsMenu));
	}

	public void HideSettings() {
		settingsMenu.SetActive(false);
		ShowCanvas();
		StartCoroutine(SelectNextFrame(gameObject));
	}

	public void ToggleGhost() {
		bool ghostEnabled = FindObjectOfType<RaceLogic>().ToggleGhost();
		ghostText.text = ghostEnabled ? "Hide Ghost" : "Show Ghost";
	}

	public void Menu() {
		Time.timeScale = 1f;
		mainMenu.SetFromTrack();
		SaveManager.LoadScene("MainMenu");
	}

	IEnumerator SelectNextFrame(GameObject parent) {
		yield return new WaitForEndOfFrame();
		LayoutRebuilder.ForceRebuildLayoutImmediate(parent.GetComponentInChildren<RectTransform>());
		parent.GetComponentInChildren<Selectable>(includeInactive: true).Select();
	}
}
