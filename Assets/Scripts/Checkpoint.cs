using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class Checkpoint : MonoBehaviour {
	public UnityEvent onPlayerEnter;

	void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Player")) {
			onPlayerEnter.Invoke();
		}
	}
}
