using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class BoundsRecalculator : MonoBehaviour {
	// on spline change, go through all mesh LODs in child and recalculate their bounds
	void Start() {
		foreach (LODGroup lod in GetComponentsInChildren<LODGroup>()) {
			print("recalculating bounds for "+ lod.name);
			lod.RecalculateBounds();
		}
	}
}
