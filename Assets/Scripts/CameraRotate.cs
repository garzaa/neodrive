using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Animations;
using Cinemachine;

public class CameraRotate : MonoBehaviour {

    public float snapDistance = 0.5f;
    public List<Vector2> snapVectors;
    public Car car;

    Vector3 targetPos;

    float rotationAngle;
    Vector2 cameraStick;
    public Transform ring;

    Vector3 camVelocity = Vector3.zero;
    float rotationSpeed = 0;

    public float chaseSmoothTime = 0.05f;
    public float rotationSmoothTime = 0.1f;

    public List<CinemachineVirtualCamera> cameras;
    int currentCamera = 0;

    float clutchReleased = -999;

    Camera mainCam;

    void Start() {
        mainCam = Camera.main;
        CycleCamera();
    }

    void CycleCamera() {
        for (int i=0; i<cameras.Count; i++) {
            if (i == currentCamera) {
                cameras[i].enabled = true;
            } else {
                cameras[i].enabled = false;
            }
        }
    }

    void Update() {
        if (InputManager.ButtonDown(Buttons.CYCLE_CAMERA)) {
            currentCamera += 1;
            currentCamera %= cameras.Count;
            CycleCamera();
        }

        if (InputManager.ButtonUp(Buttons.CLUTCH)) {
            clutchReleased = Time.unscaledTime;
        }

        if (InputManager.ButtonDown(Buttons.TOGGLE_TELEMETRY)) {
            mainCam.cullingMask ^= 1 << LayerMask.NameToLayer("Telemetry");
        }

        // don't move the camera around but allow holding its position
        if (!InputManager.Button(Buttons.CLUTCH)) {
            cameraStick = new Vector2(
                InputManager.GetAxis(Buttons.CAM_X),
                InputManager.GetAxis(Buttons.CAM_Y)
            ).normalized;
        }

        // if they start pushing the stick a frame before pressing the clutch, 
        // don't lock them into a bad camera angle for the next half-second
        if (cameraStick.sqrMagnitude < 0.9f || Time.unscaledTime < clutchReleased+0.5f) {
            cameraStick = Vector2.zero;
        }

        targetPos = car.transform.position;
        rotationAngle = Vector3.SignedAngle(-transform.forward, car.rb.velocity, Vector3.up);

        if (car.Drifting) {
            rotationAngle = Vector3.SignedAngle(-transform.forward, car.forwardVector, Vector3.up);
            // TODO: rotate based on the drift direction, slowly move towards it? very subtle rotation
        }

        // if the car's barely moving, put it at the car's rear
        if (car.rb.velocity.sqrMagnitude < 0.2f) {
            rotationAngle = Vector3.SignedAngle(-transform.forward, -car.transform.forward, Vector3.up);
        }

        if (cameraStick.sqrMagnitude > 0) {
            for (int i=0; i<snapVectors.Count; i++) {
                if (Vector2.Distance(cameraStick, snapVectors[i]) < snapDistance) {
                    cameraStick = snapVectors[i];
                    break;
                }
            }
            // don't flip it around if the car starts moving backwards
            rotationAngle = Vector3.SignedAngle(-transform.forward, -car.transform.forward, Vector3.up);
            rotationAngle += Vector3.SignedAngle(Vector3.forward, new Vector3(cameraStick.x, 0, cameraStick.y), Vector3.up);
        }

        // later, if grounded, average the ground normals and tilt the camera up/down to account for that
        float y = Mathf.SmoothDampAngle(ring.localRotation.eulerAngles.y, rotationAngle, ref rotationSpeed, rotationSmoothTime);
        ring.localRotation = Quaternion.Euler(0, y, 0);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref camVelocity, chaseSmoothTime, maxSpeed: 500);
    }   
}
