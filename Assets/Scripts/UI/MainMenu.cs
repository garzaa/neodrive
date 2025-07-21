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
		SceneManager.LoadScene(track);
	}

	void Update() {
		if (InputManager.ButtonDown(Buttons.UICANCEL)) {
			CloseSubmenu();
		}
	}

	public void OpenSubmenu(GameObject submenu) {
		submenus.Push(submenu);
		submenu.SetActive(true);
		submenu.GetComponentInChildren<Selectable>().Select();
	}

	public void CloseSubmenu() {
		if (submenus.Count > 1) {
			submenus.Pop().SetActive(false);
			GameObject g = submenus.Pop();
			g.SetActive(true);
			g.GetComponentInChildren<Selectable>().Select();
		} else if (submenus.Count == 1) {
			submenus.Pop().SetActive(false);
			mainMenu.SetActive(true);
			mainMenu.GetComponentInChildren<Selectable>().Select();
		}
	}
}
