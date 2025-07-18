using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System;

public class BinarySaver {
	string baseDataPath;
	string dataPath;
	string currentTrack;

	public BinarySaver(string currentTrack) {
		this.currentTrack = currentTrack;
		baseDataPath = Application.isEditor ? Application.dataPath : Application.persistentDataPath;
		dataPath = Application.dataPath;
	}

	public Ghost GetAuthorGhost() {
		string fn = Path.Combine(
			dataPath,
			"ghosts",
			currentTrack,
			"author.haunt"
		);
		Ghost g;
		try {
			g = LoadGhost(fn);
			if (!CompatibleVersions(g.version)) {
				Debug.Log("no compatible ghost version");
			}
		} catch (Exception e) {
			Debug.Log(e);
			return null;
		}
		return g;
	}

	public List<Ghost> GetGhosts() {
		// list all files in the ghost folder
		string[] filenames = Directory.GetFiles(GetGhostFolder(), "player.haunt");
		List<Ghost> ghosts = new();
		foreach (string fn in filenames) {
			Debug.Log("looking at ghost "+fn);
			try {
				Ghost g = LoadGhost(fn);
				if (CompatibleVersions(g.version)) {
					ghosts.Add(g);
				}
			} catch (Exception e) {
				Debug.Log("invalid ghostfile "+fn+"\n"+e);
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
