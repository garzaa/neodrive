using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Wheel : MonoBehaviour {
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

	public Text groundedText;
	public Image compressionBar;

	public bool reverseRotation;
	float fakeGroundBump;

	public TrailRenderer tireSkid;

	public GameObject normalSpeedObject;
	public GameObject highSpeedObject;

	MeshRenderer normalMesh;
	MeshRenderer speedMesh;

	float offset;

	public TrailRenderer[] fireStreaks;
	public GameObject wheelFire;

	void Awake() {
		settings = GetComponentInParent<Car>()?.settings;
		if (settings == null) {
			settings = GetComponentInParent<GhostCar>().settings;
		}
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

	public float GetCompressionRatio() {
		return suspensionCompression / settings.suspensionTravel;
	}

	public void AddForce(Car car, Vector3 f) {
		car.rb.AddForceAtPosition(f, transform.position);
	}

	public void UpdateWheelVisuals(float flatSpeed, float rpm, bool boosting, bool drifting) {
		if (Time.timeScale == 0) return;
		fakeGroundBump = 0;

		// start wheel bumping at 20mph and peak at 60
		fakeGroundBump = Mathf.Clamp((flatSpeed-20f)/40f, 0, 1);
		fakeGroundBump *= Mathf.Sin((Time.time+offset) * 64f) * 0.005f;
		fakeGroundBump *= Grounded ? 1 : 0;

		wheelObject.transform.position = transform.position - transform.up * (settings.suspensionTravel - suspensionCompression);
		wheelObject.transform.position += transform.up * fakeGroundBump;
		Vector3 v = wheelObject.transform.localRotation.eulerAngles;

		// get wheel position against the ground
		float deg = rpm / 60f * Time.deltaTime * 360f;
		v.z += deg * (reverseRotation ? -1 : 1);
		wheelObject.transform.localRotation = Quaternion.Euler(v);

		bool highSpeed = rpm > 400;
		normalMesh.enabled = !highSpeed;
		speedMesh.enabled = highSpeed;

		foreach (TrailRenderer r in fireStreaks) {
			r.emitting = boosting;
		}

		// do not simplify this no matter how much vscode complains
		// unity's serialization screws with the null checks
		if (wheelFire != null) {
			wheelFire.SetActive(boosting);
		}

		tireSkid.emitting = Grounded && drifting;
	}

	public float GetWheelRPMFromSpeed(float flatSpeed) {
        return flatSpeed / (wheelRadius * 2f * Mathf.PI) * 60f;
    }
}
