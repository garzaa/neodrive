using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System;
using Unity.VisualScripting;

public class BinarySaver {
	string baseDataPath;
	string currentTrack;

	public BinarySaver(string currentTrack) {
		this.currentTrack = currentTrack;
		baseDataPath = Application.isEditor ? Application.streamingAssetsPath : Application.persistentDataPath;
	}

	public Ghost GetAuthorGhost() {
		string fn = Path.Combine(
			Application.streamingAssetsPath,
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
		Directory.CreateDirectory(GetGhostFolder(false));
		string[] filenames = Directory.GetFiles(GetGhostFolder(false), "player.haunt");
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

	string GetGhostPath(string playerName, bool author) {
		return Path.Combine(
			GetGhostFolder(author),
			playerName + ".haunt"
		);
	}

	string GetGhostFolder(bool author) {
		return Path.Combine(
			author ? Application.streamingAssetsPath : baseDataPath,
			"ghosts",
			currentTrack
		);
	}

	public async void SaveGhost(Ghost g, bool author = false) {
		string filepath = GetGhostPath(g.playerName, author);
		await Task.Run(() => {
			// have to call it manually. insane
			g.OnBeforeSerialize();
			Directory.CreateDirectory(Path.GetDirectoryName(filepath));
			FileStream dataStream = new(filepath, FileMode.Create);
			BinaryFormatter formatter = new();
			formatter.Serialize(dataStream, g);
			dataStream.Close();
		});
		Debug.Log("ghost saved at "+filepath);
	}

	Ghost LoadGhost(string path) {
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		FileStream dataStream = new(path, FileMode.Open);
		BinaryFormatter converter = new();
		Ghost g = converter.Deserialize(dataStream) as Ghost;
		dataStream.Close();
		g.OnAfterDeserialize();
		return g;
	}

	bool CompatibleVersions(string testVersion) {
        string[] saveVersion = testVersion.Split('.');
        string[] currentVersion = Application.version.Split('.');

        return saveVersion[0].Equals(currentVersion[0]);
    }
}
