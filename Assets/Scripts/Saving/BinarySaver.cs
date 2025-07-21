using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System;
using Unity.VisualScripting;
using System.Linq;

public class BinarySaver {
	string baseDataPath;
	string currentTrack;
	string applicationVersion;

	public BinarySaver(string currentTrack) {
		this.currentTrack = currentTrack;
		applicationVersion = Application.version;
		baseDataPath = Application.isEditor ? Application.streamingAssetsPath : Application.persistentDataPath;
	}

	string GetAuthorGhostPath(string trackName) {
		return Path.Combine(
			Application.streamingAssetsPath,
			"ghosts",
			trackName,
			"author.haunt"
		);
	}

	public Ghost GetAuthorGhost(string trackName = null) {
		string fn = GetAuthorGhostPath(trackName ?? currentTrack);
		Debug.Log("looking at author in path "+fn);
		Ghost g;
		try {
			g = LoadGhost(fn);
			if (!CompatibleVersions(g.version)) {
				Debug.Log("no compatible ghost version");
			}
		} catch (Exception e) {
			Debug.Log("error lading author ghost: " + e);
			return null;
		}
		return g;
	}

	public void DeleteAuthorGhost() {
		string fn = GetAuthorGhostPath(currentTrack);
		try {
			DeleteGhost(fn);
			Debug.Log("author ghost deleted at path "+fn);
		} catch (Exception e) {
			Debug.Log("some error deleting author ghost " + e);
		}
	}

	public void DeletePlayerGhost() {
		Directory.CreateDirectory(GetGhostFolder(false, currentTrack));
		string[] filenames = Directory.GetFiles(GetGhostFolder(false, currentTrack), "player.haunt");
		foreach (string fn in filenames) {
			try {
				File.Delete(fn);
				Debug.Log("deleted ghost "+fn);
			} catch (Exception e) {
				Debug.Log("error deleting ghostfile "+fn+"\n"+e);
			}
		}
	}

	public List<Ghost> GetGhosts(string trackName = null) {
		// list all files in the ghost folder
		Directory.CreateDirectory(GetGhostFolder(false, trackName ?? currentTrack));
		string[] filenames = Directory.GetFiles(GetGhostFolder(false, trackName ?? currentTrack), "player.haunt");
		List<Ghost> ghosts = new();
		foreach (string fn in filenames) {
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

	string GetGhostPath(string playerName, bool author, string trackName) {
		return Path.Combine(
			GetGhostFolder(author, trackName),
			playerName + ".haunt"
		);
	}

	string GetGhostFolder(bool author, string trackName) {
		return Path.Combine(
			author ? Application.streamingAssetsPath : baseDataPath,
			"ghosts",
			trackName
		);
	}

	public async void SaveGhost(Ghost g, bool author = false) {
		string filepath = GetGhostPath(g.playerName, author, currentTrack);
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

	void DeleteGhost(string path) {
		if (!path.EndsWith(".haunt")) {
			throw new Exception("trying to delete a non-ghost file");
		}
		File.Delete(path);
	}

	bool CompatibleVersions(string testVersion) {
        string[] saveVersion = testVersion.Split('.');
        string[] currentVersion = applicationVersion.Split('.');

        return saveVersion[0].Equals(currentVersion[0]);
    }
	
	public string GetMedalForTrack(string trackName) {
		Ghost authorGhost = GetAuthorGhost(trackName);
		if (authorGhost == null) return "";
		float authorTime = authorGhost.totalTime;
		var ghosts = GetGhosts(trackName).OrderBy(x => x.totalTime);
		if (ghosts.Count() == 0) return "";
		float playerTime = ghosts.First().totalTime;
		if (playerTime < authorTime) return "author";
		if (playerTime < authorTime * 1.1f) return "gold";
		if (playerTime < authorTime * 1.2f) return "silver";
		if (playerTime < authorTime * 1.5f) return "bronze";
		return "";
	}
}
