using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EngineLight : MonoBehaviour {
	Animator animator;

	void Start() {
		animator = GetComponent<Animator>();
	}

	public void Startup() {
		animator.SetTrigger("Startup");
	}

	public void Flash() {
		animator.SetTrigger("Flash");
	}

	public void SetOn() {
		animator.SetBool("On", true);
	}

	public void SetOff() {
		animator.SetBool("On", false);
	}
}
