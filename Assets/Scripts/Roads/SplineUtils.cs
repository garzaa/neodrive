using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SplineArchitect.Objects;
using NaughtyAttributes;

[ExecuteInEditMode]
public class SplineUtils : MonoBehaviour {
	Spline spline;
	public float loopRadius = 50;
	public float roadWidth = 10;
	public float widthMultiplier = 1;

	readonly Vector3[] normals = new Vector3[3];

	void Start() {
		GetSpline();
		// FixBounds();
	}

	void GetSpline() {
		if (spline == null) spline = GetComponent<Spline>();
	}

	[Button("Add Loop")]
	public void AddLoop() {
		// three segments. look at the tangent direction of the last segment
		GetSpline();
		Vector3[] rightUpForward = new Vector3[3];
		spline.GetNormalsNonAlloc(rightUpForward, 1);
		Vector3 lastPos = spline.GetPosition(1);
		// first one - base of the loop
		// tangent A points forward in the direction of the spline
		// tangent B points back to the last point
		spline.CreateSegment(
			lastPos + (loopRadius * 0.5f * rightUpForward[2]),
			lastPos + (loopRadius * rightUpForward[2]),
			lastPos
		);

		// loop going up midpoint
		lastPos = spline.GetPosition(1);
		Vector3 newPos = lastPos + (loopRadius * rightUpForward[1]) + (loopRadius * rightUpForward[2]);
		spline.CreateSegment(
			newPos,
			newPos + (loopRadius * 0.5f * rightUpForward[1]),
			newPos - (loopRadius * 0.5f * rightUpForward[1])
		);

		// loop apigee
		lastPos = newPos;
		newPos = lastPos + (loopRadius * rightUpForward[1]) - (loopRadius * rightUpForward[2]);
		// move right so it doesn't overlap
		// coming down it'll move right again
		newPos += rightUpForward[0] * roadWidth/2f * widthMultiplier;
		spline.CreateSegment(
			newPos,
			newPos - (loopRadius * 0.5f * rightUpForward[2]),
			newPos + (loopRadius * 0.5f * rightUpForward[2])
		);
		SetFlat(spline.segments[^1]);

		// down the backside
		lastPos = newPos;
		newPos = lastPos - (loopRadius * rightUpForward[1]) - (loopRadius * rightUpForward[2]);
		newPos += rightUpForward[0] * roadWidth/2f * widthMultiplier;
		spline.CreateSegment(
			newPos,
			newPos - (loopRadius * 0.5f * rightUpForward[1]),
			newPos + (loopRadius * 0.5f * rightUpForward[1])
		);
		SetFlat(spline.segments[^1]);

		// then the bottom anchor
		lastPos = newPos;
		newPos = lastPos - (loopRadius * rightUpForward[1]) + (loopRadius * rightUpForward[2]);
		spline.CreateSegment(
			newPos,
			newPos + (loopRadius * 0.5f * rightUpForward[2]),
			newPos - (loopRadius * 0.5f * rightUpForward[2])
		);
		SetFlat(spline.segments[^1]);

		// then one more afterwards to give it a flat tangent
		lastPos = newPos;
		newPos = lastPos + loopRadius * rightUpForward[2];
		spline.CreateSegment(
			newPos,
			newPos + (loopRadius * 0.5f * rightUpForward[2]),
			newPos - (loopRadius * 0.5f * rightUpForward[2])
		);
		SetFlat(spline.segments[^1]);

		spline.monitor.ForceUpdate();
	}
	
	void SetFlat(Segment s) {
		spline.GetNormalsNonAlloc(normals, s.zPosition / spline.length);

		float dotUp = Vector3.Dot(normals[1], Vector3.up);
		float dotDown = Vector3.Dot(normals[1], Vector3.down);
		float dot = dotUp > dotDown ? dotUp : dotDown;

		s.zRotation -= Mathf.Acos(dot) * Mathf.Rad2Deg;
		spline.CalculateCachedNormals();
	}

	[Button("Remove Loop")]
	public void RemoveLoop() {
		// add more as you add more vertices
		spline.RemoveSegment(spline.segments.Count-1);
		spline.RemoveSegment(spline.segments.Count-1);
		spline.RemoveSegment(spline.segments.Count-1);
		spline.RemoveSegment(spline.segments.Count-1);
		spline.RemoveSegment(spline.segments.Count-1);
		spline.RemoveSegment(spline.segments.Count-1);
	}

	[Button("Fix Spline LODs")]
	public void FixBounds() {
		foreach (LODGroup lod in GetComponentsInChildren<LODGroup>()) {
			lod.RecalculateBounds();
		}
	}

	[Button("Fix Checkpoint Names")]
	public void FixCheckpoints() {
		Checkpoint[] checkpoints = FindObjectsOfType<Checkpoint>();
		for (int i=0; i<checkpoints.Length; i++) {
			checkpoints[i].name = "Checkpoint " + i;
		}
	}

	[Button("Check Spline Length")]
	public void CheckLength() {
		print(spline.length);
	}
}
