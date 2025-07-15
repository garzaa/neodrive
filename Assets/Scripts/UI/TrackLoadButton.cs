using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class TrackLoadButton : MonoBehaviour {
	public SceneReference track;

	void Start() {
		GetComponent<Button>().onClick.AddListener(() => FindObjectOfType<MainMenu>().LoadTrack(track));
	}
}
