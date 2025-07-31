using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarCustomization : SavedObject {
	// just wheels for now
	public CustomWheel wheel;

	public CustomWheel LoadWheel(string wheelName) {
		return Resources.Load<CustomWheel>("Wheels/"+wheelName);
	}

	protected override void LoadFromProperties() {
		wheel = LoadWheel(Get<string>("wheelName"));
		GetComponent<Car>().ApplyWheel(wheel);
	}

	protected override void SaveToProperties(ref Dictionary<string, object> properties) {
		properties["wheelName"] = wheel.GetName();
	}

}
