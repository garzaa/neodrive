using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

public class Garage : MonoBehaviour {
	// render wheel options
	// switch wheels
	// load all wheels and talk to achievements to see which other ones are unlocked
	// wheels should be linked to achievements actually, not the other way around
	CustomWheel[] wheels;
	Achievements achievements;
	CarCustomization carCustomization;
	int currentWheel;
	
	public Text wheelTitle;
	public Text wheelRequirement;
	public GameObject wheelSelectButton;

	public Animator garageAnimator;

	public CustomWheel lockedWheel;

	void Awake() {
		wheels = Resources.LoadAll<CustomWheel>("Wheels")
			.Where(x => x.achievement == null || !x.achievement.hidden)
			.ToArray();
		achievements = FindObjectOfType<Achievements>();
		carCustomization = FindObjectOfType<CarCustomization>();
	}

	void Start() {
		CustomWheel w = carCustomization.GetWheel();
		for (int i=0; i<wheels.Length; i++) {
			if (w.name == wheels[i].name) {
				currentWheel = i;
				break;
			}
		}
		ApplyWheel();
	}

	void Update() {
		if (EventSystem.current.currentSelectedGameObject == wheelSelectButton) {
			if (InputManager.ButtonDown(Buttons.UILEFT)) {
				PrevWheel();
			} else if (InputManager.ButtonDown(Buttons.UIRIGHT)) {
				NextWheel();
			}
		}
	}

	public void NextWheel() {
		if (currentWheel == wheels.Length-1) {
			currentWheel = 0;
		} else {
			currentWheel += 1;
		}
		garageAnimator.SetTrigger("WheelSwitch");
		ApplyWheel();
	}

	public void PrevWheel() {
		if (currentWheel == 0) {
			currentWheel = wheels.Length-1;
		} else {
			currentWheel -= 1;
		}
		garageAnimator.SetTrigger("WheelSwitch");
		ApplyWheel();
	}

	void ApplyWheel() {
		CustomWheel w = wheels[currentWheel];
		if (w.achievement) {
			if (!achievements.Has(w.achievement)) {
				wheelTitle.text = "???";
				carCustomization.SetWheelLocked(lockedWheel);
				wheelRequirement.text = "unlocked by " + w.achievement.GetName();
			} else {
				carCustomization.SetWheel(w);
				wheelTitle.text = w.GetName();
				wheelRequirement.text = "unlocked by " + w.achievement.GetName();
			}
		} else {
			carCustomization.SetWheel(w);
			wheelTitle.text = w.GetName();
			wheelRequirement.text = "";
		}
	}
}
