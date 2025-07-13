using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct CarSnapshot {
	public readonly Vector3 position;
	public readonly Quaternion rotation;
	public readonly float rpm;
	public readonly float steerAngle;
	public readonly float gas;
	public readonly bool drifting;

	public CarSnapshot(Vector3 p, Quaternion q, float r, float a, float g, bool d) {
		position = p;
		rotation = q;
		rpm = r;
		steerAngle = a;
		gas = g;
		drifting = d;
	}
}
