using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class AlertText : MonoBehaviour {
	public Text mainText;
	public Text constantText;

	Animator anim;

	void Start() {
		anim = GetComponent<Animator>();
	}

	public void Alert(string text, bool constant=false) {
		if (constant) {
			constantText.text = text;
			anim.SetTrigger("TriggerConstant");
		} else {
			mainText.text = text;
			anim.SetTrigger("Trigger");
		}
	}
}
