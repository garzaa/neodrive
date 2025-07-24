using UnityEngine;
using UnityEngine.UI;

public class SettingsSlider : MonoBehaviour {
    public string prefName;
    public Text valueLabel;
    public int defaultValue = 5;

    virtual protected void OnEnable() {
        GetComponentInChildren<Slider>().value = PlayerPrefs.GetInt(prefName, defaultValue);
        // force an update
        HandleValueChanged(GetComponentInChildren<Slider>().value);
    }

    void OnDisable() {
        OnEnable();
    }

    virtual public void HandleValueChanged(float val) {
        PlayerPrefs.SetInt(prefName, (int) val);
        if (valueLabel) valueLabel.text = ((int) val).ToString();
        // this can stay commented because volume is set in volumeslider
        // GameOptions.Load();
    }
}
