using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarCustomization : SavedObject {
	CustomWheel wheel;
	Car car;

	public CustomWheel wheelOverride;

	// for main menu and whatever else
	public Wheel[] extraWheels;

	protected override void Initialize() {
		car = FindObjectOfType<Car>();
	}

	public CustomWheel LoadWheelObject(string wheelName) {
		return Resources.Load<CustomWheel>("Wheels/"+wheelName);
	}

	protected override void LoadFromProperties() {
		string savedWheel;
		try {
			savedWheel = Get<string>("wheelName");
		} catch {
			savedWheel = "437 Special";
		}
		SetWheel(LoadWheelObject(savedWheel));
	}

	protected override bool ForceLoadIfNoProperties() {
		return true;
	}

	private void ApplyWheel() {
		#if UNITY_EDITOR
			if (wheelOverride != null) wheel = wheelOverride;
		#endif
		if (car) car.ApplyWheel(wheel);
		foreach (Wheel w in extraWheels) {
			w.ApplyCustomWheel(wheel);
		}
	}

	protected override void SaveToProperties(ref Dictionary<string, object> properties) {
		properties["wheelName"] = wheel.name;
	}

	public void SetWheel(CustomWheel newWheel) {
		wheel = newWheel;
		ApplyWheel();
	}

	public void SetWheelLocked(CustomWheel lockedWheel) {
		foreach (Wheel w in extraWheels) {
			w.ApplyCustomWheel(lockedWheel);
		}
	}

	public CustomWheel GetWheel() {
		return wheel;
	}
}
