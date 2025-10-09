using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FreewayCar : MonoBehaviour {
	FreewayCars parentCars;

	void Start() {
		parentCars = GetComponentInParent<FreewayCars>();
	}

	void OnCollisionEnter(Collision collision) {
		if (!collision.gameObject.CompareTag("Player")) return;
		parentCars.OnCarHit(this);
		Debug.Log($"hit by {collision.collider.gameObject.name}");
	}
}
