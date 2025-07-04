using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarBody : MonoBehaviour {
    [Header("Rocking Settings")]
    [Tooltip("The maximum angle (in degrees) the object will rock")]
    [SerializeField] public float maxXAngle = 4f;
	public float maxYAngle = 4f;

    [Tooltip("How much the parent's X-axis velocity influences the target rocking angle.")]
    [SerializeField] private float xVelocityInfluence = 1f;
	[SerializeField] private float yVelocityInfluence = 1f;

    [Header("Spring Simulation Settings")]
    [Tooltip("The stiffness of the simulated spring. Higher values mean a stiffer spring.")]
    [SerializeField] private float springStiffnessX = 50f; // k in Hooke's Law
    [SerializeField] private float springStiffnessY = 50f; // k in Hooke's Law

    [Tooltip("The damping ratio of the simulated spring. 0 = no damping, 1 = critically damped, >1 = overdamped.")]
    [Range(0f, 2f)] // A common range for damping ratios
    [SerializeField] private float dampingRatio = 0.8f; // ξ (zeta)

    private Rigidbody parentRigidbody;
    private Quaternion initialLocalRotation;
    private float xAngle = 0f;
    private float xSpringVelocity = 0f;

	private float yAngle = 0f;
	private float ySpringVelocity = 0f;

	float velocityXLastStep, velocityYLastStep;

	float extraYForce;
	float wobbleTs = -999f;
	bool wobbling = false;

    void Awake()
    {
        // Get the parent Rigidbody component
        parentRigidbody = GetComponentInParent<Rigidbody>();

        // Store the initial local rotation of this object
        initialLocalRotation = transform.localRotation;

        if (parentRigidbody == null)
        {
            Debug.LogWarning("RockingObject: No Rigidbody found on parent or higher in the hierarchy. This script requires a parent Rigidbody to function.", this);
        }
    }

	public void StartWobbling() {
		wobbling = true;
		yAngle *= 9f;
		extraYForce = 1000;
	}

	public void StopWobbling() {
		wobbling = false;
		yAngle /= 9f;
		extraYForce = 0;
	}

    void FixedUpdate() {
        if (parentRigidbody == null)
        {
            // If no parent Rigidbody, there's nothing to react to.
            return;
        }

		if (wobbling) {
			if (Time.time - wobbleTs > 0.05f) {
				extraYForce *= -1;
				wobbleTs = Time.time;
			}
		}

        // Get the parent's velocity on the X-axis
        float parentVelocityX = Vector3.Dot(parentRigidbody.velocity, -transform.forward);
		parentVelocityX = (parentVelocityX - velocityXLastStep) / Time.fixedDeltaTime;

		float parentVelocityY = Vector3.Dot(parentRigidbody.velocity, transform.right);
		parentVelocityY = (parentVelocityY - velocityYLastStep) / Time.fixedDeltaTime;

        // Calculate the target rocking angle based on parent's velocity
        // A higher velocity will lead to a larger target angle, up to maxRockAngle
        // Negative to rock opposite to movement (e.g., move right, tilt left)
        float targetRockAngle = -parentVelocityX * xVelocityInfluence;

        // Clamp the target angle to the defined maximum
        targetRockAngle = Mathf.Clamp(targetRockAngle, -maxXAngle, maxXAngle);

        // --- Simulate Spring Physics ---
        // Calculate the displacement from the target position
        float displacement = xAngle - targetRockAngle;

        // Calculate the spring force (Hooke's Law: F = -k * x)
        float springForce = -springStiffnessX * displacement;

        // Calculate damping force (proportional to velocity: F_d = -c * v)
        // For critical damping, c = 2 * sqrt(k * m). Here, we assume mass 'm' is 1 for simplicity
        // and use dampingRatio to scale the damping.
        float dampingForce = -2f * Mathf.Sqrt(springStiffnessX) * dampingRatio * xSpringVelocity;

        // Total force (or acceleration, assuming unit mass)
        float totalAcceleration = springForce + dampingForce;

        // Update spring velocity
        xSpringVelocity += totalAcceleration * Time.fixedDeltaTime;

        // Update current rocking angle
        xAngle += xSpringVelocity * Time.fixedDeltaTime;

		targetRockAngle = -parentVelocityY * yVelocityInfluence;
		targetRockAngle = Mathf.Clamp(targetRockAngle, -maxYAngle, maxYAngle);
		displacement = yAngle - targetRockAngle;
		springForce = -springStiffnessY * displacement;
		dampingForce = -2f * Mathf.Sqrt(springStiffnessY) * dampingRatio * ySpringVelocity;
		totalAcceleration = springForce + dampingForce + extraYForce;
		ySpringVelocity += totalAcceleration * Time.fixedDeltaTime;
		yAngle += ySpringVelocity * Time.fixedDeltaTime;

        // Apply the rocking rotation to the object's local X-axis
        // The rotation is applied relative to its initial local rotation
        Quaternion newRotation = initialLocalRotation * Quaternion.Euler(-xAngle, 0, -yAngle);
        transform.localRotation = newRotation;

		velocityXLastStep = Vector3.Dot(parentRigidbody.velocity, -transform.forward);
		velocityYLastStep = Vector3.Dot(parentRigidbody.velocity, transform.right);
    }
}
