using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

public class QualityDropdown : MonoBehaviour {
	Dropdown dropdown;
	List<string> qualityNames = new();
	public static readonly string qualityName = "QualityLevel";

	void Start() {
		dropdown = GetComponentInChildren<Dropdown>();
		qualityNames = QualitySettings.names.ToList();
		dropdown.ClearOptions();
		dropdown.AddOptions(qualityNames);
		dropdown.onValueChanged.AddListener(OnValueChange);
		// default to max quality
		dropdown.value = PlayerPrefs.GetInt(qualityName, qualityNames.Count);
		OnValueChange(dropdown.value);
	}

	void OnValueChange(int settingLevel) {
		PlayerPrefs.SetInt(qualityName, settingLevel);
		GameOptions.Load();
	}

}
