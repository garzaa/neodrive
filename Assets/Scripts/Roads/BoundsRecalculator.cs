using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

[ExecuteInEditMode]
public class BoundsRecalculator : MonoBehaviour {
	// on spline change, go through all mesh LODs in child and recalculate their bounds
	void Start() {
		FixBounds();
	}

	[Button("Fix Spline LODs")]
	void FixBounds() {
		foreach (LODGroup lod in GetComponentsInChildren<LODGroup>()) {
			print("recalcualted bounds on "+lod.name);
			lod.RecalculateBounds();
		}
	}
}
