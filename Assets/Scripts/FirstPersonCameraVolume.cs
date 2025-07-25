using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FirstPersonCameraVolume : MonoBehaviour {
	CameraRotate cameraRotate;

	void Start() {
		cameraRotate = FindObjectOfType<CameraRotate>();
	}

	public void OnTriggerEnter(Collider other) {
		if (other.gameObject.CompareTag("Player")) {
			cameraRotate.ForceFirstPerson();
		}
	}

	public void OnTriggerExit(Collider other) {
		if (other.gameObject.CompareTag("Player")) {
			cameraRotate.StopForcingFirstPerson();
		}
	}
}
