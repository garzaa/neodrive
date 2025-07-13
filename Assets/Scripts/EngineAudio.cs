using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EngineAudio : MonoBehaviour {
	float maxEngineVolume;
	List<RPMPoint> rpmPoints = new();

	public void BuildSoundCache(EngineSettings engine, AudioSource engineAudio) {
		maxEngineVolume = engineAudio.volume;
        foreach (RPMPoint r in engine.rpmPoints) {
            AudioSource rAudio = engineAudio.gameObject.AddComponent<AudioSource>();
            rAudio.volume = maxEngineVolume;
            rAudio.outputAudioMixerGroup = engineAudio.outputAudioMixerGroup;
            rAudio.clip = r.throttle;
            rAudio.loop = true;
            rAudio.spatialBlend = 1;
            rAudio.minDistance = engineAudio.minDistance;
            rAudio.Play();
            AudioSource rOffAudio = engineAudio.gameObject.AddComponent<AudioSource>();
            rOffAudio.volume = maxEngineVolume;
            rOffAudio.outputAudioMixerGroup = engineAudio.outputAudioMixerGroup;
            rOffAudio.clip = r.throttleOff;
            rOffAudio.loop = true;
            rOffAudio.spatialBlend = 1;
            rOffAudio.minDistance = engineAudio.minDistance;
            rOffAudio.Play();

            rpmPoints.Add(new RPMPoint(r, rAudio, rOffAudio));
        }
	}

	public void SetRPMAudio(float rpm, float gas, bool fuelCutoff) {
        RPMPoint lowTarget = rpmPoints[0];
        RPMPoint highTarget = rpmPoints[0];
        for (int i=1; i<rpmPoints.Count-1; i++) {
            RPMPoint current = rpmPoints[i];
            RPMPoint lower = rpmPoints[i-1];
            RPMPoint higher = rpmPoints[i+1];

            // in the one-in-a-million scenario where the RPM exactly matches up
            if (rpm == current.rpm) {
                lowTarget = current;
                highTarget = current;
                break;
            }

            // if at the start, and it's just the lower rpm, return the lowest RPM
            if (i==1 && rpm < lower.rpm) {
                lowTarget = lower;
                highTarget = lower;
                break;
            }
            // same for the end
            if (i == rpmPoints.Count-2 && rpm > higher.rpm) {
                lowTarget = higher;
                highTarget = higher;
                break;
            }

            if (rpm > lower.rpm && rpm < current.rpm) {
                lowTarget = lower;
                highTarget = current;
            } else if (rpm > current.rpm && rpm < higher.rpm) {
                lowTarget = current;
                highTarget = higher;
            }
        }

        for (int i=0; i<rpmPoints.Count; i++) {
            rpmPoints[i].throttleAudio.volume = 0;
            rpmPoints[i].throttleOffAudio.volume = 0;
        }

        if (rpm == 0) {
            return;
        }

        // set the volume for low and high targets based on RPM
        float rpmRatio = (rpm - lowTarget.rpm) / (highTarget.rpm - lowTarget.rpm);
        float lowVolume = maxEngineVolume * (1-rpmRatio);
        float highVolume = maxEngineVolume * rpmRatio;
        float targetLowPitch = rpm / lowTarget.rpm;
        float targetHighPitch = rpm / highTarget.rpm;

        if (lowTarget == highTarget) {
            lowTarget.throttleAudio.volume = 1;
            lowTarget.throttleOffAudio.volume = 1;
            highTarget.throttleAudio.volume = 1;
            highTarget.throttleOffAudio.volume = 1;
            lowTarget.throttleAudio.volume *= gas;
            lowTarget.throttleOffAudio.volume *= 1-gas;
            lowTarget.throttleAudio.pitch = targetLowPitch;
            lowTarget.throttleOffAudio.pitch = targetLowPitch;
            return;
        }

        lowTarget.throttleAudio.volume = lowVolume;
        lowTarget.throttleOffAudio.volume = lowVolume;
        highTarget.throttleAudio.volume = highVolume;
        highTarget.throttleOffAudio.volume = highVolume;
        // then lerp between each one based on gas
        lowTarget.throttleAudio.volume *= gas;
        highTarget.throttleAudio.volume *= gas;
        // audibly drop the audio on fuel cutoff so you Know
        lowTarget.throttleOffAudio.volume *= 1-gas * 0.5f * (!fuelCutoff ? 1f : 0.2f);
        highTarget.throttleOffAudio.volume *= 1-gas * 0.5f * (!fuelCutoff ? 1f : 0.2f);

        // then warp the sound to match the RPM
        lowTarget.throttleAudio.pitch = targetLowPitch;
        lowTarget.throttleOffAudio.pitch = targetLowPitch;
        highTarget.throttleAudio.pitch = targetHighPitch;
        highTarget.throttleOffAudio.pitch = targetHighPitch;
	}
}
