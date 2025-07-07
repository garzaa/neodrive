using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Diagnostics;

public class Wheel : MonoBehaviour {
	Car car;
	CarSettings settings;
	RaycastHit raycastHit = new();
	RaycastHit[] hits;

	const int wheelRays = 16;
	readonly Vector3[] wheelCastRays = new Vector3[wheelRays];
	
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

	public bool reverseRotation;
	float fakeGroundBump;

	public TrailRenderer tireSkid;

	public GameObject normalSpeedObject;
	public GameObject highSpeedObject;

	MeshRenderer normalMesh;
	MeshRenderer speedMesh;

	float mph;
	float offset;

	void Awake() {
		car = GetComponentInParent<Car>();
		settings = car.settings;
		wheelMesh = normalSpeedObject.GetComponent<MeshFilter>().mesh;
		wheelRadius = 0.5f*(wheelMesh.bounds.size.x * normalSpeedObject.transform.localScale.x);
		groundedText = GetComponentInChildren<Text>();
		compressionBar = GetComponentsInChildren<Image>()[1];
		GenerateRays();
		normalMesh = normalSpeedObject.GetComponent<MeshRenderer>();
		speedMesh = highSpeedObject.GetComponent<MeshRenderer>();
		offset = transform.localPosition.magnitude;
	}

	void GenerateRays() {
		for (int i=0; i<16; i++) {
			float angle = Mathf.Deg2Rad * (i/(float)wheelRays * -180f);
			wheelCastRays[i] = new Vector3(
				0,
				Mathf.Sin(angle) * wheelRadius,
				Mathf.Cos(angle) * wheelRadius
			);
		}
	}

	public bool GetRaycast() {
		// get the nearest raycast out of the arc of wheel raycasts
		float minDist = float.MaxValue;
		bool hasHit = false;
		foreach (Vector3 rayOffset in wheelCastRays) {
			if (Physics.Raycast(
				new Ray(transform.position + rayOffset + transform.up*wheelRadius, -transform.up),
				out RaycastHit tempHit,
				settings.suspensionTravel + wheelRadius,
				settings.wheelRaycast
			))
			{
				hasHit = true;
				if (tempHit.distance < minDist)
				{
					minDist = tempHit.distance;
					raycastHit = tempHit;
				}
			}
		}
		return hasHit;
	}

	public void OnDrawGizmosSelected() {
		if (!Application.isPlaying) return;
        Gizmos.color = Color.green;
 
        //Draw the suspension
        Gizmos.DrawLine(
            transform.position - transform.up * wheelRadius, 
            transform.position - (transform.up * (wheelRadius + settings.suspensionTravel - suspensionCompression))
        );
 
        Vector3 point1;
        Vector3 point0 = transform.TransformPoint(wheelRadius * new Vector3(0, Mathf.Sin(0), Mathf.Cos(0)));
        for (int i = 1; i <= 20; ++i)
        {
            point1 = transform.TransformPoint(wheelRadius * new Vector3(0, Mathf.Sin(i / 20.0f * Mathf.PI * 2.0f), Mathf.Cos(i / 20.0f * Mathf.PI * 2.0f)));
            Gizmos.DrawLine(point0, point1);
            point0 = point1;
 
        }
        Gizmos.color = Color.white;

		Gizmos.color = Color.cyan;
		foreach (Vector3 r in wheelCastRays) {
			Gizmos.DrawRay(transform.position + r + transform.up*wheelRadius, -transform.up * (settings.suspensionTravel+wheelRadius));
		}
    }

	public Vector3 GetSuspensionForce() {
		bool hit = GetRaycast();

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
		UpdateTelemetry();
		return suspensionForce;
	}

	void UpdateTelemetry() {
		compressionBar.rectTransform.sizeDelta = new Vector2(
            compressionBar.rectTransform.sizeDelta.x,
            suspensionCompression / settings.suspensionTravel
        );
	}

	public void AddForce(Vector3 f) {
		car.rb.AddForceAtPosition(f, transform.position);
	}

	public void UpdateWheel(float flatSpeed, bool grounded, float idealRPM) {
		fakeGroundBump = 0;
		mph = Mathf.Abs(flatSpeed * Car.u2mph);

		// start wheel bumping at 20mph and peak at 60
		fakeGroundBump = Mathf.Clamp((flatSpeed-20f)/40f, 0, 1);
		fakeGroundBump *= Mathf.Sin((Time.time+offset) * 64f) * 0.005f;
		fakeGroundBump *= grounded ? 1 : 0;

		wheelObject.transform.position = transform.position - transform.up * (settings.suspensionTravel - suspensionCompression);
		wheelObject.transform.position += transform.up * fakeGroundBump;
		Vector3 v = wheelObject.transform.localRotation.eulerAngles;
		// get wheel position against the ground
		if (flatSpeed != 0) {
			float wheelCircumference = 2 * Mathf.PI * wheelRadius;
			float deg = 360f * (flatSpeed/wheelCircumference) * Time.fixedDeltaTime;
			print(rpm);
			v.z += deg * (reverseRotation ? -1 : 1);
			wheelObject.transform.localRotation = Quaternion.Euler(v);
		}

		bool highSpeed = mph > 50;
		normalMesh.enabled = !highSpeed;
		speedMesh.enabled = highSpeed;
	}
}
