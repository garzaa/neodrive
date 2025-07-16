using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RespawnTrigger : MonoBehaviour {
	Car car;

	void Start() {
		car = FindObjectOfType<Car>();
	}

	void OnTriggerEnter(Collider other) {
		if (other.tag == "Player") {
			car.Respawn();
		}
	}
}
