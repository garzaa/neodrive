using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class KickableProp : MonoBehaviour {
	Vector3 startPos;
	Quaternion startRot;
	Rigidbody rb;

	int defaultLayer;

	void Start() {
		startPos = transform.position;
		startRot = transform.rotation;
		rb = GetComponent<Rigidbody>();
		FindObjectOfType<Car>().onRespawn.AddListener(() => {
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			transform.position = startPos;
			transform.rotation = startRot;
			gameObject.layer = defaultLayer;
		});
		defaultLayer = gameObject.layer;
	}

	void OnCollisionEnter(Collision collision) {
		if (collision.gameObject.tag == "Player") {
			gameObject.layer = LayerMask.NameToLayer("Debris");
		}
	}
}
