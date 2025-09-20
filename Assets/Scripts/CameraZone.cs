using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;

public class CameraZone : MonoBehaviour {
	CinemachineVirtualCamera replayCam;

	void Awake() {
		replayCam = GameObject.Find("CarTrackingCamera").GetComponent<CinemachineVirtualCamera>();
	}

	void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Player") || other.CompareTag("PlayerGhost")) {
			replayCam.transform.position = transform.position;
		}
	}
}
