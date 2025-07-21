using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Threading.Tasks;

public class TrackLoadButton : MonoBehaviour {
	public SceneReference track;
	public Sprite trackImage;
	public Image trackPreviewThumbnail;
	public Text trackTitle;
	public Image bestMedal;
	
	string trackName;

	public Sprite bronze, silver, gold, author;

	void Start() {
		GetComponent<Button>().onClick.AddListener(() => FindObjectOfType<MainMenu>().LoadTrack(track));
		trackName = track.ScenePath.Split("/")[^1];
		trackTitle.text = trackName;
		trackPreviewThumbnail.sprite = trackImage;

		PopulateMedal();
	}

	async void PopulateMedal() {
		var medalTask = new TaskCompletionSource<string>();
		await Task.Run(() => {
			medalTask.SetResult(new BinarySaver("MAINMENU").GetMedalForTrack(trackName));
		});
		medalTask.Task.ConfigureAwait(true).GetAwaiter().OnCompleted(() => {
			string medalName = medalTask.Task.Result;
			if (medalName == "bronze") bestMedal.sprite = bronze;
			else if (medalName == "silver") bestMedal.sprite = silver;
			else if (medalName == "gold") bestMedal.sprite = gold;
			else if (medalName == "author") bestMedal.sprite = author;
			else bestMedal.enabled = false;
		});
	}
}
