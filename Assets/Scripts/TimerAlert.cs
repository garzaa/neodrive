using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class TimerAlert : MonoBehaviour {
	Text alertText;
	Animator alertAnimator;

	void Start() {
		alertText = GetComponentInChildren<Text>();
		alertAnimator = GetComponent<Animator>();
	}

	public void Alert(string text) {
		alertText.text = text;
        alertAnimator.SetTrigger("Trigger");
	}
}
