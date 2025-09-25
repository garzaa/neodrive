using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class FinishLine : MonoBehaviour {
	public UnityEvent onFinishCross;

	void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Player")) {
			onFinishCross.Invoke();
		}
	}
}
