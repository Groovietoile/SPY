using UnityEngine;
using FYFY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using TMPro;
using System.Xml;
using System.Linq;

/// <summary>
/// Manage main menu to launch a specific mission
/// </summary>
public class TitleScreenSystem : FSystem {
	private GameData gameData;
	private GameObject campagneMenu;
	private GameObject playButton;
	private GameObject quitButton;
	private GameObject backButton;
	private GameObject canvas;
	private GameObject cList;
	private Dictionary<GameObject, List<GameObject>> levelButtons; //key = directory button,  value = list of level buttons
	private TMP_InputField inputName; // Where the user input their name
	private GameObject inputField;
	private GameObject opacityToggle;
	private Family buttons_f = FamilyManager.getFamily(new AnyOfComponents(typeof(Button), typeof(Toggle)));

	public TitleScreenSystem(){
		if (Application.isPlaying)
		{
			gameData = GameObject.Find("GameData").GetComponent<GameData>();
			gameData.levelList = new Dictionary<string, List<string>>();
			campagneMenu = GameObject.Find("CampagneMenu");
			playButton = GameObject.Find("Jouer");
			quitButton = GameObject.Find("Quitter");
			backButton = GameObject.Find("Retour");
			canvas = GameObject.Find("Canvas");
			GameObjectManager.dontDestroyOnLoadAndRebind(GameObject.Find("GameData"));

			cList = GameObject.Find("CampagneList");
			levelButtons = new Dictionary<GameObject, List<GameObject>>();

			GameObjectManager.setGameObjectState(campagneMenu, false);
			GameObjectManager.setGameObjectState(backButton, false);
			string levelsPath = Application.streamingAssetsPath + Path.DirectorySeparatorChar + "Levels";
			List<string> levels;
			foreach (string directory in Directory.GetDirectories(levelsPath))
			{
				levels = readScenario(directory);
				if (levels != null)
				{
					gameData.levelList[Path.GetFileName(directory)] = levels; //key = directory name
				}
			}

			inputName = GameObject.Find("InputName").GetComponent<TMP_InputField>();
			inputField = GameObject.Find("InputName");
			opacityToggle = GameObject.Find("OpacityToggle");
			//create level directory buttons
			foreach (string key in gameData.levelList.Keys)
			{
				GameObject directoryButton = Object.Instantiate<GameObject>(Resources.Load("Prefabs/Button") as GameObject, cList.transform);
				directoryButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = key;
				levelButtons[directoryButton] = new List<GameObject>();
				GameObjectManager.bind(directoryButton);
				// add on click
				directoryButton.GetComponent<Button>().onClick.AddListener(delegate {
					showLevels(directoryButton);
					canvas.GetComponent<AudioSource>().Play();
				});
				// create level buttons
				for (int i = 0; i < gameData.levelList[key].Count; i++)
				{
					GameObject button = Object.Instantiate<GameObject>(Resources.Load("Prefabs/LevelButton") as GameObject, cList.transform);
					button.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() => GameObjectManager.addComponent<LRS_levelButton>(button.transform.Find("Button").gameObject));
					button.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() => canvas.GetComponent<AudioSource>().Play());
					button.transform.Find("Button").GetChild(0).GetComponent<TextMeshProUGUI>().text = Path.GetFileNameWithoutExtension(gameData.levelList[key][i]);
					int indice = i;
					button.transform.Find("Button").GetComponent<Button>().onClick.AddListener(delegate { launchLevel(key, indice); });
					levelButtons[directoryButton].Add(button);
					GameObjectManager.bind(button);
					GameObjectManager.setGameObjectState(button, false);
				}
			}
			if(gameData.userName != "") {
				Debug.Log("username = "+gameData.userName);
				GameObjectManager.setGameObjectState(inputField, false);
				playButton.GetComponent<Button>().interactable = true;
			}

			MainLoop.instance.StartCoroutine(delaySetOpacity());
		}
	}

	private IEnumerator delaySetOpacity() {
		yield return null;
		foreach (GameObject bouton in buttons_f) {
			setOpacity(bouton);
		}
	}

	// Save user name in GameData
	public void saveUserName()
	{
		if(gameData.userName == "") {
			gameData.userName = inputName.text;
		}

	}

	public void userNameChanged() {
		if(inputName.text.Length > 0) {
			playButton.GetComponent<Button>().interactable = true;
        }
        else {
			playButton.GetComponent<Button>().interactable = false;
		}
    }

	public void setOpacity(GameObject bouton) {
		Color buttonColor;

		if (PlayerPrefs.GetString(gameData.opacityKey).Equals("off")) {
			buttonColor = bouton.GetComponent<Image>().color;
			buttonColor.a = 255;
			bouton.GetComponent<Image>().color = buttonColor;
			bouton.GetComponent<Image>().sprite = Resources.Load<Sprite>("UI Images/background.png");
			opacityToggle.GetComponent<Toggle>().isOn = false;
		}

		else { //on = default value
			buttonColor = bouton.GetComponent<Image>().color;
			buttonColor.a = 129;
			bouton.GetComponent<Image>().color = buttonColor;

			// Load all sprites in atlas
			Sprite[] atlas = Resources.LoadAll<Sprite>("Sci-Fi UI/_SciFi_GUISkin_/atlas");

			bouton.GetComponent<Image>().sprite = atlas.Single(s => s.name == "progress_bar");
			opacityToggle.GetComponent<Toggle>().isOn = true;
		}
	}

	public void changeOpacity() {
		if (opacityToggle.GetComponent<Toggle>().isOn) {
			PlayerPrefs.SetString(gameData.opacityKey, "on");
		}
		else {
			PlayerPrefs.SetString(gameData.opacityKey, "off");
		}
		foreach (GameObject bouton in buttons_f) {
			setOpacity(bouton);
		}

	}
	private List<string> readScenario(string repositoryPath){
		if(File.Exists(repositoryPath+Path.DirectorySeparatorChar+"Scenario.xml")){
			List<string> levelList = new List<string>();
			XmlDocument doc = new XmlDocument();
			doc.Load(repositoryPath+Path.DirectorySeparatorChar+"Scenario.xml");
			XmlNode root = doc.ChildNodes[1]; //root = <scenario/>
			foreach(XmlNode child in root.ChildNodes){
				if (child.Name.Equals("level")){
					levelList.Add(repositoryPath + Path.DirectorySeparatorChar + (child.Attributes.GetNamedItem("name").Value));
				}
			}
			return levelList;			
		}
		return null;
	}

	protected override void onProcess(int familiesUpdateCount) {
		if(Input.GetButtonDown("Cancel")){
			Application.Quit();
		}
	}

	public void showCampagneMenu(){
		GameObjectManager.setGameObjectState(campagneMenu, true);
		foreach(GameObject directory in levelButtons.Keys){
			//show directory buttons
			GameObjectManager.setGameObjectState(directory, true);
			//hide level buttons
			foreach(GameObject level in levelButtons[directory]){
				GameObjectManager.setGameObjectState(level, false);
			}
		}
		GameObjectManager.setGameObjectState(playButton, false);
		GameObjectManager.setGameObjectState(quitButton, false);
		GameObjectManager.setGameObjectState(backButton, true);
		GameObjectManager.setGameObjectState(inputField, false);
		GameObjectManager.setGameObjectState(opacityToggle, false);
	}

	private void showLevels(GameObject levelDirectory){
		//show/hide levels
		foreach(GameObject directory in levelButtons.Keys){
			//hide level directories
			GameObjectManager.setGameObjectState(directory, false);
			//show levels
			if(directory.Equals(levelDirectory)){
				//foreach(GameObject go in levelButtons[directory]){
				for(int i = 0 ; i < levelButtons[directory].Count ; i ++){
					GameObjectManager.setGameObjectState(levelButtons[directory][i], true);

					string directoryName = levelDirectory.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;
					//locked levels
					/*
					if(i > PlayerPrefs.GetInt(directoryName, 0)) //by default first level of directory is the only unlocked level of directory
						levelButtons[directory][i].transform.Find("Button").GetComponent<Button>().interactable = false;
					//unlocked levels
					else{
					*/
					levelButtons[directory][i].transform.Find("Button").GetComponent<Button>().interactable = true;
					//scores
					int scoredStars = PlayerPrefs.GetInt(directoryName + Path.DirectorySeparatorChar + i + gameData.scoreKey, 0); //0 star by default
					Transform scoreCanvas = levelButtons[directory][i].transform.Find("ScoreCanvas");
					for (int nbStar = 0 ; nbStar < 4 ; nbStar++){
						if(nbStar == scoredStars)
							GameObjectManager.setGameObjectState(scoreCanvas.GetChild(nbStar).gameObject, true);
						else
							GameObjectManager.setGameObjectState(scoreCanvas.GetChild(nbStar).gameObject, false);
					}
					//}
				}
			}
			//hide other levels
			else{
				foreach(GameObject go in levelButtons[directory]){
					GameObjectManager.setGameObjectState(go, false);
				}
			}
		}
	}

	public void launchLevel(string levelDirectory, int level){
		gameData.levelToLoad = (levelDirectory,level);
		MainLoop.instance.StartCoroutine(loadMainScene());
	}

	private IEnumerator loadMainScene() {
		yield return null;
		GameObjectManager.loadScene("MainScene");
	}

	// See Retour button in editor
	public void backFromCampagneMenu(){
		foreach(GameObject directory in levelButtons.Keys){
			if(directory.activeSelf){
				//main menu
				GameObjectManager.setGameObjectState(campagneMenu, false);
				GameObjectManager.setGameObjectState(playButton, true);
				GameObjectManager.setGameObjectState(quitButton, true);
				GameObjectManager.setGameObjectState(backButton, false);
				GameObjectManager.setGameObjectState(opacityToggle, true);
				playButton.GetComponent<Button>().interactable = true;
				break;
			}
			else{
				//show directory buttons
				GameObjectManager.setGameObjectState(directory, true);
				//hide level buttons
				foreach(GameObject go in levelButtons[directory]){
					GameObjectManager.setGameObjectState(go, false);
				}
			}
		}
	}

	// See Quitter button in editor
	public void quitGame(){
		Application.Quit();
	}
}