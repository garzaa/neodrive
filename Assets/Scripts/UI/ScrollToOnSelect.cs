using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ScrollToOnSelect : MonoBehaviour, ISelectHandler {
	public void OnSelect(BaseEventData d) {
        GetComponentInParent<ScrollViewUtils>().ScrollToChild(GetComponent<RectTransform>());
    }
}
