using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class GameOptions : MonoBehaviour {
	public static bool PlayerGhost { get; private set; }
	public static bool AuthorGhost { get; private set; }
	public static bool Rumble { get; private set; }

	public UnityEvent Apply;

	static GameOptions instance;

	public void Awake() {
		instance = this;
		Load();
	}

	public static void Load() {
		PlayerGhost = LoadBool("PlayerGhost", true);
		AuthorGhost = LoadBool("AuthorGhost", true);
		Rumble = LoadBool("Rumble", true);
		QualitySettings.vSyncCount = LoadBool("VSync") ? 1 : 0;
		QualitySettings.SetQualityLevel(
			PlayerPrefs.GetInt(QualityDropdown.qualityName, QualitySettings.names.Length)
		);
		instance.Apply.Invoke();
		foreach (SettingsSlider slider in FindObjectsOfType<SettingsSlider>(includeInactive: true)) {
			slider.OnEnable();
		}
	}

	static bool LoadBool(string name, bool defaultValue = false) {
		return PlayerPrefs.GetInt(name, defaultValue ? 1 : 0) == 1;
	}
}
