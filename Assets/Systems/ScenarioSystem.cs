using UnityEngine;
using FYFY;

public class ScenarioSystem : FSystem {

	public static ScenarioSystem instance;
	private GameData gameData; 

	public ScenarioSystem()
	{
		if(Application.isPlaying)
		{
			gameData = GameObject.Find("GameData").GetComponent<GameData>();

		}
		instance = this;
	}

	// Use this to update member variables when system pause. 
	// Advice: avoid to update your families inside this function.
	protected override void onPause(int currentFrame) {
	}

	// Use this to update member variables when system resume.
	// Advice: avoid to update your families inside this function.
	protected override void onResume(int currentFrame){
	}

	// Use to process your families.
	protected override void onProcess(int familiesUpdateCount) {
	}

	// Choose next level according to last level's type or last level's score
	private void chooseNextLevel()
    {
		// If last level is tutorial, go to next level
		if (gameData.tagsDictionary.ContainsValue("tutorial"))
			gameData.levelToLoad.Item2++;

		// If last level too easy (3 stars) : choose level before next tutorial if it exists
		else if(true) {

        } 
		// If last level too hard (1 star) : choose previous level not done if it exists
		else if(true)
        {

        }

    }
}