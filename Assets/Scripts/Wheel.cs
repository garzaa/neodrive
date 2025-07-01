using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class Wheel : MonoBehaviour {
	Car car;
	CarSettings settings;
	RaycastHit raycastHit = new();
	RaycastHit[] hits;
	
	Mesh wheelMesh;
	public GameObject wheelObject;

	public bool Grounded;

	public float wheelRadius;
	public float suspensionCompression;
	public float suspensionCompressionLastStep;
	public Vector3 suspensionForce;

	const float springTarget = 0;

	public float rpm;

	public Text groundedText;
	public Image compressionBar;

	Color c;

	public bool reverseRotation;

	Rigidbody wheelRB;

	void Awake() {
		car = GetComponentInParent<Car>();
		settings = car.settings;
		wheelMesh = wheelObject.GetComponent<MeshFilter>().mesh;
		wheelRadius = 0.5f*(wheelMesh.bounds.size.x * wheelObject.transform.localScale.x);
		groundedText = GetComponentInChildren<Text>();
		compressionBar = GetComponentsInChildren<Image>()[1];
		wheelRB = GetComponent<Rigidbody>();
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
		Gizmos.DrawWireSphere(transform.position, wheelRadius);
		Gizmos.DrawWireSphere(transform.position - (transform.up * settings.suspensionTravel) - (wheelRadius*transform.up), wheelRadius);
        Gizmos.color = Color.white;
    }

	public Vector3 GetSuspensionForce() {
		// TODO: if you really want a circle, just gotta generate a bunch of 
		// rays at the start and use those. like a big semicircle on the bottom
		// of the wheel. PAIN AND SUFFERING.
		// they should go up from the center, nbot move the cente down
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
		} else {
			Grounded = false;
			suspensionCompression = 0;
		}

		suspensionForce = transform.up * (suspensionCompression - settings.suspensionTravel * springTarget) * settings.springStrength;
		suspensionForce += transform.up * (suspensionCompression - suspensionCompressionLastStep) / Time.fixedDeltaTime * settings.springDamper;

		suspensionCompressionLastStep = suspensionCompression;
		UpdateWheel();
		UpdateTelemetry();
		return suspensionForce;
	}

	void UpdateTelemetry() {
        c = groundedText.color;
        c.a = Grounded ? 1 : 0.1f;
        groundedText.color = c;

		compressionBar.rectTransform.sizeDelta = new Vector2(
            compressionBar.rectTransform.sizeDelta.x,
            (suspensionCompression / (settings.suspensionTravel))
        );
	}

	public void AddForce(Vector3 f) {
		car.rb.AddForceAtPosition(f, transform.position);
	}

	void UpdateWheel() {
		wheelObject.transform.position = transform.position - transform.up * (settings.suspensionTravel - suspensionCompression);
		Vector3 v = wheelObject.transform.localRotation.eulerAngles;
		// get wheel position against the ground
		float forwardsVelocity = Vector3.Dot(car.rb.velocity, -transform.forward);
		if (forwardsVelocity != 0) {
			float wheelCircumference = 2 * Mathf.PI * wheelRadius;
			float deg = 360f * (forwardsVelocity/wheelCircumference) * Time.fixedDeltaTime;
			v.z += deg * (reverseRotation ? -1 : 1);
			wheelObject.transform.localRotation = Quaternion.Euler(v);
		}
	}
}
