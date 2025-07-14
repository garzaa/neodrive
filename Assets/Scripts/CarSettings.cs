using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Data/CarSettings")]
public class CarSettings : ScriptableObject {
	public float suspensionTravel;
	public float springStrength = 15000f;
	public float springDamper = 1000f;
	public LayerMask wheelRaycast;
	public float gearShiftTime = 0.25f;

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
	
	[Header("Steering")]
	public float maxSteerAngle = 30;
	public float steerSpeed = 10f;
	public AnimationCurve steerLimitCurve;
	public float maxCorneringForce = 20f;
	public float tireSlip = 1f;
	public bool tcs = true;
	[Range(0, 1)]
	public float tcsBraking = 0.2f;

	public float burnoutThreshold = 500f;

	[Header("Drifting")]
	[Tooltip("Engine power multiplier when drifting to avoid speed loss")]
	public float driftBoost = 0.5f;
	[Tooltip("Rotational torque to apply when drifting, based on steer angle")]
	public float driftControl = 1f;

	[Header("Air Control")]
	public float airSpinControl = 0.5f;
	public float airPitchControl = 0.5f;
}
