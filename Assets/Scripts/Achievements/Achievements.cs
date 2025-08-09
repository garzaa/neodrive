using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class Achievements : SavedObject {

#if UNITY_EDITOR
	[MenuItem("GameObject/Neodrive/Create Achievement")]
	public static void CreateAchievement() {
		Achievement a = ScriptableObject.CreateInstance<Achievement>();
		AssetDatabase.CreateAsset(a, "Assets/Resources/Runtime/Achievements/NewAchievement.asset");
		EditorUtility.FocusProjectWindow();
		Selection.activeObject = a;
	}
#endif

	HashSet<string> achievements = new();
	// TODO: if there's only one track left to get bronze/silver/gold/author on, save its name?
	// what if the tracks get updated
	// although hmm, ok. just repopoulate this in the main menu, ez
	string lastBronze;
	string lastSilver;
	string lastGold;
	string lastAuthor;
	Achievement[] loadedAchievements = null;
	Dictionary<string, Achievement> stringNames = new();

	public Transform unlockedContainer;
	public Transform lockedContainer;
	public GameObject achievementPrefab;

	public Animator animator;
	public Text unlockTitle;
	public Image unlockIcon;

	public Text numUnlocked;
	int unlockedCount;
	int totalCount;

	protected override void Initialize() {
		loadedAchievements ??= Resources.LoadAll<Achievement>("Achievements");
	}

    protected override void LoadFromProperties() {
        achievements = GetHashSet<string>(nameof(achievements));
		ListAchievements();
    }

    protected override void SaveToProperties(ref Dictionary<string, object> properties) {
        properties[nameof(achievements)] = achievements;
    }

	public bool Has(Achievement a) {
		return achievements.Contains(a.GetName());
	}

	public void Get(Achievement a) {
		if (!Has(a)) {
			achievements.Add(a.GetName());
			NotifyUnlock(a);
		}
	}

	public void Get(string s) {
		Get(stringNames[s]);
	}

	public void NotifyUnlock(Achievement a) {
#if !STEAM
		unlockTitle.text = a.GetName();
		unlockIcon.sprite = a.Icon;
		animator.SetTrigger("Unlock");
#endif
	}

	public void ListAchievements() {
		UtilityMethods.ClearUIList(lockedContainer);
		UtilityMethods.ClearUIList(unlockedContainer);
		unlockedCount = achievements.Count;
		totalCount = loadedAchievements.Length;
		numUnlocked.text = "Achievements"; // $"Unlocked: {unlockedCount}/{totalCount}";
		foreach (Achievement a in loadedAchievements) {
			if (a.hidden) continue;
			AddUIPrefab(a);
			stringNames[a.GetName()] = a;
		}
	}

	void AddUIPrefab(Achievement a) {
		GameObject g = Instantiate(achievementPrefab, lockedContainer);
		Text[] textObjects = g.GetComponentsInChildren<Text>();
		Image[] images = g.GetComponentsInChildren<Image>();
		textObjects[0].text = a.GetName();
		textObjects[1].text = a.Description;
		images[2].sprite = a.Icon;
		totalCount++;
		if (Has(a)) {
			unlockedCount++;
			g.transform.SetParent(unlockedContainer, worldPositionStays: false);
		} else {
			if (a.Secret) {
				textObjects[1].text = "???";
			}
			textObjects[0].color = new Color32(100, 100, 100, 255);
			textObjects[1].color = new Color32(100, 100, 100, 255);
			images[0].color = new Color32(50, 50, 50, 255);
			images[2].color = new Color32(255, 255, 255, 50);
		}
	}

}	
