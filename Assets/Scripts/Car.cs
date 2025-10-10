using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Cinemachine;
using Rewired;
using Random = UnityEngine.Random;

[RequireComponent(typeof(EngineAudio))]
public class Car : MonoBehaviour {

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
    bool groundedLastStep;
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
    public AudioSource engineAudioSource;
    public AudioSource gearshiftAudio;
    public AudioSource wheelAudio;
    public AudioSource tireSkid;
    public AudioSource pointsAudio;

    public TrailRenderer[] tireSkids;

    readonly List<RPMPoint> rpmPoints = new();

    public Text gearTelemetry;
    bool fuelCutoff = false;
    bool gearshiftFuelCutoff = false;
    public int currentGear { get; private set; }
    bool engineStarting = false;
    public bool engineRunning { get; private set; }
    bool engineStalling = false;
    
    Camera mainCamera;

    public UnityEvent onGearChange;

    CarBody carBody;

    CinemachineImpulseSource impulseSource;
    public CinemachineImpulseSource trackingCamImpulseSource;

    bool drifting = false;
    bool burnout = false;
    float driftingTime = 0;
    bool hydroplaning = false;
    bool inWater = false;
    public AudioSource hydroplaneNoise;
    public float forwardTraction = 1f;
    bool clutch = false;
    bool clutchOutThisFrame, clutchInThisFrame;
    float clutchOutTime = 0;
    public float clutchRatio = 1f;
    float brakeGlow = 0f;

    float currentGrip = 1f;

    public const float u2mph = 2.2369362912f;
    public const float mph2u = 1/u2mph;

    Tachometer tachometer;
    Speedometer speedometer;
    NitroxMeter nitroxMeter;
    public Animator perfectShiftEffect;
    public Animator gripEffect;
    float gripEffectStrength;
    AlertText alertText;
    bool shiftLurch = false;

    public AudioSource perfectShiftAudio;
    public int lastGear;

    public EngineLight checkEngine;
    public EngineLight transmissionTemp;
    public EngineLight tcsLight;
    public EngineLight lcsLight;
    public EngineLight handbrakeLight;

    float handbrakeDown = -999;
    bool assistDisabled = false;
    bool lcsDisabled = false;
    float tcsFrac;
    Vector3 frontAxle, rearAxle;
    float bumpTS = -999;
    float vibrationTS;
    float timeAtEdge = 0;

    public AudioClip boostSound;
    public AudioClip[] impactSounds;
    public GameObject boostEffect;
    public bool boosting { get; private set; }
    bool automatic = false;

    float tireSkidVolume;
    float spawnTime;
    public bool finished { get; private set; }

    [SerializeField]
    List<Canvas> dashboardUI;
    public int dashboardDisableSources = 0;

    Vector3 startPoint;
    Quaternion startRotation;

    public UnityEvent onRespawn;
    IEnumerator respawnRoutine;
    public UnityEvent onEngineStart;
    public bool forceClutch;
    public bool forceBrake;

    public ParticleSystem collisionHitmarker;
    MaterialPropertyBlock shaderBlock;
    MeshRenderer carMesh;
    
    Achievements achievements;
    
    bool usingKeyboard, usingWheel, usingController;

    // this is currently broken
    bool fullAutomatic = false;
    bool changingGear = false;    

    public bool Drifting {
        get {
            return drifting;
        }
    }

    bool ignition {
        get { return !fuelCutoff && engineRunning && !gearshiftFuelCutoff; }
    }

    EngineAudio engineAudio;
    
    Coroutine collisionRecoveryRoutine;

    RaceData raceData = new();

    void Awake() {
        wheels = new Wheel[]{WheelFL, WheelFR, WheelRL, WheelRR};
    }

    void Start() {
        respawnRoutine = RespawnRoutine();
        currentGear = 0;
        rb = GetComponent<Rigidbody>();
        rb.inertiaTensor = new Vector3(20, 25, 6);
        rb.mass = settings.carMass;
        rb.centerOfMass = centerOfGravity.transform.localPosition;
        
        engineAnimator = GetComponent<Animator>();
        engineAudio = GetComponent<EngineAudio>();
        engineAudio.BuildSoundCache(engine, engineAudioSource);
        mainCamera = Camera.main;
        carBody = GetComponentInChildren<CarBody>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        foreach (TrailRenderer t in tireSkids) {
            t.emitting = false;
        }
        alertText = GetComponentInChildren<AlertText>();
        tachometer = GetComponentInChildren<Tachometer>();
        speedometer = GetComponentInChildren<Speedometer>();
        nitroxMeter = GetComponentInChildren<NitroxMeter>();
        nitroxMeter.max = settings.maxNitrox;
        spawnTime = Time.time;
        FinishLine f = FindObjectOfType<FinishLine>();
        if (f) {
            f.onFinishCross.AddListener(() => {
                if (FindObjectOfType<RaceLogic>().raceType == RaceType.HOTLAP) {
                    nitroxMeter.Reset();
                    raceData = new();
                }
            });
            RaceLogic r = FindObjectOfType<RaceLogic>();
            dashboardUI.Add(r.GetComponentInChildren<Canvas>());
            r.onValidFinish.AddListener(OnValidFinish);
        }
        pointsAudio.mute = true;
        shaderBlock = new();
		carMesh = transform.Find("BodyMesh/CarBase/Body").GetComponent<MeshRenderer>();
        carMesh.GetPropertyBlock(shaderBlock, 0);
        StartLine startLine = FindObjectOfType<StartLine>();
        if (startLine != null) {
            Vector3 pos = startLine.transform.position + (0.1f * Vector3.up);
            transform.SetPositionAndRotation(pos, startLine.transform.rotation);
            rb.MovePosition(pos);
            rb.MoveRotation(startLine.transform.rotation);
        }
        startPoint = transform.position;
        startRotation = transform.rotation;
        achievements = FindObjectOfType<Achievements>();
        onRespawn.AddListener(engineAudio.OnCarRespawn);
    }

    void Update() {
        UpdateInputs();

        bool currentClutch = InputManager.Clutch() || forceClutch;
        if (clutch && !currentClutch) {
            clutchOutThisFrame = true;
            if (!automatic) raceData.totalShifts++;
            clutchOutTime = Time.time;
        } else if (!clutch && currentClutch) {
            clutchInThisFrame = true;
        }
        clutch = currentClutch;

        boostEffect.SetActive(boosting);

        if (drifting || forwardTraction < 1f || boosting) {
            tireSkidVolume = 0.5f;
        } else {
            tireSkidVolume = 0f;
        }
        tireSkid.volume = Mathf.MoveTowards(tireSkid.volume, tireSkidVolume, 4f * Time.deltaTime);
        pointsAudio.mute = !((tcsFrac==0 && timeAtEdge>0.2f) || driftingTime>0);
        UpdateVibration();
        UpdateCameraVibration();
        carMesh.GetPropertyBlock(shaderBlock, 0);
        shaderBlock.SetColor("_Emissive_Color", brake > 0 ? Color.white : Color.black);
        carMesh.SetPropertyBlock(shaderBlock, 0);
        if (clutch && (currentGear == 1)) {
            if (engine.PeakPower(engineRPM)) {
                float diff = Mathf.Abs(engine.maxPower - engine.GetPower(engineRPM));
                float percent = 1 - (diff / (engine.maxPower * 0.1f));
                perfectShiftEffect.SetFloat("PowerLight", percent);
            }
        } else {
            perfectShiftEffect.SetFloat("PowerLight", 0);
        }
    }

    void UpdateInputs() {
        if (Time.timeScale == 0) return;
        gas = InputManager.GetAxis(Buttons.GAS);
        brake = InputManager.GetAxis(Buttons.BRAKE);
        if (forceBrake) brake = 1;
        steering = InputManager.GetAxis(Buttons.STEER);

         if (InputManager.ClutchIn() || InputManager.ClutchOut()) {
            gearshiftAudio.PlayOneShot(engine.clutchSound);
        }

        if (InputManager.ClutchIn()) {
            lastGear = currentGear;
        }

        // TODO: check for paddle shifting mode
        if (
            (InputManager.ButtonDown(Buttons.GEARDOWN) && clutch)
            // this is to allow buffering of a shift input if the player slightly mistimes it
            || (clutchInThisFrame && (InputManager.player.GetButtonTimePressed(Buttons.GEARDOWN) > ReInput.time.unscaledTime-0.25f))
        ) {
            if (InputManager.Button(Buttons.SHIFTALT)) {
                currentGear = -1;
                if (Vector3.Dot(rb.velocity, transform.forward) > 60*mph2u) {
                    achievements.Get("R For Racing");
                    StallEngine();
                }
            } else {
                if (currentGear > -1) {
                    ChangeGear(currentGear - 1);
                }
            }
        } else if (
            (InputManager.ButtonDown(Buttons.GEARUP) && clutch)
            || (clutchInThisFrame && (InputManager.player.GetButtonTimePressed(Buttons.GEARUP) > ReInput.time.unscaledTime-0.25f))
        ) {
            if (currentGear < engine.gearRatios.Count) {
                ChangeGear(currentGear+1);
            }
        }

        if (GameOptions.PaddleShift && !changingGear) {
            if (InputManager.ButtonDown(Buttons.PADDLE_UP)) {
                StartCoroutine(AutoGearUp());
            } else if (InputManager.ButtonDown(Buttons.PADDLE_DOWN)) {
                StartCoroutine(AutoGearDown());
            }
        }
        
        if (InputManager.ButtonDownWithManualClutch(Buttons.STARTENGINE)) {
            if (!engineRunning && !engineStarting) {
                StartCoroutine(StartEngine());
            } else {
                engineRunning = false;
            }
        }

        if (InputManager.ButtonDown(Buttons.HANDBRAKE)) {
            handbrakeDown = Time.time;
        }

        if (InputManager.ButtonDown(Buttons.PAUSE) && InputManager.Button(Buttons.CLUTCH)) {
			Respawn();
		}

        if (clutch) {
            if (InputManager.ButtonDown(Buttons.GEAR1)) {
                ChangeGear(1, true);
            }
            if (InputManager.ButtonDown(Buttons.GEAR2)) {
                ChangeGear(2, true);
            }
            if (InputManager.ButtonDown(Buttons.GEAR3)) {
                ChangeGear(3, true);
            }
            if (InputManager.ButtonDown(Buttons.GEAR4)) {
                ChangeGear(4, true);
            }
            if (InputManager.ButtonDown(Buttons.GEAR5)) {
                ChangeGear(5, true);
            }
            if (InputManager.ButtonDown(Buttons.GEAR6)) {
                ChangeGear(6, true);
            }
            if (InputManager.ButtonDown(Buttons.GEARR)) {
                ChangeGear(-1, true);
            }
        }

        if (settings.enableNitrox && InputManager.ButtonDown(Buttons.BOOST) && nitroxMeter.Ready()) {
            StartCoroutine(Boost());
            rb.AddRelativeTorque(-125, 0, 0, ForceMode.Acceleration);
        }
    }

    void OnCollisionEnter(Collision collision) {
        // don't play the bang noise if the car suspension bottoms out
        if (rb.velocity.sqrMagnitude > 5 * mph2u && Vector3.Angle(transform.up, collision.contacts[0].normal) > 20f) {
            gearshiftAudio.PlayOneShot(impactSounds[Random.Range(0, impactSounds.Length-1)]);
            // ok need ot not play this when the wheel colliders hit. howmst
            collisionHitmarker.transform.SetPositionAndRotation(collision.contacts[0].point, Quaternion.FromToRotation(
                collisionHitmarker.transform.up,
                collision.contacts[0].normal
            ));
            collisionHitmarker.Emit(1);
        }

        if (grounded && collision.gameObject.layer == LayerMask.NameToLayer("Ground")) {
            if (collisionRecoveryRoutine != null) {
                StopCoroutine(collisionRecoveryRoutine);
            }
            collisionRecoveryRoutine = StartCoroutine(CollisionRecovery());
        }
    }

    IEnumerator Boost() {
        raceData.nitros++;
        rb.velocity -= Vector3.Project(rb.velocity, transform.right);
        gearshiftAudio.PlayOneShot(boostSound);
        nitroxMeter.OnBoost();
        StartCoroutine(GearLurch());
        rb.AddForce(100 * settings.nitroxBoost * transform.forward, ForceMode.Impulse);
        boosting = true;
        automatic = true;
        yield return new WaitForSeconds(settings.boostDuration);
        boosting = false;
        yield return new WaitForSeconds(1);
        automatic = false;
    }

    IEnumerator StartEngine() {
        onEngineStart.Invoke();
        engineStarting = true;
        engineAudioSource.PlayOneShot(engine.startupNoise);
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
        FLForce = WheelFL.GetSuspensionForce(rb);
        FRForce = WheelFR.GetSuspensionForce(rb);
        RLForce = WheelRL.GetSuspensionForce(rb);
        RRForce = WheelRR.GetSuspensionForce(rb);

        WheelFL.AddForce(this, FLForce);
        WheelFR.AddForce(this, FRForce);
        WheelRL.AddForce(this, RLForce);
        WheelRR.AddForce(this, RRForce);

        grounded = false;
        hydroplaning = false;
        inWater = true;
        foreach (Wheel w in wheels) {
            if (w.Grounded) {
                grounded = true;
            }
            if (w.hydroplaning) {
                hydroplaning = true;
            }
            if (!w.inWater) {
                // all four wheels gotta be in/not hydroplaning
                inWater = false;
            }
        }

        frontAxle = (WheelFL.transform.position + WheelFR.transform.position) / 2f;
        rearAxle = (WheelRL.transform.position + WheelRR.transform.position) / 2f;

        UpdateEngine();
        if (Time.timeScale > 0) {
            float targetBrakeGlow = grounded && rb.velocity.sqrMagnitude > 5f && (brake > 0) ? 1 : 0;
            if (targetBrakeGlow > brakeGlow) {
                brakeGlow = Mathf.Lerp(brakeGlow, targetBrakeGlow, 10f * Time.fixedDeltaTime);
            } else {
                brakeGlow = Mathf.Lerp(brakeGlow, targetBrakeGlow, 1f * Time.fixedDeltaTime);
            }

            foreach (Wheel w in wheels) {
                float rpm = w.GetWheelRPMFromSpeed(Vector3.Dot(rb.velocity, transform.forward));
                // if in a drive gear with the clutch in
                if ((w == WheelRR || w == WheelRL) && !clutch && currentGear != 0 && engineRunning) {
                    rpm = Mathf.Lerp(rpm, GetWheelRPMFromEngineRPM(engineRPM), forwardTraction*clutchRatio);
                    if (!grounded) {
                        rpm = GetWheelRPMFromEngineRPM(engineRPM);
                    }
                } else if ((w == WheelFR || w == WheelFL) && !grounded) {
                    // let wheels free-spin if in the air, but apply some drag
                    rpm = Mathf.MoveTowards(w.rpm, 0, 1 * Time.fixedDeltaTime);
                }

                bool wheelBoost = false;
                if (w == WheelRR || w == WheelRL) {
                    wheelBoost = boosting;
                }
                w.UpdateWheelVisuals(
                    Vector3.Dot(rb.GetPointVelocity(w.transform.position),transform.forward),
                    rpm,
                    wheelBoost,
                    Drifting || ((w==WheelRR||w==WheelRL) && forwardTraction < 1f),
                    brakeGlow
                );
            }
        }

        // if not currently doing a burnout, increase grip again
        if (!burnout && !drifting) forwardTraction = Mathf.MoveTowards(forwardTraction, 1, 0.5f * Time.fixedDeltaTime);
        lcsLight.SetOff();
        burnout = false;
        if (WheelRR.Grounded || WheelRL.Grounded) {
            if (gas > 0 && ignition && currentGear != 0 && !clutch) {
                float mult = currentGear < 0 ? -1 : 1;
                mult *= boosting ? settings.nitroxBoost : 1;
                // if you can just barely toe the line with TCS, you don't get slowed down
                // otherwise you get slowed down a bit
                mult *= 1-tcsFrac*settings.tcsBraking;
                
                float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
                float wantedAccel = GetWantedAccelForGas(gas, forwardSpeed);
                if (wantedAccel > settings.burnoutThreshold) {
                    bool lcsBreak = ((wantedAccel-settings.burnoutThreshold) > settings.lcsLimit) && !usingKeyboard;

                    // if braking at low speed allow doing a burnout
                    lcsBreak |= gas > 0 && brake > 0 && !clutch;
                    lcsBreak |= lcsDisabled;
                    burnout = lcsBreak;

                    // don't disable LCS on disabling assists
                    // just to make a the car a little easier to drive
                    // maybe expose that in settings later on
                    if (forwardTraction == 1 && !lcsBreak && settings.lcs && !boosting && !drifting && !InputManager.Button(Buttons.HANDBRAKE)) {
                        lcsLight.SetOn();
                        gas = GetWantedGas(settings.burnoutThreshold * 0.9f);
                    } else {
                        forwardTraction = settings.burnoutThreshold/wantedAccel;
                        forwardTraction = Mathf.Pow(forwardTraction, 2);
                    }
                    if (burnout || hydroplaning) forwardTraction = 0.5f;
                }

                float forceMagnitude = engine.GetPower(engineRPM)*gas*mult;
                Vector3 forwardForce = transform.forward * forceMagnitude;

                forwardForce *= forwardTraction;
                rb.AddForceAtPosition(forwardForce, rearAxle);

                if (drifting) {
                    float lostForce = Vector3.Dot(forwardForce, -rb.velocity);
                    // if drive force is pushing the car against its velocity
                    // and therefore slowing it down (and if the car is moving forward)
                    if (lostForce > 0 && Vector3.Dot(rb.velocity, transform.forward) > 0) {
                        lostForce = Mathf.Abs(lostForce) + (forwardForce.magnitude / forwardTraction * settings.driftBoost);
                        rb.AddForce(lostForce * settings.driftBoost * Vector3.Project(rb.velocity, transform.right).normalized);
                    }
                }

				// on a burnout, push the rear end sideways
                float sidewaysVelocity = Vector3.Project(rb.velocity, transform.right).sqrMagnitude;
                if (sidewaysVelocity > 0.01f) {
                    rb.AddForceAtPosition((1-forwardTraction) * Mathf.Sign(sidewaysVelocity) * forwardForce, rearAxle);
                }
            } else {
                if (engineRunning) rb.AddForce((engineRPM/engine.redline) * engine.engineBraking * -Vector3.Project(rb.velocity, transform.forward));
            }
        }

        if (brake > 0 && grounded) {
            Vector3 flatSpeed = Vector3.Project(rb.velocity, transform.forward);
            if ((flatSpeed.magnitude*u2mph) < 1) {
                rb.velocity -= flatSpeed;
            } else {
                rb.AddForce(brake * settings.brakeForce * -flatSpeed.normalized);
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
            currentGrip = Mathf.MoveTowards(currentGrip, 1f, 2f*Time.fixedDeltaTime);
        }
        
        if (grounded) {
            if (Drifting || hydroplaning) {
                // steering should also rotate the car's velocity when drifting
                float angleOffForward = Vector3.SignedAngle(rb.velocity, transform.forward, transform.up);
                // you can countersteer to avoid rotation
                angleOffForward *= Mathf.Clamp01(targetSteerAngle/settings.maxSteerAngle * Mathf.Sign(angleOffForward));
                // this should be drift control but we need something 0-1 bounded
                float driftVelocityChange = Mathf.Clamp01(angleOffForward * settings.driftBoost);
                // but if no gas, slide completely sideways
                driftVelocityChange *= gas;
                rb.velocity = Quaternion.AngleAxis(driftVelocityChange, transform.up) * rb.velocity;
            }

            if (WheelRL.Grounded || WheelRR.Grounded) {
                AddLateralForce(rearAxle, transform.right, false, true);
            }

            if (WheelFL.Grounded || WheelFR.Grounded) {
                AddLateralForce(frontAxle, Quaternion.AngleAxis(targetSteerAngle, transform.up) * transform.right, true, false);
                if ((hydroplaning || drifting) && rb.velocity.sqrMagnitude > 1f) {
                    // mass * distance / time^2
                    rb.AddTorque(settings.driftControl * settings.maxSteerAngle * steering * transform.up);
                }
            }

            float flatSpeed = Mathf.Abs(Vector3.Dot(rb.velocity, transform.forward)) * u2mph;
            if (flatSpeed < 0.2f) {
                wheelAudio.volume = 0;
            } else {
                wheelAudio.volume = 0.2f;
                wheelAudio.pitch = Mathf.Lerp(1, 3f, flatSpeed / 80f);
            }
            tireSkid.mute = hydroplaning;
            if (drifting || hydroplaning) {
                driftingTime += Time.fixedDeltaTime;
                string skillName = hydroplaning ? "Hydroplaning" : "Drift";
                Alert($"{skillName}\n+"+(driftingTime*settings.driftNitroGain).ToString("F0"), constant: true);
                nitroxMeter.Add(settings.driftNitroGain * Time.fixedDeltaTime);
            } else {
                driftingTime = 0;
            }
        } else {
            wheelAudio.volume = 0;
            tireSkid.mute = true;
            driftingTime = 0;
        }

        float dragForce = 0.5f * rb.velocity.sqrMagnitude * settings.drag * 0.1f;
        if (inWater && !hydroplaning) {
            dragForce *= 50f;
        }
        if (gas==0 || !ignition) dragForce = 0;
        rb.AddForce((-rb.velocity.normalized)*dragForce, ForceMode.Force);
        if (grounded) rb.AddForce(-dragForce * settings.downforceRatio * transform.up);

        UpdateTelemetry();

        // right the car if upside down
        if (Physics.Raycast(
            transform.position, 
            transform.up,
            5, 
            1 << LayerMask.NameToLayer("Ground")
        ) && !grounded && rb.velocity.sqrMagnitude < 10f) {
            rb.AddTorque(-75 * steering * transform.forward, ForceMode.Acceleration);
        } else if (!grounded) {
            UpdateAirControl();
        }
        groundedLastStep = grounded;

        hydroplaneNoise.mute = !hydroplaning && !(inWater && rb.velocity.sqrMagnitude > 5f);
        
        raceData.maxSpeed = Mathf.Max(raceData.maxSpeed, rb.velocity.magnitude);
        clutchOutThisFrame = false;
        clutchInThisFrame = false;
    }

    float GetEngineRPMFromSpeed(float flatSpeed) {
        return WheelRR.GetWheelRPMFromSpeed(flatSpeed)
            * Mathf.Sign(currentGear)
            * engine.diffRatio
            * engine.gearRatios[Mathf.Abs(currentGear)-1];
    }

    float GetWheelRPMFromEngineRPM(float engineRPM) {
        return engineRPM / engine.diffRatio / engine.gearRatios[Mathf.Abs(currentGear-1)] * Mathf.Sign(currentGear);
    }

    void UpdateEngine() {
        float flatSpeed = Vector3.Dot(rb.velocity, transform.forward);
        if (!engineRunning) {
            engineRPM = Mathf.MoveTowards(engineRPM, 0, (engine.engineBraking*(engineRPM/engine.redline)+8000f) * Time.fixedDeltaTime);
            if (engineStarting) {
                engineRPM = 1500 + Mathf.Sin(Time.time*64) * 200;
            }
        } else {
            float targetRPM = Mathf.Max(engine.idleRPM + Mathf.Sin(Time.time*64)*50f, ignition ? gas*engine.redline : 0);
            float moveSpeed = ignition ? engine.GetThrottleResponse(engineRPM) : (engine.engineBraking*(engineRPM/engine.redline)*3+1000f);
            float idealEngineRPM = Mathf.MoveTowards(engineRPM, targetRPM, moveSpeed * Time.fixedDeltaTime);
            if (currentGear != 0 && !clutch) {
                if (fullAutomatic) {
                    if (engineRPM > engine.redline-200) {
                        StartCoroutine(AutoGearUp());
                    } else if (engineRPM < engine.stallRPM+200) {
                        StartCoroutine(AutoGearDown());
                    }
                }

                engineRPMFromSpeed = grounded ? GetEngineRPMFromSpeed(flatSpeed) : engineRPM;
                if (clutchOutThisFrame) {
                    float rpmDiff = engineRPMFromSpeed - engineRPM;
                    if (rpmDiff < 0) {
                        // you can shift bad from first gear if you're at max power
                        if (rpmDiff < -settings.maxRPMDiff && (currentGear != 1 || !engine.PeakPower(idealEngineRPM))) {
                            // this is the general bad shift
                            impulseSource.GenerateImpulse();
                            StartCoroutine(GearLurch());
                            clutchRatio = 0.1f;
                            if (currentGear == 1 && gas > 0) forwardTraction = 0.1f;
                        } else if (Mathf.Abs(currentGear) == 1 && engine.PeakPower(idealEngineRPM) && Vector3.Dot(rb.velocity, transform.forward) * u2mph < 5f) {
                            // this is the max power shift launch
                            // keep the clutch ratio soft at first to avoid a money shift on launch
                            PerfectShift(rpmDiff, alert: false);
                            Alert("perfect launch \n+" + (int) engine.maxPower*5);
                            achievements.Get("Peak Power");
                            nitroxMeter.Add(engine.maxPower*5);
                            clutchRatio = 1f;
                            rb.AddForce(settings.launchBoost * mph2u * Mathf.Sign(currentGear) * transform.forward, ForceMode.VelocityChange);
                        } else if (currentGear > 1 && Vector3.Dot(rb.velocity, transform.forward) * u2mph > 1f) {
                            PerfectShift(rpmDiff);
                        }
                    } else {
                        if (Random.Range(0f, 1f) < 0.8f) {
                            exhaustAnimator.SetTrigger("Backfire");
                        }
                        if (rpmDiff > settings.maxRPMDiff) {
                            clutchRatio = 0.5f;
                            impulseSource.GenerateImpulse();
                            StartCoroutine(GearLurch());
                        } else if (idealEngineRPM < engine.redline+500) {
                            // no perfect shift on the money shift
                            PerfectShift(rpmDiff);
                        }
                    }
                }
                engineRPM = Mathf.Lerp(
                    idealEngineRPM,
                    engineRPMFromSpeed,
                    grounded ? clutchRatio : 0
                );

                // then if there's wheelspin on the ground, move more towards what the engine actually wants
                if (grounded) {
                    engineRPM = Mathf.Lerp(
                        idealEngineRPM,
                        engineRPM,
                        Mathf.Max(forwardTraction, 0.2f) * clutchRatio
                    );
                }
            } else if (currentGear == 0 || clutch) {
                engineRPM = idealEngineRPM;
            }
        }

        clutchRatio = Mathf.MoveTowards(clutchRatio, 1f, engine.clutchSharpness * Time.fixedDeltaTime);

        engineAnimator.speed = engineRPM == 0 ? 0 : 1 + (engineRPM/engine.redline);
        if (engineRPM > engine.redline-100) {
            if (fullAutomatic || automatic && currentGear < engine.gearRatios.Count && !clutch) {
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
                if (Mathf.Abs(currentGear) == 1 && gas > 0.3f) {
                    engineRPM = engine.stallRPM;
                } else if (fullAutomatic || GameOptions.PaddleShift) {
                    engineRPM = engine.stallRPM;
                } else {
                    checkEngine.Flash();
                    StallEngine();
                }
            } else if (engineRPM > engine.redline+500) {
                if (automatic && currentGear < engine.gearRatios.Count && !clutch) {
                    ChangeGear(currentGear + 1);
                } else if (!boosting) {
                    // Money Shift achievement
                    achievements.Get("Money Shift");
                    // actually throw the car forwards
                    rb.AddRelativeTorque(160, 0, 0, ForceMode.Acceleration);
                    transmissionTemp.Flash();
                    StallEngine();
                }
            }
        }

        engineAudio.SetRPMAudio(
            GetRPMAudioPoint(),
            gas,
            ignition,
            clutch, 
            Mathf.Abs(Vector3.Dot(rb.velocity, transform.forward)) * u2mph
        );
        UpdateEngineLowPass();
        carBody.transform.localPosition = new Vector3(0, engineRPM/engine.redline * 0.025f, 0);
    }

    float GetWantedAccel(float sa, Vector3 flatVelocity) {
        float theta = 90f + sa + Vector3.SignedAngle(flatVelocity, transform.forward, transform.up);
        float manualDot = flatVelocity.magnitude * Mathf.Cos(theta*Mathf.Deg2Rad);
        float wantedAccel = manualDot * settings.GetTireSlip(Vector3.Dot(flatVelocity, transform.forward)) / Time.fixedDeltaTime;
        return wantedAccel * -1;
    }

    float GetWantedSteeringAngle(float wantedAccel, Vector3 flatVelocity) {
        float manualDot = wantedAccel * Time.fixedDeltaTime / settings.GetTireSlip(Vector3.Dot(flatVelocity, transform.forward));
        if (flatVelocity.magnitude < Mathf.Epsilon) return 0;
        float cosThetaRad = manualDot / flatVelocity.magnitude;
        cosThetaRad = Mathf.Clamp(cosThetaRad, -1f, 1f);
        float theta = Mathf.Acos(cosThetaRad) * Mathf.Rad2Deg;
        float sa = theta + 90f + Vector3.SignedAngle(flatVelocity, transform.forward, transform.up);
        sa = (sa-180) * -1;
        sa = Mathf.Clamp(sa, -settings.maxSteerAngle, settings.maxSteerAngle);
        return sa;
    }
    
    float GetWantedAccelForGas(float gas, float forwardSpeed) {
        float mult = 1-tcsFrac*settings.tcsBraking;
        float forceMagnitude = engine.GetPower(engineRPM)*gas*mult;
        float wantedSpeed = forwardSpeed + (forceMagnitude * Time.fixedDeltaTime / rb.mass);
        float diff = (wantedSpeed - forwardSpeed) / Time.fixedDeltaTime;
        return diff;
    }

    float GetWantedGas(float wantedAccel) {
        float requiredForce = wantedAccel * rb.mass;
        if (requiredForce <= 0) {
            return 0f;
        }
        float mult = 1 - tcsFrac * settings.tcsBraking;
        if (mult <= 0) return 0;
        float maxEnginePower = engine.GetPower(engineRPM);
        if (maxEnginePower <= 0) return 0;
        float wantedGas = requiredForce / (maxEnginePower * mult);
        return Mathf.Clamp01(wantedGas);
    }


    void UpdateSteering() {
        targetSteerAngle = steering * settings.maxSteerAngle;
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        usingWheel = ReInput.controllers.GetLastActiveController().ImplementsTemplate<RacingWheelTemplate>();
        usingKeyboard = ReInput.controllers.GetLastActiveController().type != ControllerType.Joystick;
        usingController = !usingWheel && !usingKeyboard;
        float steeringMult = usingWheel ? 1 : settings.steerLimitCurve.Evaluate(Mathf.Abs(forwardSpeed));
        if (steering == 0) {
            targetSteerAngle = 0;
        }
        targetSteerAngle *= steeringMult;
        // keep assists off for half a second after tapping handbrake
        // user might tap handbrake without being at full lock/drift inducing steering angle
        // so this extra 0.5s allows them to "buffer" a drift
        bool handbrakeInput = InputManager.Button(Buttons.HANDBRAKE) || Time.time<handbrakeDown+0.5f;
        gripEffectStrength = 0;
        if (settings.tcs && !assistDisabled && !handbrakeInput && !drifting && grounded && forwardSpeed>1f && forwardTraction==1f && !hydroplaning) {
            Vector3 axleVelocity = rb.GetPointVelocity(frontAxle);
            Vector3 flatVelocity = Vector3.ProjectOnPlane(axleVelocity, transform.up);
            float wantedAccel = GetWantedAccel(targetSteerAngle, flatVelocity);

            if (Mathf.Abs(wantedAccel) > settings.maxCorneringAccel) {
                targetSteerAngle = GetWantedSteeringAngle(settings.maxCorneringAccel * 0.9f * Mathf.Sign(wantedAccel), flatVelocity);
                // set it to how much over the max cornering force you are
                tcsFrac = 1 - Mathf.Clamp01((Mathf.Abs(wantedAccel)-settings.maxCorneringAccel)/settings.maxCorneringAccel);
                tcsLight.SetOn();
                gripEffectStrength = 0;
            } else {
                tcsFrac = 0;
                float accelStrength = Mathf.Abs(wantedAccel);
                if (accelStrength>settings.maxCorneringAccel*settings.gripLimitThreshold && accelStrength<settings.maxCorneringAccel) {
                    gripEffectStrength = 1;
                    timeAtEdge += Time.fixedDeltaTime;
                    if (timeAtEdge > 0.2f) {
                        Alert("Grip limit\n+"+(timeAtEdge*settings.edgeNitroGain).ToString("F0"), constant: true);
                        nitroxMeter.Add(settings.edgeNitroGain * Time.fixedDeltaTime);
                    }
                } else {
                    timeAtEdge = 0;
                }
                tcsLight.SetOff();
            }
        } else {
            timeAtEdge = 0;
            tcsFrac = 0;
            tcsLight.SetOff();
        }
        gripEffect.SetFloat("PowerLight", Mathf.MoveTowards(gripEffect.GetFloat("PowerLight"), gripEffectStrength, 5f * Time.fixedDeltaTime));

        currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteerAngle, (usingWheel ? 1000 : settings.steerSpeed) * Time.fixedDeltaTime);
        Quaternion targetRotation = Quaternion.Euler(0, currentSteerAngle, 0);
        WheelFL.transform.localRotation = targetRotation;
        WheelFR.transform.localRotation = targetRotation;
    }

    float AddLateralForce(Vector3 point, Vector3 lateralNormal, bool steeringAxle, bool driveAxle) {
        Vector3 flatVelocity = Vector3.ProjectOnPlane(rb.GetPointVelocity(point), transform.up);
        if (flatVelocity.sqrMagnitude < Mathf.Epsilon) {
            return 0;
        }
        float lateralSpeed = Vector3.Dot(flatVelocity, lateralNormal);
        float wantedAccel = -lateralSpeed * settings.GetTireSlip(Vector3.Dot(flatVelocity, transform.forward)) / Time.fixedDeltaTime;
        if (steeringAxle) {
            if (Mathf.Abs(wantedAccel) > settings.maxCorneringAccel && !hydroplaning) {
                if (!drifting) raceData.driftStartPos = transform.position;
                drifting = true;
            } else {
                if (drifting) {
                    float driftDistance = Vector3.Distance(transform.position, raceData.driftStartPos);
                    raceData.longestDrift = Mathf.Max(driftDistance, raceData.longestDrift);
                }
                drifting = false;
            }
            if (drifting) {
                carBody.driftRoll = 5f * Mathf.Sign(Vector3.Dot(rb.velocity, transform.right));
                currentGrip = 0.5f / (Mathf.Abs(wantedAccel) / settings.maxCorneringAccel);
            } else {
                carBody.driftRoll = 0f;
            }
            if (hydroplaning) {
                currentGrip = settings.hydroplaneGrip;
            }
            float gs = ToGs(wantedAccel);
            gForceIndicator.rectTransform.localScale = new Vector3(gs, 1, 1);
            gForceText.text = Mathf.Abs(gs).ToString("F2") + " lateral G";
        } else if (driveAxle) {
            currentGrip *= forwardTraction;
        }
        wantedAccel *= currentGrip;
        Vector3 tireForce = lateralNormal*wantedAccel;
        rb.AddForceAtPosition(tireForce, point, ForceMode.Acceleration);
        float slowdownForce = Vector3.Project(tireForce, -flatVelocity).magnitude;
        if (drifting) {
            Vector3 v = (ignition ? gas : 0) * settings.driftBoost * slowdownForce * transform.forward;
            v = Quaternion.AngleAxis(targetSteerAngle, transform.up) * v;
            rb.AddForceAtPosition(v, point, ForceMode.Acceleration);
        }
        return wantedAccel;
    }

    void PerfectShift(float rpmDiff, bool alert=true) {
        if (lastGear == currentGear) return;
        if (engineRPM + rpmDiff > engine.redline+500) return;
        if (GameOptions.PaddleShift) return;
        perfectShiftEffect.SetTrigger("Trigger");
        perfectShiftAudio.pitch = 1 + Random.Range(-0.15f, 0.15f);
        perfectShiftAudio.Play();
        rpmDiff = Mathf.RoundToInt(rpmDiff);
        string t = "";
        if (lastGear > currentGear) {
            t = "revmatched downshift";
            if (brake > 0) {
                achievements.Get("Heel And Toe");
            }
        } else if (lastGear < currentGear) {
            t = "revmatched upshift";
        }
        int bonus = (int) Mathf.Clamp(1000-Mathf.Abs(rpmDiff), 10, 1000);
        if (alert) {
            Alert(t + "\n+" + bonus);
            nitroxMeter.Add(bonus);
        }
        if (!automatic) {
            float amt = 1 - (Mathf.Abs(rpmDiff) / settings.maxRPMDiff);
            if (amt < 0) amt = 1; // perfect launch
            raceData.goodShiftAmount += amt;
        }
    }

    void Alert(string text, bool constant=false) {
        alertText.Alert(text, constant);
    }

    public void ChangeGear(int to, bool shifter = false) {
        gearshiftAudio.PlayOneShot(engine.gearShiftNoises[Random.Range(0, engine.gearShiftNoises.Count)]);
        currentGear = to;
    }

    IEnumerator GearLurch() {
        carBody.maxXAngle *= 2f;
        if (grounded && lastGear == currentGear && gas > 0 && rb.velocity.sqrMagnitude > 2f) {
            ClutchKick();
        } else {
            shiftLurch = true;
        }
        assistDisabled = true;
        // allow burnouts on first gear but maybe don't do it in others
        if (currentGear == 1) lcsDisabled = true;
        yield return new WaitForSeconds(settings.gearShiftTime);
        assistDisabled = false;
        lcsDisabled = false;
        shiftLurch = false;
        carBody.maxXAngle /= 2f;
    }

    void ClutchKick() {
        // push the car sideways based on gas, in the direction it's sliding
        if (Mathf.Abs(steering) > 0.5f) {
            Alert("clutch kick\n+"+settings.driftNitroGain);
            nitroxMeter.Add(settings.driftNitroGain);
        }
        float angleOffForward = Vector3.SignedAngle(transform.forward, rb.velocity, transform.up);
        float slideDirection = angleOffForward > 0 ? 1 : -1;
        if (Mathf.Abs(angleOffForward) > 15) {
            rb.AddForce(engine.maxPower * slideDirection * gas * 0.5f * transform.right, ForceMode.Impulse);
        }
    }

    void StallEngine() {
        achievements.Get("Needs More Gas");
        StartCoroutine(StallRock());
        engineAudioSource.PlayOneShot(engine.stallNoise);
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

    void UpdateAirControl() {
        // halt spinning except a flat spin
        if (groundedLastStep && !grounded) {
            Vector3 spin = rb.angularVelocity;
            Vector3 flatSpin = Vector3.Project(spin, transform.up);
            spin -= flatSpin;
            rb.AddTorque(-spin);
        }

        Vector3 v = rb.angularVelocity;
        // v.y is flat spin
        v.y += steering * (1f/settings.airSpinAccel) * Time.fixedDeltaTime;
        v.y = Mathf.Clamp(v.y, -settings.airSpinMaxSpeed, settings.airSpinMaxSpeed);

        // v.x is pitch y
        v.x += InputManager.GetAxis(Buttons.CAM_Y) * (1f/settings.airSpinAccel) * Time.fixedDeltaTime;
        v.x = Mathf.Clamp(v.x, -settings.airSpinMaxSpeed, settings.airSpinMaxSpeed);

        // v.z is pitch x, rolling side to side
        v.z -= InputManager.GetAxis(Buttons.CAM_X) * (1f/settings.airSpinAccel) * Time.fixedDeltaTime;
        v.z = Mathf.Clamp(v.z, -settings.airSpinMaxSpeed, settings.airSpinMaxSpeed);

        // then slowly reset to velocity 0
        if (steering == 0) v.y = Mathf.MoveTowards(v.y, 0, 1f/settings.airSpinAccel * Time.fixedDeltaTime);
        if (InputManager.GetAxis(Buttons.CAM_Y) == 0) v.x = Mathf.MoveTowards(v.x, 0, 1f/settings.airSpinAccel * Time.fixedDeltaTime);
        if (InputManager.GetAxis(Buttons.CAM_X) == 0) v.z = Mathf.MoveTowards(v.z, 0, 1f/settings.airSpinAccel * Time.fixedDeltaTime);
        
        rb.angularVelocity = v;

        if (InputManager.ButtonDown(Buttons.HANDBRAKE) && !forceBrake) {
            rb.angularVelocity = Vector3.zero;
        }
        if (Time.time > handbrakeDown + 0.25f && !forceBrake) {
            rb.AddForce(-Vector3.Project(rb.velocity, Vector3.up)*0.2f);
        }
    }

    void UpdateEngineLowPass() {
        Vector3 towardsCamera = mainCamera.transform.position - engineAudio.transform.position;
        float cameraEngineAngle = Vector3.Angle(transform.forward, Vector3.ProjectOnPlane(towardsCamera, transform.up));
        if (cameraEngineAngle < 90) {
            engineAudioSource.outputAudioMixerGroup.audioMixer.SetFloat("ExhaustLowPassCutoff", 6000);
        } else {
            cameraEngineAngle -= 90f;
            engineAudioSource.outputAudioMixerGroup.audioMixer.SetFloat("ExhaustLowPassCutoff", Mathf.Lerp(6000, 20000, cameraEngineAngle/90f));
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
        if (
            Time.unscaledTime < spawnTime + 0.5f
            || Time.timeScale != 1
            || !GameOptions.Rumble
            || !grounded
            || !usingController
            || finished
        ) {
            InputManager.player.StopVibration();
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
			if (w.GetCompressionRatio() > 1.5f) {
				bumpTS = Time.unscaledTime;
			}
		}
		if (Time.unscaledTime < bumpTS + 0.1f) {
			bumpVibration = 1f;
		} else {
			bumpVibration = 0;
		}
        bumpVibration = 0;

        if (boosting) {
            bumpVibration += 0.5f;
        }

        if (shiftLurch) {
            bumpVibration = 1f;
        }

        if (Mathf.Abs(engineRPM - engine.idleRPM) < 50f) {
            startVibration += 0.1f + Mathf.Clamp01(Mathf.Sin(Time.time*16f)-0.5f);
        }
        
		InputManager.player.SetVibration(0, startVibration+bumpVibration);
        InputManager.player.SetVibration(1, startVibration+rpmVibration);
    }

    void UpdateCameraVibration() {
        // create a repeated impulse on the channel for the tracking camera listeners
        // then have it based on current speed (20-100mph)
        if (Time.time > vibrationTS + 0.05f) {
            trackingCamImpulseSource.GenerateImpulse(Vector3.up * Mathf.Lerp(
                0,
                0.02f,
                Mathf.Abs(Vector3.Dot(rb.velocity, rb.transform.forward)) * u2mph / 100f
            ));
            vibrationTS = Time.time;
        }
    }

    float GetRPMAudioPoint() {
        if (!engineRunning) return 0;
        float rpm = engineRPM;
        if (currentGear > lastGear) {
            // wobble the engine noise a little bit based on driveline flex
            float timeSinceClutchUp = Time.time - clutchOutTime;
            // decrease over time
            float mult = Mathf.Clamp(1 - (timeSinceClutchUp / 0.8f), 0, 1);
            // wobble more at lower gears
            mult *= 1 - (Mathf.Abs(currentGear)/engine.gearRatios.Count);
            rpm += mult * settings.drivelineFlex * Mathf.Sin(Time.time * 48f) * settings.drivelineFlex * 100 * gas;
        }
        return rpm;
    }

    public CarSnapshot GetSnapshot() {
        return new CarSnapshot(
            transform.position,
            transform.rotation, 
            engineRPM,
            targetSteerAngle,
            gas,
            drifting,
            boosting,
            brake > 0
        );
    }

    public void SetDashboardEnabled(bool b) {
        if (!b) dashboardDisableSources++;
        else dashboardDisableSources--;
        dashboardDisableSources = Mathf.Max(dashboardDisableSources, 0);

        b = dashboardDisableSources == 0;

        foreach (Canvas c in dashboardUI) {
            c.enabled = b;
        }
    }

    public void ShutoffEngine() {
        engineRunning = false;
    }

    public void Respawn() {
        StopCoroutine(respawnRoutine);
        respawnRoutine = RespawnRoutine();
        StartCoroutine(respawnRoutine);
    }

    IEnumerator RespawnRoutine() {
        raceData = new();
        nitroxMeter.Reset();
        finished = false;
        currentGear = 0;
        engineRPM = engine.idleRPM;
        engineRunning = false;
        drifting = false;
        boosting = false;
        hydroplaning = false;
        foreach (Wheel w in wheels) {
            w.UpdateWheelVisuals(
                Vector3.Dot(rb.GetPointVelocity(w.transform.position),transform.forward),
                0,
                false,
                false,
                0
            );
            w.ClearTrails();
            w.inWater = false;
        }
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
		rb.position = startPoint;
        rb.rotation = startRotation;
        transform.SetPositionAndRotation(startPoint, startRotation);
        yield return new WaitForEndOfFrame();
        engineRunning = true;
        onRespawn.Invoke();
    }

    public void ApplyWheel(CustomWheel c) {
        foreach (Wheel w in wheels) {
            w.ApplyCustomWheel(c);
        }
    }

    IEnumerator AutoGearUp() {
        if (changingGear) yield break;
        changingGear = true;
        forceClutch = true;
        gearshiftFuelCutoff = true;
        yield return new WaitForSeconds(settings.gearShiftTime);
        if (currentGear < engine.gearRatios.Count) {
            ChangeGear(currentGear+1);
        }
        float flatSpeed = Vector3.Dot(rb.velocity, transform.forward);
        engineRPM = grounded ? GetEngineRPMFromSpeed(flatSpeed) : engineRPM;
        forceClutch = false;
        gearshiftFuelCutoff = false;
        changingGear = false;
    }

    IEnumerator AutoGearDown() {
        if (changingGear) yield break;
        changingGear = true;
        forceClutch = true;
        yield return new WaitForSeconds(settings.gearShiftTime * 0.5f);
        if (currentGear > -1) {
            ChangeGear(currentGear-1);
        }
        float flatSpeed = Vector3.Dot(rb.velocity, transform.forward);
        engineRPM = grounded ? GetEngineRPMFromSpeed(flatSpeed) : engineRPM;
        forceClutch = false;
        changingGear = false;
    }

    IEnumerator CollisionRecovery() {
        // don't get completely fucked if you hit a track wall
        rb.angularDrag = 10f;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        rb.angularDrag = 0.05f;
    }

    void OnValidFinish() {
        if (drifting) {
            float driftDistance = Vector3.Distance(transform.position, raceData.driftStartPos);
            raceData.longestDrift = Mathf.Max(driftDistance, raceData.longestDrift);
        }
        if (FindObjectOfType<RaceLogic>().raceType == RaceType.ROUTE) finished = true;
    }

    public RaceData GetRaceData() {
        return raceData;
    }
}
