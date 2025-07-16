using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class RouteRace : MonoBehaviour {
	FinishLine finishLine;
	RaceLogic raceLogic;
	Car car;

	public Animator countdownAnimator;
	public Text countdown;

	void Awake() {
		finishLine = FindObjectOfType<FinishLine>();
		car = FindObjectOfType<Car>();
	}

	void Start() {
		car.onRespawn.AddListener(() => StartCoroutine(CountdownAndStart()));
		car.onEngineStart.AddListener(FirstStart);
		car.forceClutch = true;
		finishLine.onValidFinish.AddListener(OnRouteFinish);
		raceLogic = FindObjectOfType<RaceLogic>();
	}

	public void FirstStart() {
		StartCoroutine(FirstStartRoutine());
	}

	IEnumerator FirstStartRoutine() {
		car.onEngineStart.RemoveListener(FirstStart);
		yield return new WaitForSeconds(1);
		StartCoroutine(CountdownAndStart());
	}

	IEnumerator CountdownAndStart() {
		raceLogic.HideResults();
		StopCoroutine(ShowResults());
		car.forceBrake = true;
		car.forceClutch = true;
		car.ChangeGear(1);
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
		car.forceClutch = false;
		car.forceBrake = false;
		countdown.text = "GO";
		countdownAnimator.SetTrigger("Animate");
	}

	public void OnRouteFinish() {
		// stop the car (force ebrake and neutral), show results, quit button
		car.forceClutch = true;
		car.forceBrake = true;
		StartCoroutine(ShowResults());
	}

	IEnumerator ShowResults() {
		yield return new WaitForSeconds(0.5f);
		ShowResults();
	}
}
