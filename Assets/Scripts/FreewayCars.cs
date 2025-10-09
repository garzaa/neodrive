using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SplineArchitect.Objects;

public class FreewayCars : MonoBehaviour {
	List<SplineObject> cars = new();
	List<Vector3> carSpeeds = new();
	Spline spline;
	int i = 0;
	float splineLength;
	float baseSpeed = 5;
	float speedRange = 5;

	void Start() {
		spline = GetComponentInParent<Spline>();
		foreach (Transform child in transform) {
			cars.Add(child.GetComponent<SplineObject>());
			// move down the spline in a pseudo-random speed
			carSpeeds[i] = new Vector3(
				0,
				0,
				(speedRange * Mathf.Sin(((child.GetSiblingIndex() * 7 % 11) / transform.childCount)))+1+baseSpeed
			);
		}
		splineLength = spline.length;
	}

	void Update() {
		for (i=0; i<cars.Count; i++) {
			MovePositionOnSpline(i);
		}
	}

	void MovePositionOnSpline(int idx) {
		// move the car at idx by its speed / time.deltatime
		cars[i].splinePosition += carSpeeds[idx] * Time.deltaTime;
	}
}
