using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tachometer : MonoBehaviour {
	public RectTransform tachNeedle;
    const float tachResting = 126 * Mathf.Deg2Rad;

	float rpmThisStep, rpmLastStep, delta;
	float redline;
	float offset;
	float predictedRPM;
	float rpmChangeSpeed;

	const float tachRange = Mathf.PI * 1.2f;

	public void SetRPM(float rpm, float engineRedline) {
		redline = engineRedline;
		rpmLastStep = rpmThisStep;
		rpmThisStep = rpm;
		delta = rpmThisStep - rpmLastStep;
		float speed = delta / Time.fixedDeltaTime;
		float rpmChangeSpeed = speed * Time.deltaTime;
		predictedRPM = rpmThisStep;
	}

	void Update() {
		predictedRPM += rpmChangeSpeed;
        tachNeedle.rotation = Quaternion.Euler(new Vector3(
			0,
			0, 
			(tachResting - (predictedRPM/redline)*tachRange) * Mathf.Rad2Deg
		));
	}
}
