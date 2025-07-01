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
        if (grounded) {
            // TODO: combine the axis based steering into one function
            if (WheelFL.Grounded || WheelFR.Grounded) {
                float steeringDegrees = currentSteerAngle;
                float lateralSpeed = Vector3.Dot(rb.GetPointVelocity(frontAxle), Quaternion.Euler(0, steeringDegrees, 0) * transform.right);
                // the wheels want to halt all sideways velocity
                // ok this is actually globally
                float lateralAccel = -lateralSpeed * 0.5f / Time.fixedDeltaTime;
                rb.AddForceAtPosition(Quaternion.Euler(0, steeringDegrees, 0) * transform.right * lateralAccel * rb.mass, frontAxle);
            }

            if (WheelRL.Grounded || WheelRR.Grounded) {
                float lateralSpeed = Vector3.Dot(rb.GetPointVelocity(rearAxle), transform.right);
                // the wheels want to halt all sideways velocity
                // hmm, why does this need to be halved to not go insane
                float lateralAccel = -lateralSpeed * 0.5f / Time.fixedDeltaTime;
                rb.AddForceAtPosition(transform.right * lateralAccel * rb.mass, rearAxle);
                
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
