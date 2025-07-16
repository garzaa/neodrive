using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct CarSnapshot : ISerializationCallbackReceiver{
	[System.NonSerialized] public Vector3 position;
	[System.NonSerialized] public Quaternion rotation;
	public readonly float rpm;
	public readonly float steerAngle;
	public readonly float gas;
	public readonly bool drifting;
	public readonly bool boosting;

    [SerializeField] private float[] positionArray;
    [SerializeField] private float[] rotationArray;

	public CarSnapshot(Vector3 p, Quaternion q, float r, float a, float g, bool d, bool b) {
		position = p;
		rotation = q;
		rpm = r;
		steerAngle = a;
		gas = g;
		drifting = d;
		boosting = b;

		positionArray = new float[3];
        rotationArray = new float[4];
	}
	
	public void OnBeforeSerialize() {
        // Convert Vector3 to a float array
        positionArray = new float[] { position.x, position.y, position.z };
        
        // Convert Quaternion to a float array
        rotationArray = new float[] { rotation.x, rotation.y, rotation.z, rotation.w };
    }

	public void OnAfterDeserialize() {
        // Convert float array back to Vector3
        if (positionArray != null && positionArray.Length == 3) {
            position = new Vector3(positionArray[0], positionArray[1], positionArray[2]);
        }
        
        // Convert float array back to Quaternion
        if (rotationArray != null && rotationArray.Length == 4) {
            rotation = new Quaternion(rotationArray[0], rotationArray[1], rotationArray[2], rotationArray[3]);
        }
    }
}
