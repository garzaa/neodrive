using UnityEngine;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine.Audio;

public class CameraRotate : SavedObject {

    public float snapDistance = 0.5f;
    public List<Vector2> snapVectors;
    Car car;

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

    GameObject photoModeCamera;
    bool photoMode = false;
    public AudioMixerSnapshot pausedAudio;
	public AudioMixerSnapshot unpausedAudio;

    bool snapping = false;

    Quaternion rotateVelocity;

    int prevCam;
    bool allowChange = true;

    float respawnTime;

    float baseFOV;
    float targetFOV;

    int favoriteCamera = 0;

    bool chaseCamNearGround;
    Vector3 targetOffset;

    float y, x;

	protected override void LoadFromProperties() {
		favoriteCamera = Get<int>(nameof(favoriteCamera));
        currentCamera = favoriteCamera;
        CycleCamera();
	}

	protected override void SaveToProperties(ref Dictionary<string, object> properties) {
		properties[nameof(favoriteCamera)] = favoriteCamera;
	}

    override protected void Initialize() {
        car = FindObjectOfType<Car>();
        cameras[1] = car.transform.Find("BodyMesh/HoodCamera").GetComponent<CinemachineVirtualCamera>();
        mainCam = Camera.main;
        mainCam.depthTextureMode = DepthTextureMode.Depth;
        photoModeCamera = FindObjectOfType<PhotoModeCamera>(includeInactive: true).gameObject;
        photoModeCamera.SetActive(false);
        car.onRespawn.AddListener(OnRespawn);
        respawnTime = Time.time;
        baseFOV = cameras[0].m_Lens.FieldOfView;
    }

    void CycleCamera() {
        if (!allowChange) return;
        for (int i=0; i<cameras.Count; i++) {
            if (i == currentCamera) {
                cameras[i].enabled = true;
            } else {
                cameras[i].enabled = false;
            }
        }

        // save whether it's either the chase or hood camera
        if (currentCamera==0 || currentCamera==1) {
            favoriteCamera = currentCamera;
        }
    }

    void SetCamera(int idx) {
        for (int i=0; i<cameras.Count; i++) {
            if (i == idx) {
                cameras[i].enabled = true;
            } else {
                cameras[i].enabled = false;
            }
        }
    }

    void LateUpdate() {
        snapping = Time.time < respawnTime+0.5f;
        if (InputManager.ButtonDown(Buttons.PHOTOMODE)) {
            // check if something else paused it
            if (!photoMode && Time.timeScale != 1) return;
            photoMode = !photoMode;
            Time.timeScale = photoMode ? 0 : 1;
            photoModeCamera.SetActive(photoMode);
            AudioListener.volume = photoMode ? 0 : 1;
            if (photoMode) {
                pausedAudio.TransitionTo(0.5f);
            } else {
                unpausedAudio.TransitionTo(0.1f);
            }
            car.SetDashboardEnabled(!photoMode);
        }

        if (InputManager.ButtonDown(Buttons.CYCLE_CAMERA) && Time.timeScale == 1) {
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

        if (!car.grounded && car.rb.velocity.sqrMagnitude > 0 && !snapping) {
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

        Quaternion targetRotation = Quaternion.identity;
        if (car.grounded) {
            targetRotation = Quaternion.FromToRotation(transform.up, car.transform.up);
        }
        transform.rotation = QuaternionUtil.SmoothDamp(
            transform.rotation,
            targetRotation,
            ref rotateVelocity,
            55f * Time.deltaTime
        );

        if (snapping) {
            rotationAngle = Vector3.SignedAngle(transform.forward, car.transform.forward, Vector3.up);
        }


		// move the camera up a bit if near ground
		// if on a convex track segment, for example
		chaseCamNearGround = Physics.Raycast(
			new Ray(cameras[0].transform.position-targetOffset, -cameras[0].transform.up),
			out RaycastHit tempHit,
			0.5f,
			LayerMask.GetMask("Ground")
		);
        if (chaseCamNearGround) {
            targetOffset = Vector3.MoveTowards(targetOffset, cameras[0].transform.up, 0.25f * Time.fixedDeltaTime);
            Debug.DrawLine(cameras[0].transform.position, tempHit.point, Color.red);
        }
        else targetOffset = Vector3.MoveTowards(targetOffset, Vector3.zero, 0.25f * Time.fixedDeltaTime);

        y = Mathf.SmoothDampAngle(ring.localRotation.eulerAngles.y, rotationAngle, ref rotationSpeed, rotationSmoothTime);
        if (snapping) {
            y = rotationAngle;
        }
        
        // TODO: if car is grounded, and x-angle is above a certain threshold...
        // then move the camera's x-rotation to that value
        if (car.grounded && Mathf.Abs(Vector3.SignedAngle(car.transform.rotation.eulerAngles, Vector3.right, car.transform.right)) > 22) {
            x = Mathf.MoveTowardsAngle(x, car.transform.rotation.eulerAngles.x, 1f * Time.deltaTime);
        } else {
            x = Mathf.MoveTowardsAngle(x, 0, 1f * Time.deltaTime);
        }

        ring.localRotation = Quaternion.Euler(x, y, 0);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos+targetOffset, ref camVelocity, chaseSmoothTime, maxSpeed: 500);
        if (snapping) {
            transform.position = targetPos;
        }

        if (car.boosting) {
            targetFOV = baseFOV * 1.5f;
        } else {
            targetFOV = baseFOV;
        }
        float f;
        if (targetFOV > baseFOV) {
            f = Mathf.Lerp(cameras[0].m_Lens.FieldOfView, targetFOV, 4f * Time.deltaTime);
        } else {
            f = Mathf.Lerp(cameras[0].m_Lens.FieldOfView, targetFOV, 1f * Time.deltaTime);
        }
        cameras[0].m_Lens.FieldOfView = f;
    }

    public void OnRespawn() {
        respawnTime = Time.time;
    }

    public void ForceFirstPerson() {
        prevCam = currentCamera;
        allowChange = false;
        SetCamera(1);
    }

    public void StopForcingFirstPerson() {
        SetCamera(prevCam);
        allowChange = true;
    }
}
