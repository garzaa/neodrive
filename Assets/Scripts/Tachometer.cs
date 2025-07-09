using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Tachometer : MonoBehaviour {
	public RectTransform tachNeedle;
    const float tachResting = 0;

	float rpmThisStep, rpmLastStep, delta;
	float redline;
	float predictedRPM;
	float rpmChangeSpeed;

	const float tachRange = Mathf.PI * 1.5f;

	public Image needle;
	public Image needleBlur;

	float speedFraction;

	Color c;

	public void SetRPM(float rpm, float engineRedline) {
		redline = engineRedline;
		rpmLastStep = rpmThisStep;
		rpmThisStep = rpm;
		delta = rpmThisStep - rpmLastStep;
		float speed = delta / Time.fixedDeltaTime;
		rpmChangeSpeed = speed;
		rpmChangeSpeed = Mathf.Clamp(rpmChangeSpeed, -4000, 4000);
		predictedRPM = rpmThisStep;
	}

	void Update() {
		predictedRPM += rpmChangeSpeed * Time.deltaTime;
		predictedRPM = Mathf.Clamp(predictedRPM, 0, redline+500);
		speedFraction = Mathf.Clamp(Mathf.Abs(rpmChangeSpeed) / 10000f, 0, 1);

		c = needle.color;
		c.a = 1-speedFraction;
		needle.color = c;

		c = needleBlur.color;
		c.a = speedFraction;
		needleBlur.color = c;

        tachNeedle.rotation = Quaternion.Euler(new Vector3(
			0,
			0, 
			(tachResting - predictedRPM/redline*tachRange) * Mathf.Rad2Deg
		));
	}
}
