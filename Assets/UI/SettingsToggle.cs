using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsToggle : MonoBehaviour {
    public AudioClip changeSound;
    public bool defaultValue;
	AudioSource audioSource;
    bool quiet;

	void Awake() {
		GetComponentInChildren<Toggle>().onValueChanged.AddListener(HandleValueChanged);
		audioSource = GetComponentInParent<AudioSource>();
	}

    void OnEnable() {
        quiet = true;
        GetComponentInChildren<Toggle>().isOn = PlayerPrefs.GetInt(gameObject.name, defaultValue ? 1 : 0) == 1;
		HandleValueChanged(GetComponentInChildren<Toggle>().isOn);
        quiet = false;
    }

    public void HandleValueChanged(bool val) {
        PlayerPrefs.SetInt(this.name, val ? 1 : 0);
        if (!quiet) {
            audioSource.PlayOneShot(changeSound);
        }
		// GameOptions.Load();
    }
}

