using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct CarSnapshot {
	public Vector3 position;
	public Quaternion rotation;
	public float rpm;
	public float steerAngle;
	public float gas;

	public CarSnapshot(Vector3 p, Quaternion q, float r, float a, float g) {
		position = p;
		rotation = q;
		rpm = r;
		steerAngle = a;
		this.gas = g;
	}
}
