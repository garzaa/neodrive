using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PhotoModeCamera : MonoBehaviour {
	float cameraSpeed = 10f;
	public GameObject car;

	void OnEnable() {
		transform.position = car.transform.position;
		transform.SetPositionAndRotation(car.transform.position + new Vector3(3, 0, 2), Quaternion.Euler(0, 270, 0));
	}

	public void Update() {
		float moveForwardAxis = InputManager.GetAxis("CameraMoveForward");
		float moveVerticalAxis = InputManager.GetAxis("CameraMoveVertical");

		transform.position += transform.TransformDirection(new Vector3(
			InputManager.GetAxis(Buttons.STEER),
			InputManager.GetAxis("CameraMoveVertical"),
			moveForwardAxis
		) * Time.unscaledDeltaTime * cameraSpeed);

		Vector3 r = transform.localRotation.eulerAngles;
		r.y += InputManager.GetAxis(Buttons.CAM_X) * 180 * Time.unscaledDeltaTime;
		r.x -= InputManager.GetAxis(Buttons.CAM_Y) * 45 * Time.unscaledDeltaTime;
		transform.localRotation = Quaternion.Euler(r);
	}
}
