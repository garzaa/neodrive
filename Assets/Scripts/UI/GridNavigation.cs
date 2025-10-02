using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GridNavigation : MonoBehaviour {
	public int columns = 5;

	public Selectable leftSelect;

	void OnEnable() {
		SetGridNavigation(GetComponentsInChildren<Selectable>(includeInactive: true), columns);
	}

	void SetGridNavigation(Selectable[] buttons, int cols) {
		// navigation should wrap around between rows in left-right
		for (int i=0; i<buttons.Length; i++) {
			Navigation n = new() {
				mode = Navigation.Mode.Explicit
			};
			if (i > cols-1) {
				n.selectOnUp = buttons[i-cols];
			}
			if (i > 0) {
				n.selectOnLeft = buttons[i-1];
			}
			if (i < buttons.Length-1) {
				n.selectOnRight = buttons[i+1];
			}
			if (i < buttons.Length - cols) {
				n.selectOnDown = buttons[i+cols];
			}
			if (i % cols == 0 && leftSelect != null) {
				n.selectOnLeft = leftSelect;
			}
			buttons[i].navigation = n;
		}
	}
}
