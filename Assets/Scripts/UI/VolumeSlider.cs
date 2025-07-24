using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Collections;

public class VolumeSlider : SettingsSlider {
    public AudioMixerGroup mixerGroup;

	// slider should be 0-10 with 5 as the default
    override public void HandleValueChanged(float val) {
        base.HandleValueChanged(val);
        val *= 2;
        // attenuation between -10f / +10f
        // 5 should map to no change, starts at 0
        mixerGroup.audioMixer.SetFloat(prefName, val-10f);
    }
}
