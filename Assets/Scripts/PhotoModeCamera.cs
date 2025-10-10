using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using Cinemachine;

public class PhotoModeCamera : MonoBehaviour {
	readonly float cameraSpeed = 10f;
	float defaultFOV;
	CinemachineVirtualCamera cam;
	GameObject car = null;

	// this needs to be linked in the editor so it actually fires for some reason
	public UnityEvent<bool> OnPhotoModeChange;

	GameObject controlCanvas;

	void OnEnable() {
		if (car == null) {
			car = FindObjectOfType<Car>().gameObject;
			controlCanvas = GetComponentInChildren<Canvas>().gameObject;
			cam = GetComponent<CinemachineVirtualCamera>();
			defaultFOV = cam.m_Lens.FieldOfView;
		}
	}

	public void EnterPhotoMode() {
		gameObject.SetActive(true);
		transform.position = car.transform.position;
		transform.SetPositionAndRotation(car.transform.position + new Vector3(3, 0, 2), Quaternion.Euler(0, 270, 0));
		OnPhotoModeChange.Invoke(true);
		controlCanvas.SetActive(true);
	}

	public void ExitPhotoMode() {
		OnPhotoModeChange.Invoke(false);
		gameObject.SetActive(false);
		cam.m_Lens.FieldOfView = defaultFOV;
	}

	public void Update() {
		float moveForwardAxis = InputManager.GetAxis("CameraMoveForward");

		transform.position += transform.TransformDirection((InputManager.Button(Buttons.BOOST) ? 5 : 1) * cameraSpeed * Time.unscaledDeltaTime * new Vector3(
			InputManager.GetAxis(Buttons.STEER),
			InputManager.GetAxis("CameraMoveVertical"),
			moveForwardAxis
		));

		Vector3 r = transform.localRotation.eulerAngles;
		r.y += InputManager.GetAxis(Buttons.CAM_X) * 180 * Time.unscaledDeltaTime;
		r.x -= InputManager.GetAxis(Buttons.CAM_Y) * 45 * Time.unscaledDeltaTime;
		transform.localRotation = Quaternion.Euler(r);

		if (InputManager.ButtonDown(Buttons.CYCLE_CAMERA)) {
			controlCanvas.SetActive(!controlCanvas.activeSelf);
		}

		if (InputManager.Button(Buttons.PADDLE_UP)) {
			cam.m_Lens.FieldOfView -= 90 * Time.unscaledDeltaTime;
		}
		if (InputManager.Button(Buttons.PADDLE_DOWN)) {
			cam.m_Lens.FieldOfView += 90 * Time.unscaledDeltaTime;
		}
		if (InputManager.Button(Buttons.PADDLE_DOWN) && InputManager.Button(Buttons.PADDLE_UP)) {
			cam.m_Lens.FieldOfView = defaultFOV;
		}
		cam.m_Lens.FieldOfView = Mathf.Clamp(cam.m_Lens.FieldOfView, 4, 120);
	}
}
