using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

public class BinarySaver {
	readonly string baseDataPath = Application.dataPath;
	string currentScene;

	public BinarySaver(string currentScene) {
		this.currentScene = currentScene;
	}

	public async void SaveGhost(Ghost g) {
		string filepath = Path.Combine(
			baseDataPath,
			"ghosts",
			currentScene + "_" + g.playerName + ".ghost"
		);
		await Task.Run(() => {
			Directory.CreateDirectory(Path.GetDirectoryName(filepath));
			FileStream dataStream = new FileStream(filepath, FileMode.Create);
			BinaryFormatter formatter = new();
			formatter.Serialize(dataStream, g);
			dataStream.Close();
		});
		Debug.Log("ghost saved at "+filepath);
	}
}
