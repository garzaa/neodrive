using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEditorInternal;
using System;
using Unity.VisualScripting;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Events;
using Cinemachine;

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
    public Text clutchText;

    Animator engineAnimator;
    public Animator exhaustAnimator;
    float engineRPM = 0f;
    float wheelRPM = 0f;
    float maxEngineVolume;
    public AudioSource engineAudio;
    public AudioSource gearshiftAudio;
    public AudioSource wheelAudio;
    public AudioSource tireSkid;

    public TrailRenderer[] tireSkids;

    List<RPMPoint> rpmPoints = new();

    public Text gearTelemetry;
    bool fuelCutoff = false;
    int currentGear = 0;
    bool engineStarting = false;
    bool engineRunning = false;
    bool engineStalling = false;
    
    bool changingGear = false;
    
    Camera mainCamera;

    public UnityEvent onGearChange;

    CarBody carBody;

    CinemachineImpulseSource impulseSource;

    bool drifting = false;
    bool clutch = false;
    bool clutchOut = false;

    float currentGrip = 1f;

    bool ignition { get {
        // to be changed later 
        return !(fuelCutoff || changingGear);
    }}

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
        carBody = GetComponentInChildren<CarBody>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        foreach (TrailRenderer t in tireSkids) {
            t.emitting = false;
        }
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
        clutch = InputManager.Button(Buttons.CLUTCH);
        if (InputManager.ButtonUp(Buttons.CLUTCH)) {
            clutchOut = true;
        }

        if (InputManager.ButtonUp(Buttons.CLUTCH) || InputManager.ButtonDown(Buttons.CLUTCH)) {
            gearshiftAudio.PlayOneShot(engine.clutchSound);
        }

        if (!changingGear) {
            if (InputManager.DoubleTap(Buttons.GEARDOWN) && clutch) {
                currentGear = -1;
            }
            if (InputManager.ButtonDown(Buttons.SHIFTDOWN) && clutch) {
                if (currentGear > -1) {
                    StartCoroutine(ChangeGear(currentGear - 1));
                }
            } else if (InputManager.ButtonDown(Buttons.SHIFTUP) && clutch) {
                if (currentGear < engine.gearRatios.Count) {
                    StartCoroutine(ChangeGear(currentGear+1));
                }
            }
        }
        
        if (InputManager.ButtonDown(Buttons.STARTENGINE) && clutch) {
            if (!engineRunning && !engineStarting) {
                StartCoroutine(StartEngine());
            } else {
                engineRunning = false;
            }
        }

        if (clutch) {
            if (InputManager.ButtonDown(Buttons.SHIFTLEFT)) {
            }
            if (InputManager.ButtonDown(Buttons.SHIFTRIGHT)) {
            }
            if (InputManager.ButtonDown(Buttons.SHIFTUP)) {
            }
            if (InputManager.ButtonDown(Buttons.SHIFTDOWN)) {
            }
        }
    }

    IEnumerator StartEngine() {
        engineStarting = true;
        engineAudio.PlayOneShot(engine.startupNoise);
        carBody.StartWobbling();
        yield return new WaitForSeconds(engine.startupNoise.length-0.5f);
        exhaustAnimator.SetTrigger("Backfire");
        carBody.StopWobbling();
        engineStarting = false;
        impulseSource.GenerateImpulseWithVelocity(impulseSource.m_DefaultVelocity * 1f);
        engineRunning = true;
    }

    void FixedUpdate() {
        Vector3 FLForce, FRForce, RLForce, RRForce;
        FLForce = WheelFL.GetSuspensionForce();
        FRForce = WheelFR.GetSuspensionForce();
        RLForce = WheelRL.GetSuspensionForce();
        RRForce = WheelRR.GetSuspensionForce();

        WheelFL.AddForce(FLForce);
        WheelFR.AddForce(FRForce);
        WheelRL.AddForce(RLForce);
        WheelRR.AddForce(RRForce);

        grounded = false;
        foreach (Wheel w in wheels) {
            if (w.Grounded) {
                grounded = true;
                w.UpdateWheel(Vector3.Dot(rb.velocity, forwardVector), grounded);
            }
        }

        Vector3 frontAxle = (WheelFL.transform.position + WheelFR.transform.position) / 2f;
        Vector3 rearAxle = (WheelRL.transform.position + WheelRR.transform.position) / 2f;

        if (WheelRR.Grounded || WheelRL.Grounded) {
            int mult = currentGear < 0 ? -1 : 1;
            if (gas > 0 && ignition && engineRunning && currentGear != 0 && !clutch) {
                rb.AddForceAtPosition(forwardVector * engine.GetTorque(engineRPM)*gas*mult, rearAxle);
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
        if (!drifting) currentGrip = Mathf.MoveTowards(currentGrip, 1f, 0.7f*Time.fixedDeltaTime);
        if (grounded) {
            if (WheelRL.Grounded || WheelRR.Grounded) {
                float lateralAccel = AddLateralForce(rearAxle, transform.right, true);
                float gs = lateralAccel / Mathf.Abs(Physics.gravity.y);
                gForceIndicator.rectTransform.localScale = new Vector3(gs, 1, 1);
                gForceText.text = Mathf.Abs(gs).ToString("F2") + " lateral G";
            }

            if (WheelFL.Grounded || WheelFR.Grounded) {
                // rotate the lateral for the front axle by the amount of steering
                AddLateralForce(frontAxle, Quaternion.Euler(0, currentSteerAngle, 0) * transform.right, false);
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

        float dragForce = 0.5f * rb.velocity.sqrMagnitude * settings.drag * 0.0005f;
        if (gas==0 ||  fuelCutoff) dragForce = 0;
        rb.AddForce(-rb.velocity*dragForce, ForceMode.Force);

        UpdateEngine();
        UpdateTelemetry();
        posLastFrame = rb.position;
        vLastFrame = rb.velocity;
        clutchOut = false;
    }

    void UpdateEngine() {
        float flatSpeed = Mathf.Abs(MPH(Vector3.Dot(rb.velocity, forwardVector))) * (currentGear >= 0 ? 1 : -1);
        if (!engineRunning) {
            engineRPM = 0;
            if (engineStarting) {
                engineRPM = 2400;
            }
        } else if (currentGear != 0 && !clutch) { 
            // TODO: this is wheelRPM, engineRPM is gonna be different
            // good news is that the clutch spearheads the transmission chain
            float currentEngineRPM = flatSpeed*Mathf.Sign(currentGear) * engine.diffRatio * engine.gearRatios[Mathf.Abs(currentGear)-1] / (WheelRL.wheelRadius * 2f * Mathf.PI) * 60f;
            if (clutchOut) {
                float rpmDiff = engineRPM - currentEngineRPM;
                if (rpmDiff < 0 && rpmDiff < 2000) {
                    print("power shift");
                    impulseSource.GenerateImpulse();
                } else if (rpmDiff > 0 && rpmDiff > 2000) {
                    print("unsynced downshift");
                    // TODO: lock up the front wheels or something on a non-revmatched downshift
                    impulseSource.GenerateImpulse();
                }
            }
            engineRPM = currentEngineRPM;
        } else if (currentGear == 0 || clutch) {
            float targetRPM = Mathf.Max(engine.idleRPM, ignition ? gas*engine.redline : 0);
        float moveSpeed = ignition ? engine.throttleResponse : (engine.engineBraking*(engineRPM/engine.redline)+2000f);
            engineRPM = Mathf.MoveTowards(engineRPM, targetRPM, moveSpeed * Time.fixedDeltaTime);
        }

        engineAnimator.speed = engineRPM == 0 ? 0 : 1 + (engineRPM/engine.redline);
        if (engineRPM > engine.redline-100) {
            fuelCutoff = true;
            // backfire more at lower gears when bouncing off the redline
            float ratio = Mathf.Max(1-(currentGear/engine.gearRatios.Count), 1);
            if (1/(ratio) * UnityEngine.Random.Range(0f, 1f) < 0.1f) exhaustAnimator.SetTrigger("Backfire");
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
        UpdateVibration();

        carBody.transform.localPosition = new Vector3(0, engineRPM/engine.redline * 0.025f, 0);
    }

    IEnumerator ChangeGear(int to) {
        changingGear = true;
        carBody.maxXAngle *= 2f;
        gearshiftAudio.PlayOneShot(engine.gearShiftNoises[UnityEngine.Random.Range(0, engine.gearShiftNoises.Count)]);
        yield return new WaitForSeconds(settings.gearShiftTime);

        currentGear = to;
        carBody.maxXAngle /= 2f;
        changingGear = false;
    }

    void StallEngine() {
        StartCoroutine(StallRock());
        engineAudio.PlayOneShot(engine.stallNoise);
        impulseSource.GenerateImpulseWithVelocity(impulseSource.m_DefaultVelocity * 3f);
        rb.AddForce(-Vector3.Project(rb.velocity, forwardVector)*0.8f / Time.fixedDeltaTime, ForceMode.Acceleration);
        engineRunning = false;
    }

    IEnumerator StallRock() {
        carBody.maxXAngle *= 4f;
        engineStalling = true;
        yield return new WaitForSeconds(0.3f);
        engineStalling = false;
        carBody.maxXAngle /= 4f;
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

    float AddLateralForce(Vector3 point, Vector3 lateralNormal, bool driftCheck) {
        float lateralSpeed = Vector3.Dot(rb.GetPointVelocity(point), lateralNormal);
        float wantedAccel = -lateralSpeed * GetTireSlip(lateralSpeed) * 0.5f / Time.fixedDeltaTime;
        float gs = Mathf.Abs(wantedAccel / Physics.gravity.y);
        if (driftCheck) {
            if (gs > settings.maxCorneringGForce) {
                // the car should start either skidding or TCS here
                drifting = true;
                tireSkid.mute = false;
                foreach (TrailRenderer t in tireSkids) {
                    t.emitting = true && grounded;
                }
            } else {
                tireSkid.mute = true;
                drifting = false;
                foreach (TrailRenderer t in tireSkids) {
                    t.emitting = false;
                }
            }
        }
        if (drifting) {
            carBody.driftRoll = 5f;
            // instantly break traction, but ease back into it to avoid overcorrecting
            currentGrip = 0.5f / (gs / settings.maxCorneringGForce);
        } else {
            carBody.driftRoll = 0f;
        }
        wantedAccel *= currentGrip;
        rb.AddForceAtPosition(lateralNormal * wantedAccel, point, ForceMode.Acceleration);
        return wantedAccel;
    }

    float GetTireSlip(float lateralSpeed) {
        // this is flat right now, might want to make it a curve later
        return settings.tireSlip;
    }

    void UpdateTelemetry() {
        speedText.text = MPH(rb.velocity.magnitude).ToString("F0");
        rpmText.text = engineRPM.ToString("F0");
        if (currentGear > 0) {
            gearTelemetry.text = currentGear.ToString();
        } else if (currentGear == 0) {
            gearTelemetry.text = "N";
        } else {
            gearTelemetry.text = "R";
        }
        clutchText.color = new Color(1, 1, 1, clutch ? 1 : 0.2f);
    }

    void UpdateSteering() {
        // TODO: decrease this if the car is moving at speed - define a start to ramp-down and an end
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

    void UpdateVibration() {
        float vibrationAmount = 0;
        float rpmVibration = 0;
        if (engineRPM > engine.redline - 2000) {
            rpmVibration += ((engineRPM - (engine.redline-2000)) / 2000)*0.5f;
        }
        if (engineStarting || engineStalling) {
            vibrationAmount = 1f;
        }
        InputManager.player.SetVibration(0, vibrationAmount);
        InputManager.player.SetVibration(1, vibrationAmount+rpmVibration);
        
    }

    public static float MPH(float speed) {
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
        lowTarget.throttleOffAudio.volume *= 1-gas * 0.5f * (ignition ? 1f : 0.2f);
        highTarget.throttleOffAudio.volume *= 1-gas * 0.5f * (ignition ? 1f : 0.2f);

        // then warp the sound to match the RPM
        lowTarget.throttleAudio.pitch = targetLowPitch;
        lowTarget.throttleOffAudio.pitch = targetLowPitch;
        highTarget.throttleAudio.pitch = targetHighPitch;
        highTarget.throttleOffAudio.pitch = targetHighPitch;
    }
}
