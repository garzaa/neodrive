using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(TransitionManager))]
public class MainMenu : MonoBehaviour {
	TransitionManager tm;
	public GameObject mainMenu;

	readonly Stack<GameObject> submenus = new();

	void Start() {
		tm = GetComponent<TransitionManager>();
	}

	public void Exit() {
		Application.Quit();
	}

	public void LoadTrack(string track) {
		SaveManager.LoadScene(track);
	}

	void Update() {
		if (InputManager.ButtonDown(Buttons.UICANCEL)) {
			CloseSubmenu();
		}
	}

	public void OpenSubmenu(GameObject submenu) {
		if (submenus.Count > 0) {
			submenus.Peek().SetActive(false);
		} else {
			mainMenu.SetActive(false);
		}
		submenus.Push(submenu);
		submenu.SetActive(true);
		StartCoroutine(SelectNextFrame(submenu));
	}

	public void CloseSubmenu() {
		if (submenus.Count > 1) {
			submenus.Pop().SetActive(false);
			GameObject g = submenus.Pop();
			g.SetActive(true);
			StartCoroutine(SelectNextFrame(g));
		} else if (submenus.Count == 1) {
			submenus.Pop().SetActive(false);
			mainMenu.SetActive(true);
			StartCoroutine(SelectNextFrame(mainMenu));
		}
	}

	IEnumerator SelectNextFrame(GameObject parent) {
		yield return new WaitForEndOfFrame();
		parent.GetComponentInChildren<Selectable>().Select();
	}
}
