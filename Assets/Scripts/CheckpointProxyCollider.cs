using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CheckpointProxyCollider : MonoBehaviour {
	Checkpoint checkpoint;
	FinishLine finishLine;

	void Start() {
		checkpoint = GetComponentInParent<Checkpoint>();
		finishLine = GetComponentInParent<FinishLine>();
	}

	void OnTriggerEnter(Collider other) {
		if (checkpoint) checkpoint.OnTriggerEnter(other);
		if (finishLine) finishLine.OnTriggerEnter(other);
	}
}
