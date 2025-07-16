using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

public class BinarySaver {
	string baseDataPath;
	string currentTrack;

	public BinarySaver(string currentTrack) {
		this.currentTrack = currentTrack;
		baseDataPath = Application.isEditor ? Application.dataPath : Application.persistentDataPath;
	}

	public List<Ghost> GetGhosts() {
		// list all files in the ghost folder
		string[] filenames = System.IO.Directory.GetFiles(GetGhostFolder(), "*.haunt");
		List<Ghost> ghosts = new();
		foreach (string fn in filenames) {
			Ghost g = LoadGhost(fn);
			if (CompatibleVersions(g.version)) {
				ghosts.Add(g);
			}
		}
		return ghosts;
	}

	string GetGhostPath(string playerName) {
		return Path.Combine(
			GetGhostFolder(),
			playerName + ".haunt"
		);
	}

	string GetGhostFolder() {
		return Path.Combine(
			baseDataPath,
			"ghosts",
			currentTrack
		);
	}

	public async void SaveGhost(Ghost g) {
		string filepath = GetGhostPath(g.playerName);
		await Task.Run(() => {
			Directory.CreateDirectory(Path.GetDirectoryName(filepath));
			FileStream dataStream = new(filepath, FileMode.Create);
			BinaryFormatter formatter = new();
			formatter.Serialize(dataStream, g);
			dataStream.Close();
		});
		Debug.Log("ghost saved at "+filepath);
	}

	Ghost LoadGhost(string path) {
		FileStream dataStream = new(path, FileMode.Open);
		BinaryFormatter converter = new();
		Ghost g = converter.Deserialize(dataStream) as Ghost;
		dataStream.Close();
		return g;
	}

	bool CompatibleVersions(string testVersion) {
        string[] saveVersion = testVersion.Split('.');
        string[] currentVersion = Application.version.Split('.');

        return saveVersion[0].Equals(currentVersion[0]);
    }
}
