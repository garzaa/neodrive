using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Data/CarSettings")]
public class CarSettings : ScriptableObject {
	public float suspensionTravel;
	public float springStrength = 15000f;
	public float springDamper = 1000f;
	public LayerMask wheelRaycast;
}
