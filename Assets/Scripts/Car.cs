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

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfGravity.transform.localPosition;
        wheels = new Wheel[]{WheelFL, WheelFR, WheelRL, WheelRR};
    }

    void Update() {
        gas = InputManager.GetAxis(Buttons.GAS);
        brake = InputManager.GetAxis(Buttons.BRAKE);
        steering = InputManager.GetAxis(Buttons.STEER);
    }

    void FixedUpdate() {
        grounded = false;
        int groundedWheelCount = 0;
        Vector3 wheelCenter = Vector3.zero;
        foreach (Wheel w in wheels) {
            if (w.Grounded) {
                grounded = true;
                wheelCenter += w.transform.position;
                groundedWheelCount++;
            }
        }
        wheelCenter /= groundedWheelCount;

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
            int mult = InputManager.Button(Buttons.REVERSE) ? -1 : 1;
            Vector3 flatSpeed = Vector3.Project(rb.velocity, -transform.forward);
            if (Mathf.Abs(MPH(flatSpeed.magnitude)) < settings.maxSpeed) {
                rb.AddForce(-transform.forward * settings.accelForce*gas*mult);
            }
        }

        if (brake > 0 && grounded) {
            Vector3 flatSpeed = Vector3.Project(rb.velocity, -transform.forward);
            if (MPH(flatSpeed.magnitude) < 1) {
                rb.velocity -= flatSpeed;
            } else {
                rb.AddForce(-flatSpeed.normalized * brake * settings.brakeForce);
            }
        }

        UpdateSteering();
        if (grounded && false) {
            float flatSpeed = Vector3.Dot(rb.velocity, -transform.forward);
            float steeringDegrees = currentSteerAngle;
            // so the car wants to turn this much in degrees?

            float turnRate = steeringDegrees * (flatSpeed*settings.steeringMult) * Time.fixedDeltaTime;
            // ok now, calculate how much stress that would put on the car
            // and then modulate throttle/reduce skidding accordigly
            // compute deltav as direction change - vector subtraction (new - old) with rb2d velocity

            rb.MoveRotation(rb.rotation * Quaternion.Euler(0, turnRate, 0));
            rb.velocity = Quaternion.Euler(0, turnRate, 0) * rb.velocity;
            float lateralAccel = Vector3.Dot(rb.velocity - vLastFrame, transform.right);
            float gForce = (lateralAccel * rb.mass) / Mathf.Abs(Physics.gravity.y);
            gForceIndicator.rectTransform.localScale = new Vector3(gForce, 1, 1);
            gForceText.text = gForce.ToString("F2");

            // then if not sliding, eliminate sideways velocity
            Vector3 sidewaysSpeed = Vector3.Project(rb.velocity, transform.right);
            rb.velocity -= sidewaysSpeed;

            if (flatSpeed < 0.1f) {
                // instead of doing this, slowly reduce its magnitude
                // and account for steering and all that shit
                rb.angularVelocity = Vector3.zero;
            }

            // goddamn it also has angular velocity after moving around. stop that somehow
        }

        if (grounded) {
            // ok so try the horizontal acceleration at the steering column point
            if (WheelFL.Grounded || WheelFR.Grounded) {
                float steeringDegrees = currentSteerAngle;
                float lateralSpeed = Vector3.Dot(rb.GetPointVelocity(frontAxle), Quaternion.Euler(0, steeringDegrees, 0) * transform.right);
                // the wheels want to halt all sideways velocity
                // this will slow down the drift later, but no worries for now, we know how much we're losing
                float lateralAccel = -lateralSpeed / Time.fixedDeltaTime;
                float lateralForce = lateralAccel * rb.mass;
                rb.AddForceAtPosition(transform.right * lateralForce * 0.1f, frontAxle);
            }

            if (WheelRL.Grounded || WheelRR.Grounded) {
                float lateralSpeed = Vector3.Dot(rb.GetPointVelocity(rearAxle), transform.right);
                // the wheels want to halt all sideways velocity
                // hmm, why does this need to be halved to not go insane
                float lateralAccel = -lateralSpeed * 0.5f / Time.fixedDeltaTime;
                float lateralForce = lateralAccel * rb.mass;
                rb.AddForceAtPosition(transform.right * lateralForce, rearAxle);
                
                float gs = lateralAccel / Mathf.Abs(Physics.gravity.y);
                gForceIndicator.rectTransform.localScale = new Vector3(gs, 1, 1);
                gForceText.text = Mathf.Abs(gs).ToString("F2") + " lateral G";
            }
        }
        
        
        posLastFrame = rb.position;
        vLastFrame = rb.velocity;
        UpdateTelemetry();
    }

    void UpdateTelemetry() {
        speedText.text = MPH(rb.velocity.magnitude).ToString("F0");
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
}
