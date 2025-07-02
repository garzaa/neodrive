using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Animations;

public class CameraRotate : MonoBehaviour {

    public float snapDistance = 0.5f;
    public List<Vector2> snapVectors;
    public Car car;

    Vector3 targetPos;
    float rotationAngle;
    Transform ring;

    Vector3 camVelocity = Vector3.zero;
    float rotationSpeed = 0;

    public float chaseSmoothTime = 0.05f;
    public float rotationSmoothTime = 0.1f;

    void Start() {
        ring = transform.GetChild(0);
    }

    void Update() {
        targetPos = car.transform.position;
        rotationAngle = Vector3.SignedAngle(-transform.forward, car.rb.velocity, Vector3.up);

        // if the car's barely moving, put it at the car's rear
        if (car.rb.velocity.sqrMagnitude < 0.2f) {
            rotationAngle = Vector3.SignedAngle(-transform.forward, -car.transform.forward, Vector3.up);
        }

        Vector2 v = InputManager.CameraStick();
        if (v.sqrMagnitude > 0) {
            for (int i=0; i<snapVectors.Count; i++) {
                if (Vector2.Distance(v, snapVectors[i]) < snapDistance) {
                    v = snapVectors[i];
                    break;
                }
            }
            // don't flip it around if the car starts moving backwards
            rotationAngle = Vector3.SignedAngle(-transform.forward, -car.transform.forward, Vector3.up);
            rotationAngle += Vector3.SignedAngle(Vector3.forward, new Vector3(v.x, 0, v.y), Vector3.up);
        }

        // later, if grounded, average the ground normals and tilt the camera up/down to account for that
        float y = Mathf.SmoothDampAngle(ring.localRotation.eulerAngles.y, rotationAngle, ref rotationSpeed, rotationSmoothTime);
        ring.localRotation = Quaternion.Euler(0, y, 0);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref camVelocity, chaseSmoothTime, maxSpeed: 500);
    }   
}
