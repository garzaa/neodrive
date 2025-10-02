using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageButton : MonoBehaviour {
	Animator anim;
	public GameObject stageUI;

	void Start() {
		anim = GetComponent<Animator>();
		GetComponent<Button>().onClick.AddListener(OnSubmit);
	}

	void OnSubmit() {
		GetComponentInParent<StageButtons>().SelectStage(this);
	}
}
