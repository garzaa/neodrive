using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class PhotoModeCamera : MonoBehaviour {
	readonly float cameraSpeed = 10f;
	GameObject car = null;

	// this needs to be linked in the editor so it actually fires for some reason
	public UnityEvent<bool> OnPhotoModeChange;

	void OnEnable() {
		if (car == null) car = FindObjectOfType<Car>().gameObject;
	}

	public void EnterPhotoMode() {
		Debug.Log($"entering photo mode");
		gameObject.SetActive(true);
		transform.position = car.transform.position;
		transform.SetPositionAndRotation(car.transform.position + new Vector3(3, 0, 2), Quaternion.Euler(0, 270, 0));
		OnPhotoModeChange.Invoke(true);
	}

	public void ExitPhotoMode() {
		OnPhotoModeChange.Invoke(false);
		gameObject.SetActive(false);
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
	}
}
