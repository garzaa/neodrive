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
using Palmmedia.ReportGenerator.Core;

public class Car : MonoBehaviour {

    public GameObject wheelTemplate;
    public Wheel WheelFL, WheelFR, WheelRL, WheelRR;
    public CarSettings settings;
    public EngineSettings engine;
    public GameObject centerOfGravity;

    public float gas { get; private set; }
    public float brake;
    public float steering;

    public Rigidbody rb { get; private set; }
    public bool grounded { get; private set; }
    Wheel[] wheels;

    public Text speedText;
    public float currentSteerAngle { get; private set; }
    public Image gForceIndicator;
    public Text gForceText;
    public Text clutchText;

    Animator engineAnimator;
    public Animator exhaustAnimator;
    float engineRPM = 0f;
    float maxEngineVolume;
    public AudioSource engineAudio;
    public AudioSource gearshiftAudio;
    public AudioSource wheelAudio;
    public AudioSource tireSkid;

    public TrailRenderer[] tireSkids;

    readonly List<RPMPoint> rpmPoints = new();

    public Text gearTelemetry;
    bool fuelCutoff = false;
    int currentGear = 0;
    bool engineStarting = false;
    bool engineRunning = false;
    bool engineStalling = false;
    
    Camera mainCamera;

    public UnityEvent onGearChange;

    CarBody carBody;

    CinemachineImpulseSource impulseSource;

    bool drifting = false;
    bool clutch = false;
    bool clutchOutThisFrame = false;
    public float clutchRatio = 1f;

    float currentGrip = 1f;

    public const float u2mph = 2.2369362912f;
    public const float mph2u = 1/u2mph;

    public Tachometer tachometer;
    public Speedometer speedometer;
    public Animator perfectShiftEffect;
    public Animator alertAnimator;
    Text alertText;

    public AudioSource perfectShiftAudio;
    public int lastGear;

    public bool Drifting {
        get {
            return drifting;
        }
    }

    public Vector3 forwardVector { get {
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
        alertText = alertAnimator.GetComponentInChildren<Text>();
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
            clutchOutThisFrame = true;
        }

        if (InputManager.ButtonUp(Buttons.CLUTCH) || InputManager.ButtonDown(Buttons.CLUTCH)) {
            gearshiftAudio.PlayOneShot(engine.clutchSound);
        }

        if (InputManager.ButtonDown(Buttons.CLUTCH)) {
            lastGear = currentGear;
        }

        if (InputManager.DoubleTap(Buttons.GEARDOWN) && clutch) {
            currentGear = -1;
        }
        if (InputManager.ButtonDown(Buttons.SHIFTDOWN) && clutch) {
            if (currentGear > -1) {
                ChangeGear(currentGear - 1);
            }
        } else if (InputManager.ButtonDown(Buttons.SHIFTUP) && clutch) {
            if (currentGear < engine.gearRatios.Count) {
                ChangeGear(currentGear+1);
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
            }
            w.UpdateWheel(Vector3.Dot(rb.GetPointVelocity(w.transform.position), forwardVector), grounded);
        }

        Vector3 frontAxle = (WheelFL.transform.position + WheelFR.transform.position) / 2f;
        Vector3 rearAxle = (WheelRL.transform.position + WheelRR.transform.position) / 2f;

        if (WheelRR.Grounded || WheelRL.Grounded) {
            if (gas > 0 && !fuelCutoff && engineRunning && currentGear != 0 && !clutch) {
                float mult = currentGear < 0 ? -1 : 1;
                mult *= drifting ? settings.driftBoost : 1;
                rb.AddForceAtPosition(forwardVector * engine.GetPower(engineRPM)*gas*mult, rearAxle);
            } else {
                rb.AddForce(-Vector3.Project(rb.velocity, forwardVector) * (engineRPM/engine.redline) * engine.engineBraking);
            }
        }

        if (brake > 0 && grounded) {
            Vector3 flatSpeed = Vector3.Project(rb.velocity, forwardVector);
            if ((flatSpeed.magnitude*u2mph) < 1) {
                rb.velocity -= flatSpeed;
            } else {
                rb.AddForce(-flatSpeed.normalized * brake * settings.brakeForce);
            }
        }

        UpdateSteering();
        if (!drifting) {
            // return to max grip after a drift
            currentGrip = Mathf.MoveTowards(currentGrip, 1f, 0.7f*Time.fixedDeltaTime);
        }
        if (grounded) {
            if (WheelRL.Grounded || WheelRR.Grounded) {
                AddLateralForce(rearAxle, transform.right, false);
            }

            if (WheelFL.Grounded || WheelFR.Grounded) {
                // rotate the lateral for the front axle by the amount of steering
                AddLateralForce(frontAxle, Quaternion.Euler(0, currentSteerAngle, 0) * transform.right, true);
            }

            float flatSpeed = Mathf.Abs(Vector3.Dot(rb.velocity, forwardVector)) * u2mph;
            if (flatSpeed < 0.2f) {
                wheelAudio.volume = 0;
            } else {
                wheelAudio.volume = 0.5f;
                wheelAudio.pitch = Mathf.Lerp(1, 3f, flatSpeed / 80f);
            }
        } else {
            wheelAudio.volume = 0;
        }

        float dragForce = 0.5f * rb.velocity.sqrMagnitude * settings.drag * 0.005f;
        if (gas==0 ||  fuelCutoff) dragForce = 0;
        rb.AddForce(-rb.velocity*dragForce, ForceMode.Force);

        UpdateEngine();
        UpdateTelemetry();
        clutchOutThisFrame = false;

        foreach (Wheel w in wheels) {
            if (!w.Grounded) w.tireSkid.emitting = false;
        }
    }

    void UpdateEngine() {
        float flatSpeed = Mathf.Abs(u2mph*Vector3.Dot(rb.velocity, forwardVector)) * (currentGear >= 0 ? 1 : -1);
        if (!engineRunning) {
            engineRPM = Mathf.MoveTowards(engineRPM, 0, (engine.engineBraking*(engineRPM/engine.redline)+8000f) * Time.fixedDeltaTime);
            if (engineStarting) {
                engineRPM = 1500 + Mathf.Sin(Time.time*64) * 200;
            }
        } else {
            float targetRPM = Mathf.Max(engine.idleRPM + Mathf.Sin(Time.time*64)*50f, !fuelCutoff ? gas*engine.redline : 0);
            float moveSpeed = !fuelCutoff ? engine.GetThrottleResponse(engineRPM) : (engine.engineBraking*(engineRPM/engine.redline)*3+1000f);
            float idealEngineRPM = Mathf.MoveTowards(engineRPM, targetRPM, moveSpeed * Time.fixedDeltaTime);
            if (currentGear != 0 && !clutch) {
                float wheelRPM = flatSpeed*Mathf.Sign(currentGear) * engine.diffRatio * engine.gearRatios[Mathf.Abs(currentGear)-1] / (WheelRL.wheelRadius * 2f * Mathf.PI) * 60f;
                if (clutchOutThisFrame) {
                    float rpmDiff = wheelRPM - engineRPM;
                    if (rpmDiff < 0) {
                        if (rpmDiff < -700 && currentGear > 1  && lastGear<currentGear) {
                            clutchRatio = 0f;
                            impulseSource.GenerateImpulse();
                            GearLurch();
                        } else if (Mathf.Abs(currentGear) == 1 && engine.PeakPower(idealEngineRPM) && Vector3.Dot(rb.velocity, forwardVector) * u2mph < 5f) {
                            // keep the clutch ratio soft to avoid a money shift on launch
                            PerfectShift(rpmDiff, alert: false);
                            Alert("perfect launch \n+" + (int) engine.maxPower);
                            clutchRatio = 0f;
                            rb.AddForce(forwardVector*(settings.launchBoost * mph2u)*Mathf.Sign(currentGear), ForceMode.VelocityChange);
                        } else if (currentGear > 1 && Vector3.Dot(rb.velocity, forwardVector) * u2mph > 1f) {
                            PerfectShift(rpmDiff);
                        }
                    } else {
                        if (UnityEngine.Random.Range(0f, 1f) < 0.8f) {
                            exhaustAnimator.SetTrigger("Backfire");
                        }
                        if (rpmDiff > 1000) {
                            clutchRatio = 0f;
                            impulseSource.GenerateImpulse();
                            GearLurch();
                        } else if (idealEngineRPM < engine.redline+500) {
                            // no perfect shift on the money shift
                            PerfectShift(rpmDiff);
                        }
                    }
                }
                engineRPM = Mathf.Lerp(idealEngineRPM, wheelRPM, grounded ? clutchRatio : idealEngineRPM);
                // TODO: eventually, apply force to the wheels based on power at rpm
                // then calculate wheelspin accordingly
            } else if (currentGear == 0 || clutch) {
                engineRPM = idealEngineRPM;
            }
        }

        clutchRatio = Mathf.MoveTowards(clutchRatio, 1f, engine.clutchSharpness * Time.fixedDeltaTime);

        engineAnimator.speed = engineRPM == 0 ? 0 : 1 + (engineRPM/engine.redline);
        if (engineRPM > engine.redline-100) {
            fuelCutoff = true;
            // backfire more at lower gears when bouncing off the redline
            float ratio = Mathf.Max(1-(currentGear/engine.gearRatios.Count), 1);
            if (1/ratio * UnityEngine.Random.Range(0f, 1f) < 0.1f) exhaustAnimator.SetTrigger("Backfire");
            rb.AddForce(-Vector3.Project(rb.velocity, forwardVector));
        } else {
            fuelCutoff = false;
        }

        if (engineRunning) {
            if (engineRPM < engine.stallRPM) {
                if (currentGear == 1 && gas > 0.7f) {
                    // mimic letting the clutch out slowly 
                    engineRPM = engine.stallRPM;
                } else {
                    Debug.Log("low RPM stall");
                    StallEngine();
                }
            } else if (engineRPM > engine.redline+500) {
                // Debug.Log("money shift, wanted this rpm: "+engineRPM);
                StallEngine();
            }
        }

        GetRPMPoint(engineRPM, gas);
        UpdateEngineLowPass();
        UpdateVibration();

        carBody.transform.localPosition = new Vector3(0, engineRPM/engine.redline * 0.025f, 0);
    }

    float AddLateralForce(Vector3 point, Vector3 lateralNormal, bool steeringAxle) {
        float lateralSpeed = Vector3.Dot(rb.GetPointVelocity(point), lateralNormal);
        float wantedAccel = -lateralSpeed * GetTireSlip(lateralSpeed) / Time.fixedDeltaTime;
        float gs = Mathf.Abs(wantedAccel / Physics.gravity.y);
        if (!steeringAxle) {
            if (gs > settings.maxCorneringGForce) {
                // later: add TCS here
                drifting = true;
                tireSkid.mute = !grounded;
                foreach (TrailRenderer t in tireSkids) {
                    t.emitting = grounded;
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
            // the back wheels are also ground-locked here, need to do the tirespin with torque diffs
            carBody.driftRoll = 5f;
            // instantly break traction, but ease back into it to avoid overcorrecting
            currentGrip = 0.5f / (gs / settings.maxCorneringGForce);
        } else {
            carBody.driftRoll = 0f;
        }
        // hmm. maybe do this after the lateral force?
        wantedAccel *= currentGrip;
        gs = Mathf.Abs(wantedAccel / Physics.gravity.y);
        gForceIndicator.rectTransform.localScale = new Vector3(gs, 1, 1);
        gForceText.text = Mathf.Abs(gs).ToString("F2") + " lateral G";
        rb.AddForceAtPosition(lateralNormal * wantedAccel, point, ForceMode.Acceleration);
        return wantedAccel;
    }

    float GetTireSlip(float lateralSpeed) {
        // this is flat right now, might want to make it a curve later
        return settings.tireSlip;
    }

    void PerfectShift(float rpmDiff, bool alert=true) {
        if (lastGear == currentGear) return;
        perfectShiftEffect.SetTrigger("Trigger");
        perfectShiftAudio.pitch = 1 + UnityEngine.Random.Range(-0.15f, 0.15f);
        perfectShiftAudio.Play();
        rpmDiff = Mathf.RoundToInt(rpmDiff);
        string t = "";
        if (lastGear > currentGear) {
            t = "revmatched downshift";
        } else if (lastGear < currentGear) {
            t = "revmatched upshift";
        }
        if (alert) Alert(t + "\n+" + (1000-Mathf.Abs(rpmDiff)));
    }

    void Alert(string text) {
        alertText.text = text;
        alertAnimator.SetTrigger("Trigger");
    }

    void ChangeGear(int to) {
        gearshiftAudio.PlayOneShot(engine.gearShiftNoises[UnityEngine.Random.Range(0, engine.gearShiftNoises.Count)]);
        currentGear = to;
    }

    IEnumerator GearLurch() {
        carBody.maxXAngle *= 2f;
        yield return new WaitForSeconds(settings.gearShiftTime);
        carBody.maxXAngle /= 2f;
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
            engineAudio.outputAudioMixerGroup.audioMixer.SetFloat("ExhaustLowPassCutoff", 6000);
        } else {
            cameraEngineAngle -= 90f;
            engineAudio.outputAudioMixerGroup.audioMixer.SetFloat("ExhaustLowPassCutoff", Mathf.Lerp(6000, 22000, cameraEngineAngle/90f));
        }
    }

    void UpdateTelemetry() {
        speedText.text = (rb.velocity.magnitude * u2mph).ToString("F0");
        if (currentGear > 0) {
            gearTelemetry.text = currentGear.ToString();
        } else if (currentGear == 0) {
            gearTelemetry.text = "N";
        } else {
            gearTelemetry.text = "R";
        }
        clutchText.color = new Color(1, 1, 1, clutch ? 1 : (1 - (0.95f*clutchRatio)));
        tachometer.SetRPM(engineRPM, engine.redline);
        speedometer.SetSpeed(rb.velocity.magnitude * u2mph, 180);
    }

    void UpdateSteering() {
        float targetSteerAngle = Mathf.Abs(steering * settings.maxSteerAngle) * Mathf.Sign(steering);
        // don't go full lock at 100mph
        float steeringMult = Mathf.Lerp(
            1,
            0.3f,
            Mathf.Abs(Vector3.Dot(rb.velocity, forwardVector)*u2mph) / 120f
        );
        if (steering == 0) {
            targetSteerAngle = 0;
        }
        float steerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteerAngle*steeringMult, settings.steerSpeed);

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
        // audibly drop the audio on fuel cutoff so you Know
        lowTarget.throttleOffAudio.volume *= 1-gas * 0.5f * (!fuelCutoff ? 1f : 0.2f);
        highTarget.throttleOffAudio.volume *= 1-gas * 0.5f * (!fuelCutoff ? 1f : 0.2f);

        // then warp the sound to match the RPM
        lowTarget.throttleAudio.pitch = targetLowPitch;
        lowTarget.throttleOffAudio.pitch = targetLowPitch;
        highTarget.throttleAudio.pitch = targetHighPitch;
        highTarget.throttleOffAudio.pitch = targetHighPitch;
    }
}
