using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class TrackLoadButton : MonoBehaviour {
	public SceneReference track;
	public Sprite trackImage;
	public Image trackPreviewThumbnail;
	public Text trackTitle;
	
	string trackName;

	public GameObject bronze, silver, gold, author;

	BinarySaver bs;

	void Start() {
		GetComponent<Button>().onClick.AddListener(() => FindObjectOfType<MainMenu>().LoadTrack(track));
		trackName = track.ScenePath.Split("/")[^1].Split(".unity")[0];
		print("setting trakc title to " + trackName);
		trackTitle.text = trackName;
		trackPreviewThumbnail.sprite = trackImage;

		foreach (GameObject g in new GameObject[]{author, gold, silver, bronze}) {
			g.SetActive(false);
		}

		bs = new BinarySaver("MAINMENU");
		PopulateMedals();
	}

	async void PopulateMedals() {
		print("loading medals...");
		var medalTask = new TaskCompletionSource<string>();
		await Task.Run(() => {
			medalTask.SetResult(bs.GetMedalForTrack(trackName));
		});
		medalTask.Task.ConfigureAwait(true).GetAwaiter().OnCompleted(() => {
			print("loaded medals");
			string medalName = medalTask.Task.Result;
			print("medal name: "+medalName);
			switch (medalName) {
				case "author":
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
					break;
				default:
					break;
			}
		});
	}
}
