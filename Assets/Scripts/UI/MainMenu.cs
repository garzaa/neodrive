using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(TransitionManager))]
public class MainMenu : SavedObject {
	public GameObject mainMenu;

	public List<GameObject> submenuList;
	readonly Dictionary<string, GameObject> menuNames = new();

	readonly Stack<GameObject> submenus = new();

	protected override void Initialize() {
		// need to build this stupid cache because
		// you can't find inactive gameobjects
		foreach (GameObject g in submenuList) {
			menuNames[g.GetHierarchicalName()] = g;
		}
	}

	protected override void LoadFromProperties() {
		// do nothing, can just read submenus in start()
	}

	protected override void SaveToProperties(ref Dictionary<string, object> properties) {
		properties["submenus"] = submenus.Reverse().Select(x => x.GetHierarchicalName()).ToArray();
	}

	void Start() {
		foreach (string menuObjectName in GetList<string>("submenus")) {
			OpenSubmenu(menuNames[menuObjectName]);
		}
	}

	public void Exit() {
		SaveManager.WriteEternalSave();
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
			GameObject g = submenus.Peek();
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
