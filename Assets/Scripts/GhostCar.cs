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

	void Start() {
		engineAudio = GetComponent<EngineAudio>();
		engineAudio.BuildSoundCache(engineSettings, engineAudioSource);
		wheels = new Wheel[]{WheelFL, WheelFR, WheelRL, WheelRR};

		foreach (Wheel w in wheels) {
			w.UpdateWheelVisuals(0, 0, false, false);
		}

		engineAudio.SetRPMAudio(0, 0, true);
	}

	public void ApplySnapshot(CarSnapshot snapshot) {
		flatSpeed = Vector3.Dot(transform.position - positionLastUpdate, transform.forward) / Time.deltaTime;
		transform.position = snapshot.position;
		transform.rotation = snapshot.rotation;
		engineAudio.SetRPMAudio(snapshot.rpm, snapshot.gas, false);

		foreach (Wheel w in wheels) {
			w.UpdateWheelVisuals(flatSpeed, w.GetWheelRPMFromSpeed(flatSpeed), false, snapshot.drifting);
		}

		steerRotation = Quaternion.Euler(0, snapshot.steerAngle, 0);
        WheelFL.transform.localRotation = steerRotation;
        WheelFR.transform.localRotation = steerRotation;
	}
}
