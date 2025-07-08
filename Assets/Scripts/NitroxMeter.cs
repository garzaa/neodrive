using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NitroxMeter : MonoBehaviour {
	public float max = 1000;
	float current = 0;
	float needleSpeed = 600;
	float needleRange = 180;

	public RectTransform needle;

	public void Add(float amount) {
		current += amount;
		current = Mathf.Clamp(current, 0, max);
	}

	public bool Ready() {
		return current == max;
	}

	public void OnBoost() {
		// make the needle rotate the correct way
		current = 1;
	}

	void Update() {
		float targetDeg = current/max * needleRange;
		needle.rotation = Quaternion.Euler(
			0,
			0,
			Mathf.MoveTowardsAngle(needle.rotation.eulerAngles.z, targetDeg, needleSpeed*Time.deltaTime)
		);
	}
}
