using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.Events;
using Cinemachine;
using UnityEditorInternal;
using UnityEngineInternal;

public class Car : MonoBehaviour {

    public GameObject wheelTemplate;
    public Wheel WheelFL, WheelFR, WheelRL, WheelRR;
    public CarSettings settings;
    public EngineSettings engine;
    public GameObject centerOfGravity;

    public float gas { get; private set; }
    public float brake;
    public float steering;
    float targetSteerAngle;

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
    float engineRPMFromSpeed = 0f;
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
    public float forwardTraction = 1f;
    bool clutch = false;
    bool clutchOutThisFrame = false;
    float clutchOutTime = 0;
    public float clutchRatio = 1f;

    float currentGrip = 1f;

    public const float u2mph = 2.2369362912f;
    public const float mph2u = 1/u2mph;

    Tachometer tachometer;
    Speedometer speedometer;
    NitroxMeter nitroxMeter;
    public Animator perfectShiftEffect;
    public Animator alertAnimator;
    Text alertText;

    public AudioSource perfectShiftAudio;
    public int lastGear;

    public EngineLight checkEngine;
    public EngineLight transmissionTemp;
    public EngineLight tcsLight;
    public EngineLight lcsLight;
    public EngineLight handbrakeLight;

    bool tcs = true;
    float handbrakeDown = -999;
    float tcsFrac;
    Vector3 frontAxle, rearAxle;
    float bumpTS = -999;

    public AudioClip boostSound;
    bool boosting = false;
    bool automatic = false;

    float tireSkidVolume;
    float spawnTime;

    public bool Drifting {
        get {
            return drifting;
        }
    }

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
        tachometer = GetComponentInChildren<Tachometer>();
        speedometer = GetComponentInChildren<Speedometer>();
        nitroxMeter = GetComponentInChildren<NitroxMeter>();
        nitroxMeter.max = settings.maxNitrox;
        spawnTime = Time.time;
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
            rAudio.minDistance = engineAudio.minDistance;
            rAudio.Play();
            AudioSource rOffAudio = engineAudio.AddComponent<AudioSource>();
            rOffAudio.volume = maxEngineVolume;
            rOffAudio.outputAudioMixerGroup = engineAudio.outputAudioMixerGroup;
            rOffAudio.clip = r.throttleOff;
            rOffAudio.loop = true;
            rOffAudio.spatialBlend = 1;
            rOffAudio.minDistance = engineAudio.minDistance;
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
            clutchOutTime = Time.time;
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
        
        if (InputManager.ButtonDown(Buttons.STARTENGINE)) {
            if (!engineRunning && !engineStarting && clutch) {
                StartCoroutine(StartEngine());
            } else {
                engineRunning = false;
            }
        }

        if (InputManager.ButtonDown(Buttons.HANDBRAKE)) {
            handbrakeDown = Time.time;
        }

        foreach (Wheel w in wheels) {
            float rpm = GetWheelRPMFromSpeed(Vector3.Dot(rb.velocity, transform.forward));
            if ((w == WheelRR || w == WheelRL) && !clutch && currentGear != 0) {
                rpm = Mathf.Lerp(GetWheelRPMFromEngineRPM(engineRPM), rpm, forwardTraction);
            }
            if (!grounded) {
                rpm = GetWheelRPMFromEngineRPM(engineRPM);
            }

            bool wheelBoost = false;
            if (w == WheelRR || w == WheelRL) {
                wheelBoost = boosting;
            }
            w.UpdateWheelVisuals(
                Vector3.Dot(rb.GetPointVelocity(w.transform.position),transform.forward),
                rpm,
                wheelBoost,
                Drifting
            );
        }

        if (InputManager.ButtonDown(Buttons.BOOST)) {
            StartCoroutine(Boost());
            rb.AddRelativeTorque(-125, 0, 0, ForceMode.Acceleration);
        }

        if (boosting) {
            // rb.AddRelativeTorque(4, 0, 0);
        }

        tireSkid.volume = Mathf.MoveTowards(tireSkid.volume, tireSkidVolume, 4f * Time.deltaTime);
    }

    IEnumerator Boost() {
        gearshiftAudio.PlayOneShot(boostSound);
        nitroxMeter.OnBoost();
        boosting = true;
        automatic = true;
        yield return new WaitForSeconds(settings.boostDuration);
        boosting = false;
        yield return new WaitForSeconds(1);
        automatic = false;
    }

    IEnumerator StartEngine() {
        engineStarting = true;
        engineAudio.PlayOneShot(engine.startupNoise);
        carBody.StartWobbling();
        foreach (EngineLight l in GetComponentsInChildren<EngineLight>()) {
            l.Startup();
        }
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
        }

        frontAxle = (WheelFL.transform.position + WheelFR.transform.position) / 2f;
        rearAxle = (WheelRL.transform.position + WheelRR.transform.position) / 2f;

        if (WheelRR.Grounded || WheelRL.Grounded) {
            if (gas > 0 && !fuelCutoff && engineRunning && currentGear != 0 && !clutch) {
                float mult = currentGear < 0 ? -1 : 1;
                mult *= boosting ? settings.nitroxBoost : 1;
                // if you can just barely toe the line with TCS, you don't get slowed down
                // maybe rework this so you fill the boost meter while being on the edge of TCS or something
                mult *= 1-tcsFrac*0.2f;
                Vector3 desiredForce = transform.forward * engine.GetPower(engineRPM)*gas*mult;
                forwardTraction = 1f;
                Vector3 desiredVelocity = desiredForce * Time.fixedDeltaTime / rb.mass;
                float mphNextStep = Vector3.Dot(rb.velocity+desiredVelocity, transform.forward) * u2mph;
                float desiredWheelRPM = GetWheelRPMFromSpeed(mphNextStep);
                float actualWheelRPM = GetWheelRPMFromSpeed(Vector3.Dot(rb.velocity, transform.forward)*u2mph);
                float diff = Mathf.Abs(desiredWheelRPM - actualWheelRPM);
                diff = Mathf.Max(0, diff-settings.burnoutThreshold);

                // the higher the difference is, the more you spin the wheels
                float spinRatio;
                if (actualWheelRPM == 0) {
                    spinRatio = 0.01f;
                } else {
                    spinRatio = 1/((actualWheelRPM+diff) / actualWheelRPM);
                }
                forwardTraction = 1 * spinRatio;

                desiredForce *= forwardTraction;
                rb.AddForceAtPosition(desiredForce, rearAxle);

                // drift boost approaches 0 as the car straightens out
                mult = drifting ? settings.driftBoost : 0;
                mult *= Vector3.SignedAngle(transform.forward, Vector3.ProjectOnPlane(rb.velocity, transform.up), transform.up) / 90f;

                rb.AddForce(Quaternion.Euler(0, currentSteerAngle, 0) * desiredForce * mult);
            } else {
                forwardTraction = 1f;
                if (engineRunning) rb.AddForce(-Vector3.Project(rb.velocity, transform.forward) * (engineRPM/engine.redline) * engine.engineBraking);
            }
        }

        if (brake > 0 && grounded) {
            Vector3 flatSpeed = Vector3.Project(rb.velocity, transform.forward);
            if ((flatSpeed.magnitude*u2mph) < 1) {
                rb.velocity -= flatSpeed;
            } else {
                rb.AddForce(-flatSpeed.normalized * brake * settings.brakeForce);
            }
        }

        if (grounded && Time.time > handbrakeDown + 0.2f && InputManager.Button(Buttons.HANDBRAKE)) {
            Vector3 flatSpeed = Vector3.Project(rb.velocity, transform.forward);
            rb.AddForce(-flatSpeed.normalized * settings.brakeForce);
            handbrakeLight.SetOn();
        } else {
            handbrakeLight.SetOff();
        }


        UpdateSteering();
        if (!drifting) {
            // gradually return to max grip after a drift
            currentGrip = Mathf.MoveTowards(currentGrip, 1f, 0.5f*Time.fixedDeltaTime);
        }
        
        if (grounded) {
            if (WheelRL.Grounded || WheelRR.Grounded) {
                AddLateralForce(rearAxle, transform.right, false);
            }

            if (WheelFL.Grounded || WheelFR.Grounded) {
                // rotate the lateral for the front axle by the amount of steering
                AddLateralForce(frontAxle, Quaternion.AngleAxis(targetSteerAngle, transform.up) * transform.right, true);
                if ((drifting || forwardTraction < 0.9f) && rb.velocity.sqrMagnitude > 1f) {
                    rb.AddTorque(transform.up * steering * settings.maxSteerAngle * settings.driftControl);
                }
            }

            float flatSpeed = Mathf.Abs(Vector3.Dot(rb.velocity, transform.forward)) * u2mph;
            if (flatSpeed < 0.2f) {
                wheelAudio.volume = 0;
            } else {
                wheelAudio.volume = 0.5f;
                wheelAudio.pitch = Mathf.Lerp(1, 3f, flatSpeed / 80f);
            }
            tireSkid.mute = false;
            if (drifting) {
                nitroxMeter.Add(settings.driftNitroGain * Time.fixedDeltaTime);
            }
        } else {
            wheelAudio.volume = 0;
            tireSkid.mute = true;
        }

        float dragForce = 0.5f * rb.velocity.sqrMagnitude * settings.drag * 0.003f;
        if (gas==0 || fuelCutoff) dragForce = 0;
        rb.AddForce(-rb.velocity*dragForce, ForceMode.Force);

        UpdateEngine();
        UpdateTelemetry();
        clutchOutThisFrame = false;

        if (forwardTraction < 1) {
            lcsLight.SetOn();
        } else {
            lcsLight.SetOff();
        }

        // right the car if upside down
        if (Physics.Raycast(
            transform.position, 
            transform.up,
            5, 
            1 << LayerMask.NameToLayer("Ground")
        ) && !grounded) {
            rb.AddTorque(new Vector3(0, 0, steering * 100), ForceMode.Acceleration);
        }
    }

    float GetEngineRPMFromSpeed(float flatSpeed) {
        return GetWheelRPMFromSpeed(flatSpeed)
            * Mathf.Sign(currentGear)
            * engine.diffRatio * engine.gearRatios[Mathf.Abs(currentGear)-1];
    }

    float GetWheelRPMFromSpeed(float flatSpeed) {
        return flatSpeed / (WheelRL.wheelRadius * 2f * Mathf.PI) * 60f;
    }

    float GetWheelRPMFromEngineRPM(float engineRPM) {
        return engineRPM / engine.diffRatio / engine.gearRatios[Mathf.Abs(currentGear-1)] * Mathf.Sign(currentGear);
    }

    void UpdateEngine() {
        float flatSpeed = u2mph*Vector3.Dot(rb.velocity, transform.forward);
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
                engineRPMFromSpeed = GetEngineRPMFromSpeed(flatSpeed);
                if (clutchOutThisFrame) {
                    float rpmDiff = engineRPMFromSpeed - engineRPM;
                    if (rpmDiff < 0) {
                        if (rpmDiff < -700 && currentGear > 1  && lastGear<currentGear) {
                            clutchRatio = 0.5f;
                            impulseSource.GenerateImpulse();
                            GearLurch();
                        } else if (Mathf.Abs(currentGear) == 1 && engine.PeakPower(idealEngineRPM) && Vector3.Dot(rb.velocity, transform.forward) * u2mph < 5f) {
                            // keep the clutch ratio soft to avoid a money shift on launch
                            PerfectShift(rpmDiff, alert: false);
                            Alert("perfect launch \n+" + (int) engine.maxPower*5);
                            nitroxMeter.Add(engine.maxPower*5);
                            clutchRatio = 0.5f;
                            rb.AddForce(transform.forward*(settings.launchBoost * mph2u)*Mathf.Sign(currentGear), ForceMode.VelocityChange);
                        } else if (currentGear > 1 && Vector3.Dot(rb.velocity, transform.forward) * u2mph > 1f) {
                            PerfectShift(rpmDiff);
                        }
                    } else {
                        if (Random.Range(0f, 1f) < 0.8f) {
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
                engineRPM = Mathf.Lerp(
                    idealEngineRPM,
                    engineRPMFromSpeed,
                    grounded ? clutchRatio : idealEngineRPM);

                // then if there's wheelspin, move more towards what the engine actually wants
                engineRPM = Mathf.Lerp(
                    targetRPM,
                    engineRPM,
                    Mathf.Max(forwardTraction, 0.2f) * clutchRatio
                );
            } else if (currentGear == 0 || clutch) {
                engineRPM = idealEngineRPM;
            }
        }

        clutchRatio = Mathf.MoveTowards(clutchRatio, 1f, engine.clutchSharpness * Time.fixedDeltaTime);

        engineAnimator.speed = engineRPM == 0 ? 0 : 1 + (engineRPM/engine.redline);
        if (engineRPM > engine.redline-100) {
            if (automatic && currentGear < engine.gearRatios.Count && !clutch) {
                ChangeGear(currentGear + 1);
            } else {
                fuelCutoff = true;
                if (Random.Range(0f, 1f) < 0.1f) exhaustAnimator.SetTrigger("Backfire");
                rb.AddForce(-Vector3.Project(rb.velocity, transform.forward));
            }
        } else {
            fuelCutoff = false;
        }

        if (engineRunning) {
            if (engineRPM < engine.stallRPM) {
                if (Mathf.Abs(currentGear) == 1 && gas > 0.7f) {
                    // mimic letting the clutch out slowly 
                    engineRPM = engine.stallRPM;
                } else {
                    checkEngine.Flash();
                    StallEngine();
                }
            } else if (engineRPM > engine.redline+500) {
                if (automatic && currentGear < engine.gearRatios.Count && !clutch) {
                    ChangeGear(currentGear + 1);
                } else {
                    transmissionTemp.Flash();
                    StallEngine();
                }
            }
        }

        GetRPMPoint(engineRPM, gas);
        UpdateEngineLowPass();
        UpdateVibration();

        carBody.transform.localPosition = new Vector3(0, engineRPM/engine.redline * 0.025f, 0);
    }

    float GetWantedAccel(float sa, Vector3 flatVelocity) {
        float theta = 90f + sa + Vector3.SignedAngle(flatVelocity, transform.forward, transform.up);
        float manualDot = flatVelocity.magnitude * Mathf.Cos(theta*Mathf.Deg2Rad);
        float wantedAccel = manualDot * settings.tireSlip / Time.fixedDeltaTime;
        return wantedAccel * -1;
    }

    float GetWantedSteeringAngle(float wantedAccel, Vector3 flatVelocity) {
        float manualDot = wantedAccel * Time.fixedDeltaTime / settings.tireSlip;
        if (flatVelocity.magnitude < Mathf.Epsilon) return 0;

        float cosThetaRad = manualDot / flatVelocity.magnitude;
        cosThetaRad = Mathf.Clamp(cosThetaRad, -1f, 1f);

        float theta = Mathf.Acos(cosThetaRad) * Mathf.Rad2Deg;

        float sa = theta + 90f + Vector3.SignedAngle(flatVelocity, transform.forward, transform.up);
        return (sa-180) * -1;
    }


    void UpdateSteering() {
        targetSteerAngle = steering * settings.maxSteerAngle;
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        float steeringMult = settings.steerLimitCurve.Evaluate(Mathf.Abs(forwardSpeed));
        if (steering == 0) {
            targetSteerAngle = 0;
        }
        targetSteerAngle *= steeringMult;
        if (tcs && !InputManager.Button(Buttons.HANDBRAKE) && !drifting && grounded && forwardSpeed>1f) {
            Vector3 axleVelocity = rb.GetPointVelocity(frontAxle);
            Vector3 flatVelocity = Vector3.ProjectOnPlane(axleVelocity, transform.up);
            float wantedAccel = GetWantedAccel(targetSteerAngle, flatVelocity);

            if (Mathf.Abs(wantedAccel) > settings.maxCorneringForce) {
                targetSteerAngle = GetWantedSteeringAngle(settings.maxCorneringForce * 0.9f * Mathf.Sign(wantedAccel), flatVelocity);
                tcsFrac = 1;
                tcsLight.SetOn();
            } else {
                if (Mathf.Abs(wantedAccel)>settings.maxCorneringForce*0.8f && Mathf.Abs(wantedAccel)<settings.maxCorneringForce) {
                    // eventually: highlight the speedmeter and alert that it's almost TCS
                    nitroxMeter.Add(forwardSpeed*(settings.edgeNitroGain/100f) * Time.fixedDeltaTime);
                }
                tcsLight.SetOff();
            }
        } else {
            tcsLight.SetOff();
        }

        currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteerAngle, settings.steerSpeed * Time.fixedDeltaTime);
        Quaternion targetRotation = Quaternion.Euler(0, currentSteerAngle, 0);
        WheelFL.transform.localRotation = targetRotation;
        WheelFR.transform.localRotation = targetRotation;
    }

    float AddLateralForce(Vector3 point, Vector3 lateralNormal, bool steeringAxle) {
        Vector3 flatVelocity = Vector3.ProjectOnPlane(rb.GetPointVelocity(point), transform.up);
        if (flatVelocity.sqrMagnitude < Mathf.Epsilon) {
            return 0;
        }
        float lateralSpeed = Vector3.Dot(flatVelocity, lateralNormal);
        float wantedAccel = -lateralSpeed * settings.tireSlip / Time.fixedDeltaTime;
        if (steeringAxle) {
            if (Mathf.Abs(wantedAccel) > settings.maxCorneringForce) {
                drifting = true;
                tireSkidVolume = 1;
            } else {
                tireSkidVolume = 0;
                drifting = false;
            }
            if (drifting || forwardTraction < 0.9f) {
                carBody.driftRoll = 5f * Mathf.Sign(Vector3.Dot(rb.velocity, transform.right));
                currentGrip = 0.5f / (Mathf.Abs(wantedAccel) / settings.maxCorneringForce);
            } else {
                carBody.driftRoll = 0f;
            }
            float gs = ToGs(wantedAccel);
            gForceIndicator.rectTransform.localScale = new Vector3(gs, 1, 1);
            gForceText.text = Mathf.Abs(gs).ToString("F2") + " lateral G";
        }
        // hmm. maybe do this after the lateral force?
        wantedAccel *= currentGrip;
        rb.AddForceAtPosition(lateralNormal * wantedAccel, point, ForceMode.Acceleration);
        return wantedAccel;
    }

    void PerfectShift(float rpmDiff, bool alert=true) {
        if (lastGear == currentGear) return;
        if (engineRPM + rpmDiff > engine.redline+500) return;
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
        int bonus = (int) Mathf.Clamp(1000-Mathf.Abs(rpmDiff), 10, 1000);
        if (alert) {
            Alert(t + "\n+" + bonus);
            nitroxMeter.Add(bonus);
        }
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
        rb.AddForce(-Vector3.Project(rb.velocity, transform.forward)*0.8f / Time.fixedDeltaTime, ForceMode.Acceleration);
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
        float cameraEngineAngle = Vector3.Angle(transform.forward, Vector3.ProjectOnPlane(towardsCamera, transform.up));
        if (cameraEngineAngle < 90) {
            engineAudio.outputAudioMixerGroup.audioMixer.SetFloat("ExhaustLowPassCutoff", 6000);
        } else {
            cameraEngineAngle -= 90f;
            engineAudio.outputAudioMixerGroup.audioMixer.SetFloat("ExhaustLowPassCutoff", Mathf.Lerp(6000, 20000, cameraEngineAngle/90f));
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

    float ToGs(float force) {
        return Mathf.Abs(force / Physics.gravity.y);
    }

    void UpdateVibration() {
        if (Time.time < spawnTime + 0.5f || Time.timeScale != 1) {
            return;
        }
        float startVibration = 0;
        float rpmVibration = 0;
        if (engineRPM > engine.redline - 2000) {
            rpmVibration += ((engineRPM - (engine.redline-2000)) / 2000)*0.5f;
        }

        if (engineStalling || engineStarting) {
            startVibration = 1f;
        }

		// check if wheel suspension is compressed
		float bumpVibration;
		foreach (Wheel w in wheels) {
			if (w.GetCompressionRatio() > 1f) {
				bumpTS = Time.time;
			}
		}
		if (Time.time < bumpTS + 0.2f) {
			bumpVibration = 1f;
		} else {
			bumpVibration = 0;
		}

		InputManager.player.SetVibration(0, startVibration+bumpVibration);
        InputManager.player.SetVibration(1, startVibration+rpmVibration);
        
    }

    void GetRPMPoint(float rpm, float gas) {
        if (currentGear > lastGear) {
            // wobble the engine noise a little bit based on driveline flex
            float timeSinceClutchUp = Time.time - clutchOutTime;
            // decrease over time
            float mult = Mathf.Clamp(1 - (timeSinceClutchUp / 0.8f), 0, 1);
            // wobble more at lower gears
            mult *= 1 - (Mathf.Abs(currentGear)/engine.gearRatios.Count);
            rpm += mult * settings.drivelineFlex * Mathf.Sin(Time.time * 48f) * settings.drivelineFlex * 100 * gas;
        }

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
