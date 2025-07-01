using System.Collections.Generic;
using UnityEngine;

public class DemoWheel : MonoBehaviour {
    public float mass = 20, radius = 0.5f, maxSpeed = 1000f, steerAngle, motorTorque, brakeTorque;
    public AnimationCurve powerCurve;
    public LayerMask collisionLayerMask;
    [Header ("Friction")]
    public float tireGripFactor = 1;

    [Header ("Suspension")]
    public float suspensionRestDist = .5f;
    public float springStrength, springDamper;

    [Header ("Gizmos")]
    public int numSegments = 32;
    public Color color = Color.white;
    public bool grounded;
    [HideInInspector] public float hitDistance;
    [HideInInspector] public Vector3 point;
    List<Wheel> wheels;
    Rigidbody rb;
    void Awake () {
        rb = GetComponentInParent<Rigidbody> ();
    }
    void Update () {
        SetWheelGrounded ();
    }

    void FixedUpdate () {
        if (grounded) {
            ApplySuspensionForce ();
            ApplySidewaysForce ();
            ApplyAccelerationForce ();
            ApplyBrakeForce ();
        }
    }

    void SetWheelGrounded () {
        RaycastHit hit;
        grounded = Physics.Raycast (transform.position, -transform.parent.up, out hit, radius, collisionLayerMask);
        point = grounded? hit.point : Vector3.one * float.NaN;
        hitDistance = hit.distance;
    }
    public void ApplySuspensionForce () {
        Vector3 springDir = transform.up;
        Vector3 tireWorldVel = rb.GetPointVelocity (transform.position);
        float offset = suspensionRestDist - hitDistance;
        float vel = Vector3.Dot (springDir, tireWorldVel);
        float force = (offset * springStrength) - (vel * springDamper);
        rb.AddForceAtPosition (springDir * force, transform.position);
    }

    void ApplySidewaysForce () {
        Vector3 tireWorldVel = rb.GetPointVelocity (transform.position);
        float steeringVel = Vector3.Dot (transform.right, tireWorldVel);
        float desiredVelChange = -steeringVel * tireGripFactor;
        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
        rb.AddForceAtPosition (transform.right * mass * desiredAccel, transform.position);
    }

    void ApplyAccelerationForce () {
        Vector3 accelDir = transform.forward;
        if (motorTorque != 0) {
            float carSpeed = Vector3.Dot (rb.transform.forward, rb.velocity);
            float normalizedSpeed = (Mathf.Clamp01 (Mathf.Abs (carSpeed) / maxSpeed));
            float availableTorque = powerCurve.Evaluate (normalizedSpeed) * motorTorque;
            rb.AddForceAtPosition (accelDir * availableTorque, transform.position);
        }
    }

    public void ApplyBrakeForce () {
        if (brakeTorque > 0) {
            Vector3 tireWorldVel = rb.GetPointVelocity (transform.position);
            Vector3 forwardDir = transform.forward;
            Vector3 brakeDir = -Vector3.Project (tireWorldVel, forwardDir.normalized).normalized;
            rb.AddForceAtPosition (brakeDir * brakeTorque, transform.position);
        }
    }

    private void OnDrawGizmos () {
        // Set the color for the circle
        Gizmos.color = color;

        // Calculate the position of the first point
        Vector3 startPoint = transform.position + transform.right * radius;

        // Calculate the rotation matrix to convert points to world space
        Matrix4x4 rotationMatrix = Matrix4x4.TRS (Vector3.zero, transform.rotation, Vector3.one);

        // Draw the circle
        for (int i = 1; i <= numSegments; i++) {
            // Calculate the angle of the current point
            float angle = i * 2.0f * Mathf.PI / numSegments;

            // Calculate the position of the current point in local space
            Vector3 localEndPoint = new Vector3 (0.0f, Mathf.Sin (angle), Mathf.Cos (angle)) * radius;

            // Convert the local point to world space
            Vector3 endPoint = rotationMatrix.MultiplyPoint3x4 (localEndPoint) + transform.position;

            // Draw a line between the previous point and the current point
            Gizmos.DrawLine (startPoint, endPoint);

            // Update the previous point to the current point
            startPoint = endPoint;
        }
    }
}
