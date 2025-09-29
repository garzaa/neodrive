using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EngineAudio : MonoBehaviour {

	float maxEngineVolume;
	readonly List<RPMPoint> rpmPoints = new();
    AudioSource gearAudio;
    EngineSettings engine;
    bool bigSteps = false;

    const float tremoloHZ = 5;
    float t = 0;

    bool mute = false;
    
    public void OnCarRespawn() {
        CancelInvoke(nameof(Unmute));
        mute = true;
        Invoke(nameof(Unmute), 0.1f);
    }

    void Unmute() {
        mute = false;
    }

	public void BuildSoundCache(EngineSettings engine, AudioSource engineAudioSource, bool bigSteps = false) {
        this.engine = engine;
        this.bigSteps = bigSteps;
		maxEngineVolume = engineAudioSource.volume;
        foreach (RPMPoint r in engine.rpmPoints) {
			if (bigSteps && (r.rpm % 1000 != 0)) continue;

            AudioSource rAudio = engineAudioSource.gameObject.AddComponent<AudioSource>();
            rAudio.volume = 0;
            rAudio.outputAudioMixerGroup = engineAudioSource.outputAudioMixerGroup;
            rAudio.clip = r.throttle;
            rAudio.loop = true;
            rAudio.spatialBlend = 1;
            rAudio.minDistance = engineAudioSource.minDistance;
            rAudio.Play();
            AudioSource rOffAudio = engineAudioSource.gameObject.AddComponent<AudioSource>();
            rOffAudio.volume = 0;
            rOffAudio.outputAudioMixerGroup = engineAudioSource.outputAudioMixerGroup;
            rOffAudio.clip = r.throttleOff;
            rOffAudio.loop = true;
            rOffAudio.spatialBlend = 1;
            rOffAudio.minDistance = engineAudioSource.minDistance;
            rOffAudio.Play();

            rpmPoints.Add(new RPMPoint(r, rAudio, rOffAudio));
        }

        gearAudio = engineAudioSource.gameObject.AddComponent<AudioSource>();
        gearAudio.volume = 0;
        gearAudio.outputAudioMixerGroup = engineAudioSource.outputAudioMixerGroup;
        gearAudio.clip = engine.gearSound.gearLoop;
        gearAudio.loop = true;
        gearAudio.spatialBlend = 1;
        gearAudio.minDistance = engineAudioSource.minDistance;
        gearAudio.Play();
	}

	public void SetRPMAudio(float rpm, float gas, bool fuelCutoff, bool clutchPressed, float carSpeed) {
        // shorthand for checking whether we're on a ghost car
        // which has no speed currently computed
        SetGearAudio(carSpeed, clutchPressed, gas);
        RPMPoint lowTarget = rpmPoints[0];
        RPMPoint highTarget = rpmPoints[0];
        for (int i=1; i<rpmPoints.Count-1; i++) {
            RPMPoint current = rpmPoints[i];
            RPMPoint lower = rpmPoints[i-1];
            RPMPoint higher = rpmPoints[i+1];

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

            if (rpm >= lower.rpm && rpm <= current.rpm) {
                lowTarget = lower;
                highTarget = current;
            } else if (rpm >= current.rpm && rpm <= higher.rpm) {
                lowTarget = current;
                highTarget = higher;
            }
        }

        for (int i=0; i<rpmPoints.Count; i++) {
            rpmPoints[i].throttleAudio.volume = 0;
            rpmPoints[i].throttleOffAudio.volume = 0;
        }

        if (rpm == 0 || mute) {
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

    void SetGearAudio(float carSpeed, bool clutch, float gas) {
        if (mute) {
            gearAudio.volume = 0;
            return;
        }

        float frac = carSpeed / engine.gearSound.baseSpeed;
        gearAudio.volume = engine.gearSound.volumeCurve.Evaluate(frac);

        // shorten the RPM diff so it doesn't fluctuate as much
        float diff = engine.gearSound.baseSpeed - carSpeed;
        diff *= -engine.gearSound.toneShiftAmount;
        float updatedRPM  = engine.gearSound.baseSpeed + diff;
        float targetPitch = updatedRPM / engine.gearSound.baseSpeed;
        gearAudio.pitch = targetPitch;
        
        // sinewave volume modulation
        // at default RPM: 5 hz
        // change sinewave freq based on rpm ratio diff
        t += Time.deltaTime * Mathf.PI * 2f * tremoloHZ * (carSpeed / engine.gearSound.baseSpeed);
        // get a sinewave normalized betwen 1 and (1-tremoloVolumeRange)
        // midpoint at 1 - (tremoloVolumeRange/2)
        float s = Mathf.Sin(t) 
            * (engine.gearSound.tremoloVolumeRange / 2f)
            + (1-(engine.gearSound.tremoloVolumeRange/2f));
        gearAudio.volume *= s;
        // reduce volume partly based on gas/clutch
        gas *= clutch ? 0 : 1;
        gearAudio.volume *= 1-((1-gas)*0.45f);
        gearAudio.volume *= engine.gearSound.baseVolume;
    }
}
