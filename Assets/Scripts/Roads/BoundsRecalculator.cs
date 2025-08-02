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
			lod.RecalculateBounds();
		}
	}

	[Button("Fix Checkpoint Names")]
	public void FixCheckpoints() {
		Checkpoint[] checkpoints = GetComponentsInChildren<Checkpoint>();
		for (int i=0; i<checkpoints.Length; i++) {
			checkpoints[i].name = "Checkpoint " + i;
		}
	}
}
