using UnityEngine;
using FYFY;
using System.IO;
using System.Xml;

public class ScenarioSystem : FSystem {
	private Family levelscenario_f = FamilyManager.getFamily(new AllOfComponents(typeof(LevelScenario)));
	private GameData gameData; 

	public ScenarioSystem()
	{
		if(Application.isPlaying)
		{
			gameData = GameObject.Find("GameData").GetComponent<GameData>();
			levelscenario_f.addEntryCallback(chooseNextLevel);

		}
	}

	// Choose next level according to last level's type or last level's score
	private void chooseNextLevel(GameObject unused)
    {
		//Debug.Log("chooseNextLevel");
		int scoredStars = PlayerPrefs.GetInt(gameData.levelToLoad.Item1 + Path.DirectorySeparatorChar + gameData.levelToLoad.Item2 + gameData.scoreKey, 0);

		// If last level is tutorial, go to next level
		if (gameData.tagsDictionary.ContainsValue("tutorial") || scoredStars == 2) {
			gameData.levelToLoad.Item2++;
			//Debug.Log("last level is tutorial, go to next level");
		}

		// If last level too hard (1 star) : choose previous level not done if it exists
		else if (scoredStars == 1) {
			//Debug.Log("1 star");
			int currentLevel = gameData.levelToLoad.Item2;
			int nextLevel = -1;
			for (int i = gameData.levelToLoad.Item2 - 1; i > 0; i--) {
				gameData.levelToLoad.Item2 = i;
				//level not done = zero star
				if (PlayerPrefs.GetInt(gameData.levelToLoad.Item1 + Path.DirectorySeparatorChar + gameData.levelToLoad.Item2 + gameData.scoreKey, 0) == 0) {
					nextLevel = i;
					//Debug.Log("previous level not done");
					break;
                }
			}
			//there is no previous level not done
			if (nextLevel == -1) {
				gameData.levelToLoad.Item2 = currentLevel + 1;
				//Debug.Log("no previous level not done");
			}
		}

		// If last level too easy (3 stars) : choose level before next tutorial if it exists
		else if (scoredStars == 3) {
			//Debug.Log("3 stars");
			//Debug.Log("current level = " + gameData.levelToLoad.Item2);
			int nextTuto = -1;
			for(int i = gameData.levelToLoad.Item2 + 1 ; i < gameData.levelList[gameData.levelToLoad.Item1].Count ; i++) {
				if (levelHasTag(gameData.levelToLoad.Item1, i, "tutorial")) {
					//Debug.Log("tuto = " + i);
					nextTuto = i;
					break;
                }
			}
			Debug.Log("nextTuto = " + nextTuto);
			//no tutorial found = go to next level
            if (nextTuto == -1) {
				gameData.levelToLoad.Item2++;
				//Debug.Log("no tutorial found = go to next level : "+ gameData.levelToLoad.Item2);
			}
            else {
				//level before next tutorial exists
				if (nextTuto > gameData.levelToLoad.Item2 + 1) {
					gameData.levelToLoad.Item2 = nextTuto - 1;
					//Debug.Log("level before next tutorial exists");
				}
				//next level = tuto
                else {
					gameData.levelToLoad.Item2 = nextTuto;
					//Debug.Log("next level = tuto");
				}
            }
		}
	}

	//check if level contains tag
	private bool levelHasTag(string directory, int levelIndex, string tagName) {
		//load xml level file
		XmlDocument doc = new XmlDocument();
		doc.Load(gameData.levelList[directory][levelIndex]);

		//get tags node
		XmlNode tags = doc.GetElementsByTagName("tags")[0];

		foreach (XmlNode tag in tags.ChildNodes) {
            if (tag.Attributes[0].Value.Equals(tagName)) {
				return true;
            }
		}

		return false;
	}
}