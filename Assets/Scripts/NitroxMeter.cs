using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NitroxMeter : MonoBehaviour {
	public float max = 1000;
	public float current = 0;
	public float needleSpeed = 100;
	public float needleRange = 180;

	public RectTransform needle;

	public void Add(float amount) {
		current += amount;
		current = Mathf.Clamp(current, 0, max);
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
