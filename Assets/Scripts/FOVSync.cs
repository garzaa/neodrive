using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class FOVSync : MonoBehaviour {
	public Camera baseline;
	Camera thisCamera;

	void Start() {
		thisCamera = GetComponent<Camera>();
	}

	void LateUpdate() {
		thisCamera.fieldOfView = baseline.fieldOfView;
	}
}
