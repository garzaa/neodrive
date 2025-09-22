using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(EngineAudio))]
public class GhostCar : MonoBehaviour {
	public Wheel WheelFL, WheelFR, WheelRL, WheelRR;
	public AudioSource engineAudioSource;

	public EngineSettings engineSettings;
	public CarSettings settings;

	EngineAudio engineAudio;
	Wheel[] wheels;

	float flatSpeed;
	Vector3 positionLastUpdate;
	Quaternion steerRotation;

	public GameObject boostEffect;

	MaterialPropertyBlock shaderBlock;
    MeshRenderer carMesh;

	void Start() {
		engineAudio = GetComponent<EngineAudio>();
		engineAudio.BuildSoundCache(engineSettings, engineAudioSource, bigSteps: true);
		wheels = new Wheel[]{WheelFL, WheelFR, WheelRL, WheelRR};

		foreach (Wheel w in wheels) {
			w.UpdateWheelVisuals(0, 0, false, false, 0);
		}

		boostEffect.SetActive(false);

		engineAudio.SetRPMAudio(0, 0, true, false);

		shaderBlock = new();
		carMesh = transform.Find("BodyMesh/CarBase/Body").GetComponent<MeshRenderer>();
        carMesh.GetPropertyBlock(shaderBlock);
	}

	public void ApplySnapshot(CarSnapshot snapshot) {
		flatSpeed = Vector3.Dot(transform.position - positionLastUpdate, transform.forward) / Time.deltaTime;
		transform.SetPositionAndRotation(snapshot.position, snapshot.rotation);
		engineAudio.SetRPMAudio(snapshot.rpm, snapshot.gas, false, false);

		foreach (Wheel w in wheels) {
			bool wheelBoost = snapshot.boosting && (w==WheelRR || w==WheelRL);
			w.UpdateWheelVisuals(flatSpeed, w.GetWheelRPMFromSpeed(flatSpeed), wheelBoost, snapshot.drifting, 0);
		}

		boostEffect.SetActive(snapshot.boosting);

		steerRotation = Quaternion.Euler(0, snapshot.steerAngle, 0);
        WheelFL.transform.localRotation = steerRotation;
        WheelFR.transform.localRotation = steerRotation;
		carMesh.GetPropertyBlock(shaderBlock);
        shaderBlock.SetColor("_Emissive_Color", snapshot.brake ? Color.white : Color.black);
        carMesh.SetPropertyBlock(shaderBlock);
	}
	
	void OnDisable() {
		foreach (TrailRenderer t in GetComponentsInChildren<TrailRenderer>()) {
			if (t) t.Clear();
		}
	}
}
