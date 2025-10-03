using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using NaughtyAttributes;
using System;

// TODO: eventually, scroll in the scroll view. HOUGH
public class TrackLoadButton : MonoBehaviour {
	public SceneReference track;
	public Sprite trackImage;

	[Foldout("Internal")]
	public Image trackPreviewThumbnail;
	[Foldout("Internal")]
	public Text trackTitle;
	[Foldout("Internal")]
	public Text trackNumber;
	[Foldout("Internal")]
	public GameObject bronze, silver, gold, author;
	
	string trackName;
	BinarySaver bs;

	void Start() {
		GetComponent<Button>().onClick.AddListener(() => FindObjectOfType<MainMenu>().LoadTrack(track));
		trackName = track.ScenePath.Split("/")[^1].Split(".unity")[0];
		string stageNumber = (transform.parent.GetSiblingIndex()+1).ToString();
		trackNumber.text = stageNumber + "-" + (transform.GetSiblingIndex()+1).ToString("D2");
		trackTitle.text = trackName;
		trackPreviewThumbnail.sprite = trackImage;

		foreach (GameObject g in new GameObject[]{author, gold, silver, bronze}) {
			g.SetActive(false);
		}

		bs = new BinarySaver("MAINMENU");
		PopulateMedals();
	}

	void OnValidate() {
		if (track != null) {
			trackName = track.ScenePath.Split("/")[^1].Split(".unity")[0];
			trackTitle.text = trackName;
			name = trackName;
		}

		if (trackImage != null) {
			trackPreviewThumbnail.sprite = trackImage;
		}
	}

	async void PopulateMedals() {
		var medalTask = new TaskCompletionSource<string>();
		await Task.Run(() => {
			medalTask.SetResult(bs.GetMedalForTrack(trackName));
		});
		medalTask.Task.ConfigureAwait(true).GetAwaiter().OnCompleted(() => {
			try {
				string medalName = medalTask.Task.Result;
				switch (medalName) {
					case "author":
						FindObjectOfType<RaceLogic>().firstAuthorAchievement.Get();
						author.SetActive(true);
						goto case "gold";
					case "gold":
						gold.SetActive(true);
						goto case "silver";
					case "silver": 
						silver.SetActive(true);
						goto case "bronze";
					case "bronze":
						bronze.SetActive(true);
						print("got a medal");
						break;
					default:
						break;
				}
			} catch (NullReferenceException) {
				// if the buttons finish loading after going to a new scene
				// easy hack so we don't interrupt the async bs
			}
		});
	}
}
