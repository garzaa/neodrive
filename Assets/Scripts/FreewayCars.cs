using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SplineArchitect.Objects;
using Unity.VisualScripting.Antlr3.Runtime.Tree;

public class FreewayCars : MonoBehaviour {
	List<SplineObject> cars = new();
	List<Vector3> carSpeeds = new();
	List<Vector3> startingPositions = new();
	Spline spline;
	int i = 0;
	float splineLength;
	float baseSpeed = 50 * Car.mph2u;
	float speedRange = 20 * Car.mph2u;

	HashSet<int> disabledCars = new();

	void Start() {
		spline = GetComponentInParent<Spline>();
		foreach (Transform child in transform) {
			SplineObject so = child.GetComponent<SplineObject>();
			cars.Add(so);
			// move down the spline in a pseudo-random speed
			carSpeeds.Add(new Vector3(
				0,
				0,
				(speedRange * Mathf.Sin((child.GetSiblingIndex() * 7 % 11) / transform.childCount))+1+baseSpeed
			));
			startingPositions.Add(so.splinePosition);
		}
		splineLength = spline.length;

		FindObjectOfType<Car>().onRespawn.AddListener(OnCarRespawn);
	}

	void Update() {
		for (i=0; i<cars.Count; i++) {
			MovePositionOnSpline(i);
		}
	}

	void MovePositionOnSpline(int idx) {
		if (disabledCars.Contains(idx)) return;

		cars[i].splinePosition += carSpeeds[idx] * Time.deltaTime;
		if (cars[i].splinePosition.z > splineLength) {
			cars[i].splinePosition = new Vector3(
				cars[i].splinePosition.x,
				cars[i].splinePosition.y,
				cars[i].splinePosition.z - splineLength
			);
		}
	}

	void OnCarRespawn() {
		for (i=0; i<cars.Count; i++) {
			SetKinematic(cars[i].gameObject);
			cars[i].splinePosition = startingPositions[i];
		}
		disabledCars.Clear();
	}

	void SetPhysical(GameObject freewayCar) {
		var rb = freewayCar.GetComponent<Rigidbody>();
		rb.isKinematic = false;
		freewayCar.GetComponent<FreewayCar>().enabled = false;
		int si = freewayCar.transform.GetSiblingIndex();
		rb.velocity += carSpeeds[si].z * freewayCar.transform.forward;
		disabledCars.Add(si);
	}

	void SetKinematic(GameObject freewayCar) {
		var rb = freewayCar.GetComponent<Rigidbody>();
		rb.isKinematic = true;
		freewayCar.GetComponent<FreewayCar>().enabled = true;
	}

	public void OnCarHit(FreewayCar freewayCar) {
		SetPhysical(freewayCar.gameObject);
	}
}
