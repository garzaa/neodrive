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

	// this will deal with torque curves and shit later on naturally
	public float brakeForce = 5f;

	[Tooltip("Get a speed boost with this many MPH on a perfect launch")]
	public float launchBoost = 20f;

	public float drag = 0.3f;
	
	[Header("Steering")]
	public float maxSteerAngle = 30;
	public float steerSpeed = 10f;
	[Tooltip("Force, in Gs, that the car can hold before it starts skidding")]
	public float maxCorneringGForce = 2f;
	public float tireSlip = 0.5f;
	public float driftBoost = 1f;
}
