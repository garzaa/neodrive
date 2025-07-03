using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEditorInternal;
using System;
using Unity.VisualScripting;

public class Car : MonoBehaviour {

    public GameObject wheelTemplate;
    public Wheel WheelFL, WheelFR, WheelRL, WheelRR;
    public CarSettings settings;
    public EngineSettings engine;
    public GameObject centerOfGravity;

    public float gas;
    public float brake;
    public float steering;

    Vector3 forceCenter;
    Vector3 vLastFrame;
    Vector3 posLastFrame;

    public Rigidbody rb { get; private set; }
    public bool grounded { get; private set; }
    Wheel[] wheels;

    public Text speedText;
    public float currentSteerAngle;
    public Image gForceIndicator;
    public Text gForceText;
    public Text rpmText;

    Animator engineAnimator;
    float engineRPM = 0f;
    float maxEngineVolume;
    public AudioSource engineAudio;
    public AudioSource gearshiftAudio;
    public AudioSource wheelAudio;

    List<RPMPoint> rpmPoints = new();

    public Text audioTelemetry;
    bool fuelCutoff = false;
    int currentGear = 0;
    bool engineStarting = false;
    bool engineRunning = false;
    
    float engineChangeVelocity;
    
    Camera mainCamera;

    Vector3 forwardVector { get {
        // because I modeled it facing the wrong way
        return -transform.forward;
    }}

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfGravity.transform.localPosition;
        wheels = new Wheel[]{WheelFL, WheelFR, WheelRL, WheelRR};
        engineAnimator = GetComponent<Animator>();
        BuildSoundCache();
        mainCamera = Camera.main;
    }

    void BuildSoundCache() {
        maxEngineVolume = engineAudio.volume;
        foreach (RPMPoint r in engine.rpmPoints) {
            AudioSource rAudio = engineAudio.AddComponent<AudioSource>();
            rAudio.volume = maxEngineVolume;
            rAudio.outputAudioMixerGroup = engineAudio.outputAudioMixerGroup;
            rAudio.clip = r.throttle;
            rAudio.loop = true;
            rAudio.spatialBlend = 1;
            rAudio.Play();
            AudioSource rOffAudio = engineAudio.AddComponent<AudioSource>();
            rOffAudio.volume = maxEngineVolume;
            rOffAudio.outputAudioMixerGroup = engineAudio.outputAudioMixerGroup;
            rOffAudio.clip = r.throttleOff;
            rOffAudio.loop = true;
            rOffAudio.spatialBlend = 1;
            rOffAudio.Play();

            rpmPoints.Add(new RPMPoint(r, rAudio, rOffAudio));
        }
    }

    void Update() {
        gas = InputManager.GetAxis(Buttons.GAS);
        brake = InputManager.GetAxis(Buttons.BRAKE);
        steering = InputManager.GetAxis(Buttons.STEER);
        if (InputManager.DoubleTap(Buttons.GEARDOWN)) {
            currentGear = -1;
        }
        if (InputManager.ButtonDown(Buttons.GEARDOWN)) {
            if (currentGear > -1) {
                // TODO: fix the reverse things
                // speed should be absoluted or inverted in certain points (i.e. engineRPM)
                currentGear--;
                gearshiftAudio.PlayOneShot(engine.gearShiftNoises[UnityEngine.Random.Range(0, engine.gearShiftNoises.Count)]);
            }
        } else if (InputManager.ButtonDown(Buttons.GEARUP)) {
            if (currentGear < engine.gearRatios.Count) {
                currentGear++;
                gearshiftAudio.PlayOneShot(engine.gearShiftNoises[UnityEngine.Random.Range(0, engine.gearShiftNoises.Count)]);
            }
        }
        
        if (InputManager.ButtonDown(Buttons.STARTENGINE)) {
            if (!engineRunning && !engineStarting) {
                StartCoroutine(StartEngine());
            } else {
                engineRunning = false;
            }
        }
    }

    IEnumerator StartEngine() {
        engineStarting = true;
        engineAudio.PlayOneShot(engine.startupNoise);
        yield return new WaitForSeconds(engine.startupNoise.length-0.2f);
        engineStarting = false;
        engineRunning = true;
    }

    void FixedUpdate() {
        grounded = false;
        foreach (Wheel w in wheels) {
            if (w.Grounded) {
                grounded = true;
            }
        }

        // if wheel upwards force is similar, add force at the center of mass
        Vector3 FLForce, FRForce, RLForce, RRForce;
        FLForce = WheelFL.GetSuspensionForce();
        FRForce = WheelFR.GetSuspensionForce();
        RLForce = WheelRL.GetSuspensionForce();
        RRForce = WheelRR.GetSuspensionForce();

        WheelFL.AddForce(FLForce);
        WheelFR.AddForce(FRForce);
        WheelRL.AddForce(RLForce);
        WheelRR.AddForce(RRForce);

        Vector3 frontAxle = (WheelFL.transform.position + WheelFR.transform.position) / 2f;
        Vector3 rearAxle = (WheelRL.transform.position + WheelRR.transform.position) / 2f;

        if (WheelRR.Grounded || WheelRL.Grounded) {
            int mult = currentGear < 0 ? -1 : 1;
            Vector3 flatSpeed = Vector3.Project(rb.velocity, forwardVector);
            if (gas > 0 && !fuelCutoff && engineRunning && currentGear != 0) {
                if (Mathf.Abs(MPH(flatSpeed.magnitude)) < settings.maxSpeed) {
                    rb.AddForceAtPosition(forwardVector * settings.accelForce*gas*mult, rearAxle);
                }
            } else {
                rb.AddForce(-Vector3.Project(rb.velocity, forwardVector) * (engineRPM/engine.redline) * engine.engineBraking);
            }
        }

        if (brake > 0 && grounded) {
            Vector3 flatSpeed = Vector3.Project(rb.velocity, forwardVector);
            if (MPH(flatSpeed.magnitude) < 1) {
                rb.velocity -= flatSpeed;
            } else {
                rb.AddForce(-flatSpeed.normalized * brake * settings.brakeForce);
            }
        }

        UpdateSteering();
        if (grounded) {
            if (WheelFL.Grounded || WheelFR.Grounded) {
                // rotate the lateral for the front axle by the amount of steering
                AddLateralForce(frontAxle, Quaternion.Euler(0, currentSteerAngle, 0) * transform.right);
            }

            if (WheelRL.Grounded || WheelRR.Grounded) {
                float lateralAccel = AddLateralForce(rearAxle, transform.right);
                float gs = lateralAccel / Mathf.Abs(Physics.gravity.y);
                gForceIndicator.rectTransform.localScale = new Vector3(gs, 1, 1);
                gForceText.text = Mathf.Abs(gs).ToString("F2") + " lateral G";
            }

            float flatSpeed = MPH(Mathf.Abs(Vector3.Dot(rb.velocity, forwardVector)));
            if (flatSpeed < 0.2f) {
                wheelAudio.volume = 0;
            } else {
                wheelAudio.volume = 0.5f;
                wheelAudio.pitch = Mathf.Lerp(1, 3f, flatSpeed / 80f);
            }
        } else {
            wheelAudio.volume = 0;
        }
        UpdateEngine();
        UpdateTelemetry();
        posLastFrame = rb.position;
        vLastFrame = rb.velocity;
    }

    void UpdateEngine() {
        float flatSpeed = Mathf.Abs(MPH(Vector3.Dot(rb.velocity, forwardVector))) * Mathf.Sign(currentGear);
        if (!engineRunning) {
            engineRPM = 0;
            if (engineStarting) {
                engineRPM = 2400;
            }
        } else if (currentGear != 0) {
            engineRPM = flatSpeed * engine.diffRatio * engine.gearRatios[Mathf.Abs(currentGear)-1] / (WheelRL.wheelRadius * 2f * Mathf.PI) * 60f;
        } else if (currentGear == 0) {
            float targetRPM = Mathf.Max(engine.idleRPM, fuelCutoff ? 0 : gas*engine.redline);
            engineRPM = Mathf.MoveTowards(engineRPM, targetRPM, engine.throttleResponse * Time.fixedDeltaTime);
        }

        engineAnimator.speed = engineRPM == 0 ? 0 : 1 + (engineRPM/engine.redline);
        if (engineRPM > engine.redline-100) {
            fuelCutoff = true;
            rb.AddForce(-Vector3.Project(rb.velocity, forwardVector));
        } else {
            fuelCutoff = false;
        }

        // anti-stall check before I write the clutch
        if (engineRunning && engineRPM < engine.stallRPM && (gas < 0.5f || currentGear > 1)) {
            StallEngine();
        } else if (engineRPM > engine.redline+500) {
            // money shift
            StallEngine();
        }

        GetRPMPoint(engineRPM, gas);
        UpdateEngineLowPass();
    }

    void StallEngine() {
        engineAudio.PlayOneShot(engine.stallNoise);
        rb.AddForce(-Vector3.Project(rb.velocity, forwardVector)*0.8f / Time.fixedDeltaTime, ForceMode.Acceleration);
        engineRunning = false;
    }

    void UpdateEngineLowPass() {
        Vector3 towardsCamera = mainCamera.transform.position - engineAudio.transform.position;
        float cameraEngineAngle = Vector3.Angle(forwardVector, Vector3.ProjectOnPlane(towardsCamera, transform.up));
        if (cameraEngineAngle < 90) {
            engineAudio.outputAudioMixerGroup.audioMixer.SetFloat("ExhaustLowPassCutoff", 5000);
        } else {
            cameraEngineAngle -= 90f;
            engineAudio.outputAudioMixerGroup.audioMixer.SetFloat("ExhaustLowPassCutoff", Mathf.Lerp(5000, 22000, cameraEngineAngle/90f));
        }
    }

    float AddLateralForce(Vector3 point, Vector3 lateralNormal) {
        float lateralSpeed = Vector3.Dot(rb.GetPointVelocity(point), lateralNormal);
        float lateralAccel = -lateralSpeed * GetTireSlip(lateralSpeed) * 0.5f / Time.fixedDeltaTime;
        rb.AddForceAtPosition(lateralNormal * lateralAccel, point, ForceMode.Acceleration);
        return lateralAccel;
    }

    float GetTireSlip(float lateralSpeed) {
        // fill this in later
        return settings.tireSlip;
    }

    void UpdateTelemetry() {
        speedText.text = MPH(rb.velocity.magnitude).ToString("F0");
        rpmText.text = engineRPM.ToString("F0");
        // zero-indexing
        audioTelemetry.text = currentGear.ToString();
    }

    void UpdateSteering() {
        float targetSteerAngle = Mathf.Abs(steering * settings.maxSteerAngle) * Mathf.Sin(steering);
        if (steering == 0) {
            targetSteerAngle = 0;
        }
        float steerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteerAngle, settings.steerSpeed);

        Quaternion targetRotation = Quaternion.Euler(0, steerAngle, 0);
        WheelFL.transform.localRotation = targetRotation;
        WheelFR.transform.localRotation = targetRotation;
        currentSteerAngle = steerAngle;
    }

    float MPH(float speed) {
        return speed * 2.2369362912f;
    }

    void GetRPMPoint(float rpm, float gas) {
        // profile and optimize this later
        // jesus christ, you need to have an audiosource for every single RPM point
        RPMPoint lowTarget = rpmPoints[0];
        RPMPoint highTarget = rpmPoints[0];
        for (int i=1; i<rpmPoints.Count-1; i++) {
            RPMPoint current = rpmPoints[i];
            RPMPoint lower = rpmPoints[i-1];
            RPMPoint higher = rpmPoints[i+1];

            // in the one-in-a-million scenario where the RPM exactly matches up
            if (rpm == current.rpm) {
                lowTarget = current;
                highTarget = current;
                break;
            }

            // if at the start, and it's just the lower rpm, return the lowest RPM
            if (i==1 && rpm < lower.rpm) {
                lowTarget = lower;
                highTarget = lower;
                break;
            }
            // same for the end
            if (i == engine.rpmPoints.Count-2 && rpm > higher.rpm) {
                lowTarget = higher;
                highTarget = higher;
                break;
            }

            if (rpm > lower.rpm && rpm < current.rpm) {
                lowTarget = lower;
                highTarget = current;
            } else if (rpm > current.rpm && rpm < higher.rpm) {
                lowTarget = current;
                highTarget = higher;
            }
        }

        for (int i=0; i<rpmPoints.Count; i++) {
            rpmPoints[i].throttleAudio.volume = 0;
            rpmPoints[i].throttleOffAudio.volume = 0;
        }

        if (!engineRunning) {
            return;
        }

        // set the volume for low and high targets based on RPM
        float rpmRatio = (rpm - lowTarget.rpm) / (highTarget.rpm - lowTarget.rpm);
        float lowVolume = maxEngineVolume * (1-rpmRatio);
        float highVolume = maxEngineVolume * rpmRatio;
        float targetLowPitch = rpm / lowTarget.rpm;
        float targetHighPitch = rpm / highTarget.rpm;

        if (lowTarget == highTarget) {
            lowTarget.throttleAudio.volume = 1;
            lowTarget.throttleOffAudio.volume = 1;
            highTarget.throttleAudio.volume = 1;
            highTarget.throttleOffAudio.volume = 1;
            lowTarget.throttleAudio.volume *= gas;
            lowTarget.throttleOffAudio.volume *= 1-gas;
            lowTarget.throttleAudio.pitch = targetLowPitch;
            lowTarget.throttleOffAudio.pitch = targetLowPitch;
            return;
        }

        lowTarget.throttleAudio.volume = lowVolume;
        lowTarget.throttleOffAudio.volume = lowVolume;
        highTarget.throttleAudio.volume = highVolume;
        highTarget.throttleOffAudio.volume = highVolume;
        // then lerp between each one based on gas
        lowTarget.throttleAudio.volume *= gas;
        highTarget.throttleAudio.volume *= gas;
        lowTarget.throttleOffAudio.volume *= 1-gas;
        highTarget.throttleOffAudio.volume *= 1-gas;

        // then warp the sound to match the RPM
        lowTarget.throttleAudio.pitch = targetLowPitch;
        lowTarget.throttleOffAudio.pitch = targetLowPitch;
        highTarget.throttleAudio.pitch = targetHighPitch;
        highTarget.throttleOffAudio.pitch = targetHighPitch;
    }
}
