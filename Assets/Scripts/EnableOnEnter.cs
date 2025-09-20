using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnableOnEnter : MonoBehaviour {
	GameObject target;

	void Start() {
		target.SetActive(false);
	}

	void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Player")) {
			target.SetActive(true);
		}
	}
}
