using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.Loading;

public class StageButtons : SavedObject {
	string lastStage;
	StageButton[] stageButtons;

	protected override void Initialize() {
		stageButtons = GetComponentsInChildren<StageButton>();
		if (gameObject.activeInHierarchy) SelectStage(stageButtons[0]);
	}

	void OnEnable() {
		// if lastStage is "" it's fine
		stageButtons = GetComponentsInChildren<StageButton>();
		foreach (StageButton stage in stageButtons) {
			if (stage.name.Equals(lastStage)) {
				SelectStage(stage);
				return;
			}
		}
		SelectStage(stageButtons[0]);
	}

	protected override void LoadFromProperties() {
		lastStage = Get<string>(nameof(lastStage));
	}

	protected override void SaveToProperties(ref Dictionary<string, object> properties) {
		properties[nameof(lastStage)] = lastStage;
	}

	public void SelectStage(StageButton stage) {
		lastStage = stage.name;
		// make all stage buttons point to the first of the newly shown stages
		Selectable firstChild = stage.stageUI.GetComponentInChildren<Selectable>(includeInactive: true);
		VerticalNavigation vn = GetComponent<VerticalNavigation>();
		vn.rightSelect = firstChild;
		vn.SetNavigation();

		foreach (StageButton sb in stageButtons) {
			sb.GetComponent<Animator>().SetBool("ForceHighlight", false);
			sb.stageUI.SetActive(false);
		}
		stage.GetComponent<Animator>().SetBool("ForceHighlight", true);
		stage.stageUI.SetActive(true);
	}
}
