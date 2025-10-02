using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class SetFirstSelected : MonoBehaviour {
	public Selectable firstSelected;

	void OnEnable() {
		if (!firstSelected) GetComponentInChildren<Selectable>().Select();
		else firstSelected.Select();
	}
}
