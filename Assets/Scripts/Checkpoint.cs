using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class Checkpoint : MonoBehaviour {
	public UnityEvent onPlayerEnter;

	float lastCrossTime;

	public void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Player")) {
			if (Time.time < lastCrossTime+1f) return;
			lastCrossTime = Time.time;
			onPlayerEnter.Invoke();
		}
	}
}
