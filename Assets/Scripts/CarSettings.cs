using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "Data/CarSettings")]
public class CarSettings : ScriptableObject {
	public float carMass = 10;
	public float suspensionTravel;
	public float springStrength = 15000f;
	public float springDamper = 60f;

	public LayerMask wheelRaycast;
	public float gearShiftTime = 0.25f;
	[Tooltip("Max RPM diff when shifting before lurching/breaking TCS. Below this is a perfect shift.")]
	public int maxRPMDiff = 1200;

	public bool enableNitrox = true;
	public float maxNitrox = 40000;
	[Tooltip("Engine power multiplier")]
	public float nitroxBoost = 3f;
	public float boostDuration = 1f;

	[Tooltip("Nitrox amount, per second, gained when drifting")]
	public float driftNitroGain = 300;
	[Tooltip("Nitrox amount, per second, gained when _almost_ invoking TCS at 100mph")]
	public float edgeNitroGain = 700;

	public float drivelineFlex = 1f;

	public float brakeForce = 5f;

	[Tooltip("Get a speed boost with this many MPH on a perfect launch")]
	public float launchBoost = 10f;

	public float drag = 0.3f;
	[Tooltip("Take drag and push the car down by this much of it")]
	public float downforceRatio = 0.5f;
	
	[Header("Steering")]
	public float maxSteerAngle = 30;
	public float steerSpeed = 10f;
	public AnimationCurve steerLimitCurve;
	public float maxCorneringAccel = 22f;
	[SerializeField] float tireSlip = 1f;

	[Header("Assists")]
	public bool tcs = true;
	[Range(0, 1)]
	public float tcsBraking = 0.2f;
	[Range(0, 1)]
	public float gripLimitThreshold = 0.7f;

	public bool lcs = true;
	[Tooltip("Forward/backward force the wheels can sustain before burning out/locking up")]
	public float burnoutThreshold = 20f;
	[Tooltip("Threshold above BurnoutThreshold where LCS turns off")]
	public float lcsLimit = 0.9f;

	[Header("Drifting")]
	[Tooltip("Engine power multiplier when drifting to avoid speed loss")]
	public float driftBoost = 0.5f;
	[Tooltip("Rotational torque to apply when drifting, based on steer angle")]
	public float driftControl = 1f;

	[Header("Air Control")]
	[Tooltip("Time in seconds taken to accelerate to max air spin speed")]
	public float airSpinAccel = 0.5f;
	[Tooltip("Air spin max speed, in degrees/second")]
	public float airSpinMaxSpeed = 50f;

	[Tooltip("Speed, in MPH, the car must be traveling to hydroplane")]
	public float hydroplaneSpeed = 90f;
	[Range(0f, 1f)]
	public float hydroplaneGrip = 0.1f;

	public float GetTireSlip(float forwardSpeed) {
		// if (forwardSpeed < 10f) {
		// 	return Mathf.Lerp(0.9f, tireSlip, forwardSpeed/10f);
		// }
		return tireSlip;
	}
}
