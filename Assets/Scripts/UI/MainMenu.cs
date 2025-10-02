using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TransitionManager))]
public class MainMenu : SavedObject {
	public GameObject mainMenu;

	public List<GameObject> submenuList;
	readonly Dictionary<string, GameObject> menuNames = new();

	readonly Stack<GameObject> submenus = new();
	bool fromTrack = false;
	List<string> submenuNameList = new();

	protected override void Initialize() {
		// need to build this stupid cache because
		// you can't find inactive gameobjects
		foreach (GameObject g in submenuList) {
			menuNames[g.GetHierarchicalName()] = g;
		}
		Application.wantsToQuit += UnsetFromTrack;
	}

	protected override void LoadFromProperties() {
		fromTrack = Get<bool>("fromTrack");
		submenuNameList = GetList<string>("submenus");
	}

	protected override void SaveToProperties(ref Dictionary<string, object> properties) {
		// do this so it doesn't interfere if the main menu is in a world level
		// and syncs its fromTrack bool, don't sync an empty submenu list as well. christ
		properties["submenus"] = submenuNameList;
		properties[nameof(fromTrack)] = fromTrack;
	}

	void Start() {
		if (fromTrack && gameObject.activeSelf) {
			foreach (string menuObjectName in GetList<string>("submenus")) {
				OpenSubmenu(menuNames[menuObjectName]);
			}
			fromTrack = false;
		}
	}
	
	public void SetFromTrack() {
		fromTrack = true;
	}

	public bool UnsetFromTrack() {
		fromTrack = false;
		return true;
	}

	public void Exit() {
		UnsetFromTrack();
		SaveManager.WriteEternalSave();
		Application.Quit();
	}

	public void LoadTrack(string track) {
		StartCoroutine(Load(track));
	}
	
	IEnumerator Load(string track) {
		yield return new WaitForEndOfFrame();
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
		submenuNameList = submenus.Reverse().Select(x => x.GetHierarchicalName()).ToList();
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
		submenuNameList = submenus.Reverse().Select(x => x.GetHierarchicalName()).ToList();
	}

	IEnumerator SelectNextFrame(GameObject parent) {
		yield return new WaitForEndOfFrame();
		if (!parent.GetComponent<SetFirstSelected>()) parent.GetComponentInChildren<Selectable>().Select();
	}
}
