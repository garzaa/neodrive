using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(TransitionManager))]
public class MainMenu : MonoBehaviour {
	TransitionManager tm;
	BinarySaver saver;

	void Start() {
		tm = GetComponent<TransitionManager>();
	}

	public void Exit() {
		Application.Quit();
	}

	public void LoadTrack(string track) {
		SceneManager.LoadScene(track);
	}
}
