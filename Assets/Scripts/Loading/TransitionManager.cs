using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class TransitionManager : MonoBehaviour {
	public AudioMixerSnapshot defaultAudio;

	void Start() {
		defaultAudio.TransitionTo(0.25f);
	}

	public void LoadScene(string sceneName) {
		SceneManager.LoadScene(sceneName);
	}
}
