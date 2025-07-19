using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class RoadSupport : MonoBehaviour {
	public GameObject b;
	
	void Start() {
		BuildSupport();
	}

	void Update() {
		if (Application.isEditor) BuildSupport();
	}

	void BuildSupport() {
		// goddamn is there really no way to do this
		// can't rotate or scale it without it getting screwy
		if (Physics.Raycast(b.transform.position, Vector3.down, out RaycastHit hit, 100))
		{
			b.transform.localScale = new Vector3(
				70,
				70,
				(hit.distance+2f) * 100
			);
			b.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
		}
	}
}
