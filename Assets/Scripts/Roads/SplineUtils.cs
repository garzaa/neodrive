using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SplineArchitect.Objects;

[ExecuteInEditMode]
public class SplineUtils : MonoBehaviour {
	Spline spline;
	public float loopRadius = 50;
	public float roadWidth = 10;

	void Start() {
		GetSpline();
		FixBounds();
	}

	void GetSpline() {
		if (spline == null) spline = GetComponent<Spline>();
	}

	[ContextMenu("Add Loop")]
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
		newPos += rightUpForward[0] * roadWidth/2f;
		spline.CreateSegment(
			newPos,
			newPos - (loopRadius * 0.5f * rightUpForward[2]),
			newPos + (loopRadius * 0.5f * rightUpForward[2])
		);
		spline.segments[^1].zRotation = 22.5f;

		// down the backside
		lastPos = newPos;
		newPos = lastPos - (loopRadius * rightUpForward[1]) - (loopRadius * rightUpForward[2]);
		newPos += rightUpForward[0] * roadWidth/2f;
		spline.CreateSegment(
			newPos,
			newPos - (loopRadius * 0.5f * rightUpForward[1]),
			newPos + (loopRadius * 0.5f * rightUpForward[1])
		);
		spline.segments[^1].zRotation = 45f;

		// then the bottom anchor
		lastPos = newPos;
		newPos = lastPos - (loopRadius * rightUpForward[1]) + (loopRadius * rightUpForward[2]);
		spline.CreateSegment(
			newPos,
			newPos + (loopRadius * 0.5f * rightUpForward[2]),
			newPos - (loopRadius * 0.5f * rightUpForward[2])
		);
		spline.segments[^1].zRotation = 45f;

		// then one more afterwards to give it a flat tangent
		lastPos = newPos;
		newPos = lastPos + loopRadius * rightUpForward[2];
		spline.CreateSegment(
			newPos,
			newPos + (loopRadius * 0.5f * rightUpForward[2]),
			newPos - (loopRadius * 0.5f * rightUpForward[2])
		);
		spline.segments[^1].zRotation = 45f;
	}

	[ContextMenu("Remove Loop")]
	public void RemoveLoop() {
		// add more as you add more vertices
		spline.RemoveSegment(spline.segments.Count-1);
		spline.RemoveSegment(spline.segments.Count-1);
		spline.RemoveSegment(spline.segments.Count-1);
		spline.RemoveSegment(spline.segments.Count-1);
		spline.RemoveSegment(spline.segments.Count-1);
		spline.RemoveSegment(spline.segments.Count-1);
	}

	[ContextMenu("Fix Spline LODs")]
	void FixBounds() {
		foreach (LODGroup lod in GetComponentsInChildren<LODGroup>()) {
			lod.RecalculateBounds();
		}
	}
}
