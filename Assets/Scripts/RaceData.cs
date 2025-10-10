using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class RaceData {
	public float maxSpeed;
	public int nitros;
	public float longestDrift;

	public Vector3 driftStartPos;
	public int totalShifts;
	public float goodShiftAmount;

	string[] shiftGradations = new string[]{
		"F-",
		"F",
		"D",
		"C",
		"B",
		"A",
		"S",
		"S+"
	};

	public string GetShiftQuality() {
		try {
			float averageShiftQuality = goodShiftAmount / (float) totalShifts;
			int idx = Mathf.FloorToInt(averageShiftQuality * shiftGradations.Length);
			return shiftGradations[idx];
		} catch {
			return "N/A";
		}
	}

	public int GetMaxVelocityMPH() {
		return Mathf.RoundToInt(maxSpeed * Car.u2mph);
	}

	public int GetLongestDriftFeet() {
		return Mathf.RoundToInt(longestDrift * 3.28084f);
	}
}
