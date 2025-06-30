using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Wheel : MonoBehaviour {
	Car car;
	Rigidbody carRB;
	CarSettings settings;
	RaycastHit raycastHit = new();
	
	Mesh wheelMesh;
	public GameObject wheelObject;

	public bool Grounded;

	float wheelRadius;
	public float suspensionCompression;
	public float suspensionCompressionLastStep;
	public Vector3 suspensionForce;

	const float springTarget = 0;

	void Awake() {
		car = GetComponentInParent<Car>();
		carRB = car.GetComponent<Rigidbody>();
		settings = car.settings;
		wheelMesh = wheelObject.GetComponent<MeshFilter>().mesh;
		wheelRadius = 0.5f*(wheelMesh.bounds.size.x * wheelObject.transform.localScale.x);
	}

	void FixedUpdate() {
		UpdateSuspension();
		UpdateWheel();

		if (Grounded) {
			carRB.AddForceAtPosition(suspensionForce, transform.position);
		}
	}

	public void OnDrawGizmosSelected() {
		if (!Application.isPlaying) return;
        Gizmos.color = Color.green;
 
        //Draw the suspension
        Gizmos.DrawLine(
            transform.position - transform.up * wheelRadius, 
            transform.position - (transform.up * (wheelRadius + settings.suspensionTravel - suspensionCompression))
        );
 
        //Draw the wheel
        Vector3 point1;
        Vector3 point0 = transform.TransformPoint(wheelRadius * new Vector3(0, Mathf.Sin(0), Mathf.Cos(0)));
        for (int i = 1; i <= 20; ++i)
        {
            point1 = transform.TransformPoint(wheelRadius * new Vector3(0, Mathf.Sin(i / 20.0f * Mathf.PI * 2.0f), Mathf.Cos(i / 20.0f * Mathf.PI * 2.0f)));
            Gizmos.DrawLine(point0, point1);
            point0 = point1;
 
        }
        Gizmos.color = Color.white;
    }

	void UpdateSuspension() {
		bool hit = Physics.Raycast(
			new Ray(transform.position, -transform.up),
			out raycastHit,
			settings.suspensionTravel + wheelRadius,
			settings.wheelRaycast
		);

		if (hit) {
			Grounded = true;
			suspensionCompression = settings.suspensionTravel
				+ wheelRadius
				- (raycastHit.point - transform.position).magnitude;
			// spring force
			// 0.1 - 0.25 * 0
			suspensionForce = transform.up * (suspensionCompression - settings.suspensionTravel * springTarget) * settings.springStrength;
			// damping force
			suspensionForce += transform.up * (suspensionCompression - suspensionCompressionLastStep) / Time.fixedDeltaTime * settings.springDamper;
		} else {
			suspensionCompression = 0;
			Grounded = false;
		}

		suspensionCompressionLastStep = suspensionCompression;
	}

	void UpdateWheel() {
		wheelObject.transform.position = transform.position - Vector3.up * (settings.suspensionTravel - suspensionCompression);
	}
}
