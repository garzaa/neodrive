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

	public float maxNitrox = 40000;
	public float nitroxBoost = 3f;
	public float boostDuration = 1f;

	public float drivelineFlex = 1f;

	// this will deal with torque curves and shit later on naturally
	public float brakeForce = 5f;

	[Tooltip("Get a speed boost with this many MPH on a perfect launch")]
	public float launchBoost = 10f;

	public float drag = 0.3f;
	
	[Header("Steering")]
	public float maxSteerAngle = 30;
	public float steerSpeed = 10f;
	[Tooltip("Force, in Gs, that the fromt wheels can hold")]
	public float maxCorneringGForce = 2f;
	public float tireSlip = 1f;

	public float burnoutThreshold = 500f;

	[Tooltip("Engine power multiplier when drifting to avoid speed loss")]
	public float driftBoost = 0.5f;

	[Tooltip("Rotational torque to apply when drifting, based on steer angle")]
	public float driftControl = 1f;
}
