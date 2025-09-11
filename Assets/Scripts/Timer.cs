using UnityEngine;
using UnityEngine.UI;
using System;
 
public class Timer : MonoBehaviour {
	public Text timerLabel;
	public bool realTime = false;
	public bool useDecimals = true;
	public float time = 0f;

	bool paused = true;

	float startTime;

	void Start() {
		startTime = realTime ? Time.unscaledTime : Time.time;
	}

	void Update() {
		if (paused) return;
		float currentTime = realTime ? Time.unscaledTime : Time.time;
		time = currentTime - startTime;
		if (timerLabel) timerLabel.text = FormattedTime(time);
	}

	public void ForceUpdate() {
		if (timerLabel) timerLabel.text = FormattedTime(time);
	}

	public string GetFormattedTime() {
		return FormattedTime(time);
	}

	public string FormattedTime(float t, bool keepSign = false) {
		string s;
		if (!useDecimals) s = TimeSpan.FromSeconds(t).ToString(@"mm\:ss");
		else s = TimeSpan.FromSeconds(t).ToString(@"mm\:ss\.ff");
		if (keepSign) {
			if (t >= 0) {
				s = "+" + s;
			} else {
				s = "-" + s;
			}
		}
		return s;
	}

	public void Pause() {
		paused = true;
	}

	public void Unpause() {
		paused = false;
	}

	public void Reset() {
		paused = true;
		SetTime(0);
	}

	public void Restart() {
		time = 0;
		startTime = realTime ? Time.unscaledTime : Time.time;
		paused = false;
	}

	public void SetTime(float t) {
		time = t;
	}

	public float GetTime() {
		return this.time;
	}
}
