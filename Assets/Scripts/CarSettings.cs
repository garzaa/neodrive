using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Data/CarSettings")]
public class CarSettings : ScriptableObject {
	public float suspensionTravel;
	public float springStrength = 15000f;
	public float springDamper = 1000f;
	public LayerMask wheelRaycast;
	[Tooltip("The sideways force where wheels break into a skid")]
	public float lateralSkidThreshold = 10;

	// this will deal with torque curves and shit later on naturally
	public float accelForce = 4f;
	public float brakeForce = 5f;

	[Tooltip("Max speed, in MPH, that the car can go")]
	public int maxSpeed = 10;

	[Header("Steering")]
	public float maxSteerAngle = 30;
	public float steerSpeed = 10f;
	public float maxAngularVelocity = 20f;
	public float steeringMult = 0.1f;
	[Header("Force, in Gs, that the car can hold before it starts skidding")]
	public float maxCorneringForce = 1.5f;
}
