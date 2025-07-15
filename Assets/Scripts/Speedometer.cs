using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Speedometer : MonoBehaviour {
	public RectTransform tachNeedle;
    const float tachResting = 0;

	float speedThisStep, speedLastStep, delta;
	float redline = 180;
	float predictedSpeed;
	float changeSpeed;

	const float speedRange = Mathf.PI * 1.5f;

	public Image needle;
	public Image needleBlur;

	float speedFraction;

	Color c;

	public void SetSpeed(float carSpeed, float maxSpeed) {
		redline = maxSpeed;
		speedLastStep = speedThisStep;
		speedThisStep = carSpeed;
		delta = speedThisStep - speedLastStep;
		float speed = delta / Time.fixedDeltaTime;
		changeSpeed = speed ;
		predictedSpeed = speedThisStep;
	}

	void Update() {
		if (Time.timeScale == 0) return;
		predictedSpeed += changeSpeed * Time.deltaTime;
		speedFraction = Mathf.Clamp(Mathf.Abs(changeSpeed) / 10000f, 0, 1);

		c = needle.color;
		c.a = 1-speedFraction;
		needle.color = c;

		c = needleBlur.color;
		c.a = speedFraction;
		needleBlur.color = c;

        tachNeedle.rotation = Quaternion.Euler(new Vector3(
			0,
			0, 
			(tachResting - (predictedSpeed/redline)*speedRange) * Mathf.Rad2Deg
		));
	}
}
