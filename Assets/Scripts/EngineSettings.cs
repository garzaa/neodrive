using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class RPMPoint {
    public float rpm;
    public AudioClip throttle;
    public AudioClip throttleOff;
	public AudioSource throttleAudio;
	public AudioSource throttleOffAudio;

    public RPMPoint(float rpm, AudioClip throttle, AudioClip throttleOff) {
        this.rpm = rpm;
        this.throttle = throttle;
        this.throttleOff = throttleOff;
    }

	public RPMPoint(RPMPoint r, AudioSource throttleAudio, AudioSource throttleOffAudio) {
		this.rpm = r.rpm;
		this.throttle = r.throttle;
		this.throttleOff = r.throttleOff;
		this.throttleAudio = throttleAudio;
		this.throttleOffAudio = throttleOffAudio;
	}
}

[System.Serializable]
public class GearSound {
	[Tooltip("Volume as a fraction of redline")]
	public AnimationCurve volumeCurve;
	[Tooltip("Keep pitch neutral at this speed")]
	public float baseSpeed;
	[Range(0.1f, 1)]
	public float toneShiftAmount;
	public AudioClip gearLoop;
	public float baseVolume = 0.4f;
	public float tremoloVolumeRange = 0.25f;
}

[CreateAssetMenu(menuName = "Data/EngineSettings")]
public class EngineSettings : ScriptableObject {
	public int redline = 8000;
	public float fuelCutoff = 0.6f;
	public float engineBraking = 2f;
	public float diffRatio = 4f;

	public AnimationCurve powerCurve;
	public float maxPower = 300;
	public AudioClip startupNoise;
	public AudioClip stallNoise;
	public float idleRPM = 1800;
	public float stallRPM = 1200;
	public float throttleResponse = 6000f;
	public List<RPMPoint> rpmPoints;
	public List<float> gearRatios;
	public GearSound gearSound;
	public AudioClip clutchSound;
	[Range(0.1f, 3f)]
	public float clutchSharpness = 0.7f;
	public List<AudioClip> gearShiftNoises;

	public float GetPower(float rpm) {
		return powerCurve.Evaluate(rpm / redline) * maxPower;
	}

	public bool PeakPower(float rpm) {
		return maxPower - GetPower(rpm) < (maxPower*0.1);
	}

	public float GetThrottleResponse(float rpm) {
		return 1/((rpm + 2000)/redline) * throttleResponse;
	}
}
