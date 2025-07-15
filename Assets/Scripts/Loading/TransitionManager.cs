using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour {
	public void LoadScene(string sceneName) {
		SceneManager.LoadScene(sceneName);
	}
}
