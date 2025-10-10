using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Rendering.PostProcessing;

public class GameOptions : MonoBehaviour {
	public static bool PlayerGhost { get; private set; }
	public static bool AuthorGhost { get; private set; }
	public static bool Rumble { get; private set; }
	public static bool PaddleShift { get; private set; }

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
		instance.Apply.Invoke();
		foreach (SettingsSlider slider in FindObjectsOfType<SettingsSlider>(includeInactive: true)) {
			slider.OnEnable();
		}

		int qualityLevel = PlayerPrefs.GetInt(QualityDropdown.qualityName, QualitySettings.names.Length);
		QualitySettings.SetQualityLevel(qualityLevel);
		var postProcessLayer = Camera.main.GetComponent<PostProcessLayer>();
		if (qualityLevel == 3) {
			postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
			postProcessLayer.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.High;
		} else if (qualityLevel == 2) {
			postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
			postProcessLayer.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.Medium;
		} else if (qualityLevel == 1) {
			postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
		} else if (qualityLevel == 0) {
			postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.None;
		}

		PaddleShift = LoadBool("PaddleShift", false);
	}

	static bool LoadBool(string name, bool defaultValue = false) {
		return PlayerPrefs.GetInt(name, defaultValue ? 1 : 0) == 1;
	}
}
