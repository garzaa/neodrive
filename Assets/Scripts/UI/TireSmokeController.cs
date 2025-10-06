using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class TireSmokeController : MonoBehaviour {
	void Start() {
		Color32 c = FindObjectsOfType<Light>().First(light => light.type == LightType.Directional).color;
		Debug.Log($"{c}");
		GetComponent<Image>().color = c;
	}
}
