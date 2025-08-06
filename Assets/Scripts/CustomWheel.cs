using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Data/CustomWheel")]
public class CustomWheel : ScriptableObject {
	public Material defaultMaterial;
	public Material spinningMaterial;
	public string nameOverride;
	public Achievement achievement;

	public string GetName() {
		if (string.IsNullOrEmpty(nameOverride)) {
			return name;
		}
		return nameOverride;
	}
}
