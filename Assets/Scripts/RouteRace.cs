using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class RouteRace : MonoBehaviour {
	FinishLine finishLine;
	Car car;

	public Animator countdownAnimator;
	public Text countdown;


	void Awake() {
		finishLine = FindObjectOfType<FinishLine>();
		car = FindObjectOfType<Car>();
	}

	void Start() {
		car.onRespawn.AddListener(() => StartCoroutine(CountdownAndStart()));
		car.onEngineStart.AddListener(() => StartCoroutine(FirstStart()));
		car.forceClutch = true;
	}

	IEnumerator FirstStart() {
		yield return new WaitForSeconds(1);
		StartCoroutine(CountdownAndStart());
	}

	IEnumerator CountdownAndStart() {
		car.ChangeGear(1);
		car.forceClutch = true;
		yield return new WaitForSeconds(0.25f);
		countdown.text = "3";
		countdownAnimator.SetTrigger("Animate");
		yield return new WaitForSeconds(0.5f);
		countdown.text = "2";
		countdownAnimator.SetTrigger("Animate");
		yield return new WaitForSeconds(0.5f);
		countdown.text = "1";
		countdownAnimator.SetTrigger("Animate");
		yield return new WaitForSeconds(0.5f);
		countdown.text = "GO";
		countdownAnimator.SetTrigger("Animate");
		car.forceClutch = false;
	}

	public void OnRouteFinish() {
		// stop the car, freeze inputs, show results
	}
}
