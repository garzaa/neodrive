using UnityEngine;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine.Audio;

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

    Camera mainCam;

    float startTime;

    public GameObject photoModeCamera;
    bool photoMode = false;
    public AudioMixerSnapshot pausedAudio;
	public AudioMixerSnapshot unpausedAudio;

    void Start() {
        mainCam = Camera.main;
        startTime = Time.time;
        photoModeCamera.SetActive(false);
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
        if (InputManager.ButtonDown(Buttons.PHOTOMODE)) {
            // check if something else paused it
            if (!photoMode && Time.timeScale == 0) return;
            photoMode = !photoMode;
            Time.timeScale = photoMode ? 0 : 1;
            photoModeCamera.gameObject.SetActive(photoMode);
            AudioListener.volume = photoMode ? 0 : 1;
            if (photoMode) {
                pausedAudio.TransitionTo(0.5f);
            } else {
                unpausedAudio.TransitionTo(0.1f);
            }
            car.SetDashboardEnabled(!photoMode);
        }

        if (InputManager.ButtonDown(Buttons.CYCLE_CAMERA) && Time.timeScale > 0) {
            currentCamera += 1;
            currentCamera %= cameras.Count;
            CycleCamera();
        }

        if (InputManager.ButtonDown(Buttons.TOGGLE_TELEMETRY)) {
            mainCam.cullingMask ^= 1 << LayerMask.NameToLayer("Telemetry");
        }

        // don't move the camera around but allow holding its position
        if (InputManager.Button(Buttons.CAMERA)) {
            cameraStick = new Vector2(
                InputManager.GetAxis(Buttons.CAM_X),
                InputManager.GetAxis(Buttons.CAM_Y)
            ).normalized;
        }

        if (!InputManager.Button(Buttons.CAMERA)) {
            cameraStick = Vector2.zero;
        }

        targetPos = car.transform.position;
        if (car.boosting) {
            targetPos += -car.transform.forward;
        }
        rotationAngle = Vector3.SignedAngle(transform.forward, car.rb.velocity, Vector3.up);

        if (car.Drifting) {
            rotationAngle = Vector3.SignedAngle(transform.forward, car.transform.forward, Vector3.up);
        }

        // if the car's barely moving, put it at the car's rear
        if (car.rb.velocity.sqrMagnitude < 5 * Car.mph2u) {
            rotationAngle = Vector3.SignedAngle(transform.forward, car.transform.forward, Vector3.up);
        }

        if (!car.grounded) {
            // look towards velocity
            rotationAngle = Quaternion.LookRotation(car.rb.velocity, transform.up).eulerAngles.y;
        }

        if (cameraStick.sqrMagnitude > 0) {
            for (int i=0; i<snapVectors.Count; i++) {
                if (Vector2.Distance(cameraStick, snapVectors[i]) < snapDistance) {
                    cameraStick = snapVectors[i];
                    break;
                }
            }
            // don't flip it around if the car starts moving backwards
            rotationAngle = Vector3.SignedAngle(transform.forward, car.transform.forward, Vector3.up);
            rotationAngle += Vector3.SignedAngle(Vector3.forward, new Vector3(cameraStick.x, 0, cameraStick.y), Vector3.up);
        }

        // later, if grounded, average the ground normals and tilt the camera up/down to account for that
        float y = Mathf.SmoothDampAngle(ring.localRotation.eulerAngles.y, rotationAngle, ref rotationSpeed, rotationSmoothTime);
        if (Time.time < startTime + 0.5f) {
            y = rotationAngle;
        }
        ring.localRotation = Quaternion.Euler(0, y, 0);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref camVelocity, chaseSmoothTime, maxSpeed: 500);
        if (Time.time < startTime + 0.5f) {
            transform.position = targetPos;
        }
    }   
}
