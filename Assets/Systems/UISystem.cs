using UnityEngine;
using FYFY;
using FYFY_plugins.PointerManager;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.IO;
using System.Collections;
using System.Linq;

/// <summary>
/// Manage dialogs at the begining of the level
/// Manage InGame UI (Play/Pause/Stop, reset, go back to main menu...)
/// Manage history
/// Manage end panel (compute Score and stars)
/// </summary>
public class UISystem : FSystem {
    private Family requireEndPanel = FamilyManager.getFamily(new AllOfComponents(typeof(NewEnd)), new NoneOfProperties(PropertyMatcher.PROPERTY.ACTIVE_IN_HIERARCHY));
    private Family displayedEndPanel = FamilyManager.getFamily(new AllOfComponents(typeof(NewEnd), typeof(AudioSource)), new AllOfProperties(PropertyMatcher.PROPERTY.ACTIVE_IN_HIERARCHY));
	private Family playerGO = FamilyManager.getFamily(new AllOfComponents(typeof(ScriptRef),typeof(Position)), new AnyOfTags("Player"));
	private Family editableScriptContainer = FamilyManager.getFamily(new AllOfComponents(typeof(UITypeContainer), typeof(VerticalLayoutGroup), typeof(CanvasRenderer), typeof(PointerSensitive)));
	private Family actions = FamilyManager.getFamily(new AllOfComponents(typeof(PointerSensitive), typeof(UIActionType)));
    private Family currentActions = FamilyManager.getFamily(new AllOfComponents(typeof(BasicAction),typeof(UIActionType), typeof(CurrentAction)));
	private Family newEnd_f = FamilyManager.getFamily(new AllOfComponents(typeof(NewEnd)));
	private Family resetBlocLimit_f = FamilyManager.getFamily(new AllOfComponents(typeof(ResetBlocLimit)));
	private Family scriptIsRunning = FamilyManager.getFamily(new AllOfComponents(typeof(PlayerIsMoving)));
	private Family emptyPlayerExecution = FamilyManager.getFamily(new AllOfComponents(typeof(EmptyExecution))); 
	private Family agents = FamilyManager.getFamily(new AllOfComponents(typeof(ScriptRef)));
	private Family libraryPanel = FamilyManager.getFamily(new AllOfComponents(typeof(GridLayoutGroup)));
	private Family buttons_f = FamilyManager.getFamily(new AllOfComponents(typeof(Button)));
	private GameData gameData;
	private GameObject dialogPanel;
	private int nDialog = 0;
	private GameObject buttonPlay;
	private GameObject buttonContinue;
	private GameObject buttonStop;
	private GameObject buttonReset;
	private GameObject buttonPause;
	private GameObject buttonStep;
	private GameObject buttonSpeed;
	private GameObject lastEditedScript;
	private GameObject endPanel;
	private GameObject executionCanvas;
	
	// Animated text
	private string originalText;
	TextMeshProUGUI uiText;
	private bool originalTextDisplayed;
	private float delay;

	public UISystem()
	{
		if (Application.isPlaying)
		{
			gameData = GameObject.Find("GameData").GetComponent<GameData>();

			executionCanvas = GameObject.Find("ExecutionCanvas");
			buttonPlay = executionCanvas.transform.Find("ExecuteButton").gameObject;
			buttonContinue = executionCanvas.transform.Find("ContinueButton").gameObject;
			buttonStop = executionCanvas.transform.Find("StopButton").gameObject;
			buttonPause = executionCanvas.transform.Find("PauseButton").gameObject;
			buttonStep = executionCanvas.transform.Find("NextStepButton").gameObject;
			buttonSpeed = executionCanvas.transform.Find("SpeedButton").gameObject;

			buttonReset = GameObject.Find("ResetButton");
			endPanel = GameObject.Find("EndPanel");
			GameObjectManager.setGameObjectState(endPanel.transform.parent.gameObject, false);
			dialogPanel = GameObject.Find("DialogPanel");
			GameObjectManager.setGameObjectState(dialogPanel.transform.parent.gameObject, false);

			MainLoop.instance.StartCoroutine(delaySetOpacity());
			requireEndPanel.addEntryCallback(displayEndPanel);
			displayedEndPanel.addEntryCallback(onDisplayedEndPanel);
			actions.addEntryCallback(linkTo);
			newEnd_f.addEntryCallback(levelFinished);
			resetBlocLimit_f.addEntryCallback(delegate (GameObject go) { destroyScript(go, true); });
			scriptIsRunning.addExitCallback(delegate { setExecutionState(true); });
			scriptIsRunning.addExitCallback(saveHistory);
			emptyPlayerExecution.addEntryCallback(delegate { setExecutionState(true); });
			emptyPlayerExecution.addEntryCallback(delegate { GameObjectManager.removeComponent<EmptyExecution>(MainLoop.instance.gameObject); });

			currentActions.addEntryCallback(onNewCurrentAction);

			lastEditedScript = null;

			loadHistory();

			//initialize time elapsed in level to 0 when mainsene is loaded
			gameData.timeElapsedInlevel = 0;

			//timer starts when mainscene is loaded
			MainLoop.instance.StartCoroutine(timer());
		}
    }

	IEnumerator<UnityEngine.WaitForSeconds> timer() {
		yield return new WaitForSeconds(1);
		//save elapsed second to timer only if level is not finished and player's script is not running
		if (newEnd_f.Count == 0 && scriptIsRunning.Count == 0) {
			gameData.timeElapsedInlevel++;
			//Debug.Log(gameData.timeElapsedInlevel);
		}
		
		//try to save elapsed second each second
		MainLoop.instance.StartCoroutine(timer());
	}

	//called on click for each execution button
	public void playExecutionSound() {
		executionCanvas.GetComponent<AudioSource>().Play();
	}

	private IEnumerator delaySetOpacity() {
		yield return null;
		foreach (GameObject bouton in buttons_f) {
			setOpacity(bouton);
        }
    }

	private void setOpacity(GameObject bouton) {
		Color buttonColor;
        if (bouton.transform.childCount > 0 && bouton.transform.GetChild(0).GetComponent<TextMeshProUGUI>()) {

			if (PlayerPrefs.GetString(gameData.opacityKey).Equals("off")) {
				buttonColor = bouton.GetComponent<Image>().color;
				buttonColor.a = 255;
				bouton.GetComponent<Image>().color = buttonColor;
				bouton.GetComponent<Image>().sprite = Resources.Load<Sprite>("UI Images/background.png");

				dialogPanel.GetComponent<Image>().sprite = Resources.Load<Sprite>("UI Images/background.png");
				dialogPanel.GetComponent<Image>().color = Color.black;

				endPanel.GetComponent<Image>().sprite = Resources.Load<Sprite>("UI Images/background.png");
				dialogPanel.GetComponent<Image>().color = Color.black;
			}

			else { //on = default value
				buttonColor = bouton.GetComponent<Image>().color;
				buttonColor.a = 129;
				bouton.GetComponent<Image>().color = buttonColor;

				// Load all sprites in atlas
				Sprite[] atlas = Resources.LoadAll<Sprite>("Sci-Fi UI/_SciFi_GUISkin_/atlas");

				bouton.GetComponent<Image>().sprite = atlas.Single(s => s.name == "progress_bar");

				dialogPanel.GetComponent<Image>().sprite = atlas.Single(s => s.name == "window");
				dialogPanel.GetComponent<Image>().color = Color.white;

				endPanel.GetComponent<Image>().sprite = atlas.Single(s => s.name == "window");
				dialogPanel.GetComponent<Image>().color = Color.white;
			}
        }
    }

	private void onNewCurrentAction(GameObject go){
		if (go.activeInHierarchy)
		{
			Vector3 v = GetGUIElementOffset(go.GetComponent<RectTransform>());
			if (v != Vector3.zero)
			{ // if not visible in UI
				ScrollRect containerScrollRect = go.GetComponentInParent<ScrollRect>();
				containerScrollRect.content.localPosition += GetSnapToPositionToBringChildIntoView(containerScrollRect, go.GetComponent<RectTransform>());
			}
		}
	}

	public Vector3 GetSnapToPositionToBringChildIntoView(ScrollRect scrollRect, RectTransform child){
		Canvas.ForceUpdateCanvases();
		Vector3 viewportLocalPosition = scrollRect.viewport.localPosition;
		Vector3 childLocalPosition   = child.localPosition;
		Vector3 result = new Vector3(
			0,
			0 - (viewportLocalPosition.y + childLocalPosition.y),
			0
		);
		return result;
	}

	public Vector3 GetGUIElementOffset(RectTransform rect){
        Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height);
        Vector3[] objectCorners = new Vector3[4];
        rect.GetWorldCorners(objectCorners);
 
        var xnew = 0f;
        var ynew = 0f;
        var znew = 0f;
 
        for (int i = 0; i < objectCorners.Length; i++){
            if (objectCorners[i].x < screenBounds.xMin)
                xnew = screenBounds.xMin - objectCorners[i].x;

            if (objectCorners[i].x > screenBounds.xMax)
                xnew = screenBounds.xMax - objectCorners[i].x;

            if (objectCorners[i].y < screenBounds.yMin)
                ynew = screenBounds.yMin - objectCorners[i].y;

            if (objectCorners[i].y > screenBounds.yMax)
                ynew = screenBounds.yMax - objectCorners[i].y;
				
        }
 
        return new Vector3(xnew, ynew, znew);
 
    }

	private void setExecutionState(bool finished){
		buttonReset.GetComponent<Button>().interactable = finished;
		buttonPlay.GetComponent<Button>().interactable = !finished;
		
		GameObjectManager.setGameObjectState(buttonPlay, finished);
		GameObjectManager.setGameObjectState(buttonContinue, !finished);
		GameObjectManager.setGameObjectState(buttonStop, !finished);
		GameObjectManager.setGameObjectState(buttonPause, !finished);
		GameObjectManager.setGameObjectState(buttonStep, !finished);
		GameObjectManager.setGameObjectState(buttonSpeed, !finished);

		GameObjectManager.setGameObjectState(libraryPanel.First(), finished);
		//editable canvas
		GameObjectManager.setGameObjectState(editableScriptContainer.First().transform.parent.parent.gameObject, finished);
	}
	
	private void saveHistory(int unused = 0){
		if(gameData.actionsHistory == null){
			gameData.actionsHistory = lastEditedScript;
		}
		else{
			foreach(Transform child in lastEditedScript.transform){
				Transform copy = UnityEngine.GameObject.Instantiate(child);
				copy.SetParent(gameData.actionsHistory.transform);
				GameObjectManager.bind(copy.gameObject);				
			}
			GameObjectManager.refresh(gameData.actionsHistory);
		}	
	}

	private void loadHistory(){
		if(gameData.actionsHistory != null){
			GameObject editableCanvas = editableScriptContainer.First();
			for(int i = 0 ; i < gameData.actionsHistory.transform.childCount ; i++){
				Transform child = UnityEngine.GameObject.Instantiate(gameData.actionsHistory.transform.GetChild(i));
				child.SetParent(editableCanvas.transform);
				GameObjectManager.bind(child.gameObject);
				GameObjectManager.refresh(editableCanvas);
			}
			LevelGenerator.computeNext(gameData.actionsHistory);
			foreach(BaseElement act in editableCanvas.GetComponentsInChildren<BaseElement>()){
				GameObjectManager.addComponent<Dropped>(act.gameObject);
			}
			//destroy history
			GameObject.Destroy(gameData.actionsHistory);
			LayoutRebuilder.ForceRebuildLayoutImmediate(editableCanvas.GetComponent<RectTransform>());
		}
	}

	private void restoreLastEditedScript(){
		List<Transform> childrenList = new List<Transform>();
		foreach(Transform child in lastEditedScript.transform){
			childrenList.Add(child);
		}
		GameObject container = editableScriptContainer.First();
		foreach(Transform child in childrenList){
			child.SetParent(container.transform);
			GameObjectManager.bind(child.gameObject);
		}
		GameObjectManager.refresh(container);
	}

	private void levelFinished (GameObject go){
		setExecutionState(true);
		if(go.GetComponent<NewEnd>().endType == NewEnd.Win){
			loadHistory();
			PlayerPrefs.SetInt(gameData.levelToLoad.Item1, gameData.levelToLoad.Item2+1);
			PlayerPrefs.Save();
		}
		else if(go.GetComponent<NewEnd>().endType == NewEnd.Detected){
			//copy player container into editable container
			restoreLastEditedScript();
		}
	}
	private void linkTo(GameObject go){
		if(go.GetComponent<UIActionType>().linkedTo == null){
			if(go.GetComponent<BasicAction>()){
				go.GetComponent<UIActionType>().linkedTo = GameObject.Find(go.GetComponent<BasicAction>().actionType.ToString());
			}			
			else if(go.GetComponent<IfAction>())
				go.GetComponent<UIActionType>().linkedTo = GameObject.Find("If");
			else if(go.GetComponent<ForAction>())
				go.GetComponent<UIActionType>().linkedTo = GameObject.Find("For");
		}
	}

    private void displayEndPanel(GameObject endPanel)
    {
        GameObjectManager.setGameObjectState(endPanel.transform.parent.gameObject, true);
    }

    private void onDisplayedEndPanel (GameObject endPanel)
    { 
        switch (endPanel.GetComponent<NewEnd>().endType)
        {
            case 1:
				MainLoop.instance.StartCoroutine(ShowLetterByLetter(endPanel.transform.Find("VerticalCanvas").GetComponentInChildren<TextMeshProUGUI>().transform, "Vous avez été repéré !"));
				//endPanel.transform.Find("VerticalCanvas").GetComponentInChildren<TextMeshProUGUI>().text = "Vous avez été repéré !";
                GameObjectManager.setGameObjectState(endPanel.transform.Find("NextLevel").gameObject, false);
				GameObjectManager.setGameObjectState(endPanel.transform.Find("ReloadState").gameObject, true);
				endPanel.GetComponent<AudioSource>().clip = Resources.Load("Sound/LoseSound") as AudioClip;
                endPanel.GetComponent<AudioSource>().loop = true;
                endPanel.GetComponent<AudioSource>().Play();
                break;
            case 2:
				int score = (10000 / (gameData.totalActionBloc + 1) + 5000 / (gameData.totalStep + 1) + 6000 / (gameData.totalExecute + 1) + 5000 * gameData.totalCoin);
				//Debug.Log("score = " + score);
                Transform verticalCanvas = endPanel.transform.Find("VerticalCanvas");

				// With score
				//MainLoop.instance.StartCoroutine(ShowLetterByLetter(endPanel.transform.Find("VerticalCanvas").GetComponentInChildren<TextMeshProUGUI>().transform, "Bravo vous avez gagné !\nScore: " + score));
				//verticalCanvas.GetComponentInChildren<TextMeshProUGUI>().text = "Bravo vous avez gagné !\nScore: " + score;
				
				// Without score
				MainLoop.instance.StartCoroutine(ShowLetterByLetter(endPanel.transform.Find("VerticalCanvas").GetComponentInChildren<TextMeshProUGUI>().transform, "Bravo vous avez gagné !"));

				setScoreStars(score, verticalCanvas.Find("ScoreCanvas"));

				endPanel.GetComponent<AudioSource>().clip = Resources.Load("Sound/VictorySound") as AudioClip;
                endPanel.GetComponent<AudioSource>().loop = false;
                endPanel.GetComponent<AudioSource>().Play();
				GameObjectManager.setGameObjectState(endPanel.transform.Find("NextLevel").gameObject, true);
				GameObjectManager.setGameObjectState(endPanel.transform.Find("ReloadState").gameObject, false);

				//End
				if (gameData.levelToLoad.Item2 >= gameData.levelList[gameData.levelToLoad.Item1].Count - 1)
                {
                    GameObjectManager.setGameObjectState(endPanel.transform.Find("NextLevel").gameObject, false);
					//GameObjectManager.setGameObjectState(endPanel.transform.Find("ReloadState").gameObject, false);
                }
                break;
        }
    }

	private void setScoreStars(int score, Transform scoreCanvas){
		int scoredStars = 0;
		if(gameData.levelToLoadScore != null){
			//check 0, 1, 2 or 3 stars
			if(score >= gameData.levelToLoadScore[0]){
				scoredStars = 3;
			}
			else if(score >= gameData.levelToLoadScore[1]){
				scoredStars = 2;
			}
			else {
				scoredStars = 1;
			}			
		}

		for (int nbStar = 0 ; nbStar < 4 ; nbStar++){
			if(nbStar == scoredStars)
				GameObjectManager.setGameObjectState(scoreCanvas.GetChild(nbStar).gameObject, true);
			else
				GameObjectManager.setGameObjectState(scoreCanvas.GetChild(nbStar).gameObject, false);
		}

		//save score only if better score
		int savedScore = PlayerPrefs.GetInt(gameData.levelToLoad.Item1+Path.DirectorySeparatorChar+gameData.levelToLoad.Item2+gameData.scoreKey, 0);
		if(savedScore < scoredStars){
			PlayerPrefs.SetInt(gameData.levelToLoad.Item1+Path.DirectorySeparatorChar+gameData.levelToLoad.Item2+gameData.scoreKey, scoredStars);
			PlayerPrefs.Save();			
		}
	}

	// Use to process your families.
	protected override void onProcess(int familiesUpdateCount) {
		//Activate DialogPanel if there is a message
		if(gameData.dialogMessage.Count > 0 && !dialogPanel.transform.parent.gameObject.activeSelf){
			showDialogPanel();
		}
		
	}

	//Refresh Containers size
	private void refreshUI(){
		foreach( GameObject go in editableScriptContainer){
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)go.transform );
		}
	}

	// Empty the script window
	// See ResetButton in editor
	public void resetScript(bool refund = false){
		GameObject editableContainer = editableScriptContainer.First();
		for (int i = 0 ; i < editableContainer.transform.childCount ; i++){
			if(editableContainer.transform.GetChild(i).GetComponent<BaseElement>()){
				destroyScript(editableContainer.transform.GetChild(i).gameObject, refund);				
			}
		
		}
		refreshUI();
	}

	//Recursive script destroyer
	private void destroyScript(GameObject go,  bool refund = false){
		if(go.GetComponent<UIActionType>() != null){
			if(!refund)
				gameData.totalActionBloc++;
			else
				GameObjectManager.addComponent<AddOne>(go.GetComponent<UIActionType>().linkedTo);
		}
		
		if(go.GetComponent<UITypeContainer>() != null){
			foreach(Transform child in go.transform){
				if(child.GetComponent<BaseElement>()){
					destroyScript(child.gameObject, refund);
				}
			}
		}
		go.transform.DetachChildren();
		GameObjectManager.unbind(go);
		UnityEngine.Object.Destroy(go);
	}

	public Sprite getImageAsSprite(string path){
		Texture2D tex2D = new Texture2D(2, 2); //create new "empty" texture
		byte[] fileData = File.ReadAllBytes(path); //load image from SPY/path
		if(tex2D.LoadImage(fileData)){ //if data readable
			return Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0, 0), 100.0f);
		}
		return null;	
	}

	public void showDialogPanel(){
		GameObjectManager.setGameObjectState(dialogPanel.transform.parent.gameObject, true);
		nDialog = 0;

		//dialogPanel.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = gameData.dialogMessage[0].Item1;
		MainLoop.instance.StartCoroutine(ShowLetterByLetter(dialogPanel.transform.Find("Text"), gameData.dialogMessage[0].Item1));

		GameObject imageGO = dialogPanel.transform.Find("Image").gameObject;
		if(gameData.dialogMessage[0].Item2 != null){
			GameObjectManager.setGameObjectState(imageGO,true);
			imageGO.GetComponent<Image>().sprite = getImageAsSprite(Application.streamingAssetsPath+Path.DirectorySeparatorChar+"Levels"+
			Path.DirectorySeparatorChar+gameData.levelToLoad.Item1+Path.DirectorySeparatorChar+"Images"+Path.DirectorySeparatorChar+gameData.dialogMessage[0].Item2);
		}
		else
			GameObjectManager.setGameObjectState(imageGO,false);

		if(gameData.dialogMessage.Count > 1){
			setActiveOKButton(false);
			setActiveNextButton(true);
		}
		else{
			setActiveOKButton(true);
			setActiveNextButton(false);
		}
		delay = dialogPanel.transform.Find("Text").GetComponent<Write>().delai;
	}

	// Show dialogs letter by letter
	IEnumerator<UnityEngine.WaitForSeconds> ShowLetterByLetter(Transform go, string text)
	{
		uiText = go.GetComponent<TextMeshProUGUI>();

		originalText = text;

		uiText.text = "";
		for (int i = 0; i < originalText.Length+1; i++)
		{
			uiText.text = originalText.Substring(0, i);
			yield return new WaitForSeconds(go.GetComponent<Write>().delai);			
		}
	}

	// See NextButton in editor
	public void nextDialog(){
		if (!uiText.text.Equals(originalText) && !originalTextDisplayed) {
			originalTextDisplayed = true;
			delay = dialogPanel.transform.Find("Text").GetComponent<Write>().delai;
			dialogPanel.transform.Find("Text").GetComponent<Write>().delai = 0;
			//dialogPanel.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = gameData.dialogMessage[nDialog].Item1;
		}
		else {
			dialogPanel.transform.Find("Text").GetComponent<Write>().delai = delay;
			nDialog++;
			//dialogPanel.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = gameData.dialogMessage[nDialog].Item1;
			MainLoop.instance.StartCoroutine(ShowLetterByLetter(dialogPanel.transform.Find("Text"), gameData.dialogMessage[nDialog].Item1));
			GameObject imageGO = dialogPanel.transform.Find("Image").gameObject;
			if(gameData.dialogMessage[nDialog].Item2 != null){
				GameObjectManager.setGameObjectState(imageGO,true);
				imageGO.GetComponent<Image>().sprite = getImageAsSprite(Application.streamingAssetsPath+Path.DirectorySeparatorChar+"Levels"+
				Path.DirectorySeparatorChar+gameData.levelToLoad.Item1+Path.DirectorySeparatorChar+"Images"+Path.DirectorySeparatorChar+gameData.dialogMessage[nDialog].Item2);		}
			else
				GameObjectManager.setGameObjectState(imageGO,false);

			if(nDialog + 1 < gameData.dialogMessage.Count){
				setActiveOKButton(false);
				setActiveNextButton(true);
			}
			else{
				setActiveOKButton(true);
				setActiveNextButton(false);
			}
			originalTextDisplayed = false;
		}


	}

	public void setActiveOKButton(bool active){
		GameObjectManager.setGameObjectState(dialogPanel.transform.Find("Buttons").Find("OKButton").gameObject, active);
	}

	public void setActiveNextButton(bool active){
		GameObjectManager.setGameObjectState(dialogPanel.transform.Find("Buttons").Find("NextButton").gameObject, active);
	}

	// See OKButton in editor
	public void closeDialogPanel(){
		if (!uiText.text.Equals(originalText) && !originalTextDisplayed) {
			originalTextDisplayed = true;
			dialogPanel.transform.Find("Text").GetComponent<Write>().delai = 0;
		}
        else {
			nDialog = 0;
			gameData.dialogMessage = new List<(string,string)>();;
			GameObjectManager.setGameObjectState(dialogPanel.transform.parent.gameObject, false);
			originalTextDisplayed = false;
		}

	}

	private IEnumerator delayLoadMainScene() {
		yield return null;
		GameObjectManager.loadScene("MainScene");
	}

	public void reloadScene(){
		gameData.totalActionBloc = 0;
		gameData.totalStep = 0;
		gameData.totalExecute = 0;
		gameData.totalCoin = 0;
		gameData.levelToLoadScore = null;
		gameData.dialogMessage = new List<(string,string)>();
		GameObjectManager.addComponent<LRS_levelButton>(MainLoop.instance.gameObject);
		MainLoop.instance.StartCoroutine(delayLoadMainScene());
	}

	// See TitleScreen and ScreenTitle buttons in editor
	public void returnToTitleScreen(){
		gameData.totalActionBloc = 0;
		gameData.totalStep = 0;
		gameData.totalExecute = 0;
		gameData.totalCoin = 0;
		gameData.levelToLoadScore = null;
		gameData.dialogMessage = new List<(string,string)>();
		gameData.actionsHistory = null;
		GameObjectManager.loadScene("TitleScreen");
	}

	// See NextLevel button in editor
	// Call ScenarioSystem
	public void nextLevel() {
		//adaptive level progression
		GameObjectManager.addComponent<LevelScenario>(MainLoop.instance.gameObject);
		reloadScene();
		gameData.actionsHistory = null;
	}

	// See ReloadLevel and RestartLevel buttons in editor
	public void retry(){
		gameData.totalActionBloc = 0;
		gameData.totalStep = 0;
		gameData.totalExecute = 0;
		gameData.totalCoin = 0;
		gameData.levelToLoadScore = null;
		gameData.dialogMessage = new List<(string,string)>();
		if(gameData.actionsHistory != null)
			UnityEngine.Object.DontDestroyOnLoad(gameData.actionsHistory);
		GameObjectManager.addComponent<LRS_levelButton>(MainLoop.instance.gameObject);
		MainLoop.instance.StartCoroutine(delayLoadMainScene());
	}

	public void reloadState(){
		GameObjectManager.removeComponent<NewEnd>(endPanel);
	}

	// See StopButton in editor
	public void stopScript(){
		restoreLastEditedScript();
		setExecutionState(true);
		CurrentAction act;
		foreach(GameObject go in currentActions){
			act = go.GetComponent<CurrentAction>();
			if(act.agent.CompareTag("Player"))
				GameObjectManager.removeComponent<CurrentAction>(go);
		}		
	}

	// See ExecuteButton in editor
	public void applyScriptToPlayer(){
		//if first click on play button
		if(!buttonStop.activeInHierarchy){
			gameData.totalExecute++;
			//hide panels
			GameObjectManager.setGameObjectState(libraryPanel.First(), false);
			//editable canvas
			GameObjectManager.setGameObjectState(editableScriptContainer.First().transform.parent.parent.gameObject, false);
			//clean container for each robot
			foreach(GameObject robot in playerGO){
				foreach(Transform child in robot.GetComponent<ScriptRef>().scriptContainer.transform){
					GameObjectManager.unbind(child.gameObject);
					GameObject.Destroy(child.gameObject);
				}
			}
			
			//copy editable script
			lastEditedScript = GameObject.Instantiate(editableScriptContainer.First());
			foreach(Transform child in lastEditedScript.transform){
				if(child.name.Contains("PositionBar")){
					UnityEngine.GameObject.Destroy(child.gameObject);
				}
			}

			GameObject containerCopy = CopyActionsFrom(editableScriptContainer.First(), false, playerGO.First());
			
			foreach( GameObject go in playerGO){
				GameObject targetContainer = go.GetComponent<ScriptRef>().scriptContainer;
				go.GetComponent<ScriptRef>().uiContainer.transform.Find("Header").Find("Toggle").GetComponent<Toggle>().isOn = true;	
				for(int i = 0 ; i < containerCopy.transform.childCount ; i++){
					if(!containerCopy.transform.GetChild(i).name.Contains("PositionBar")){
						Transform child = UnityEngine.GameObject.Instantiate(containerCopy.transform.GetChild(i));
						child.SetParent(targetContainer.transform);
						GameObjectManager.bind(child.gameObject);
						GameObjectManager.refresh(targetContainer);
					}

				}
				LevelGenerator.computeNext(targetContainer);
			}

			UnityEngine.Object.Destroy(containerCopy);

			//empty editable container
			resetScript();

			foreach (GameObject go in agents){
				LayoutRebuilder.ForceRebuildLayoutImmediate(go.GetComponent<ScriptRef>().uiContainer.GetComponent<RectTransform>());
				if(go.CompareTag("Player")){				
					GameObjectManager.setGameObjectState(go.GetComponent<ScriptRef>().uiContainer,true);				
				}
			}
		}
	}

    public GameObject CopyActionsFrom(GameObject container, bool isInteractable, GameObject agent){
		GameObject copyGO = GameObject.Instantiate(container); 
		foreach(TMP_Dropdown drop in copyGO.GetComponentsInChildren<TMP_Dropdown>()){
			drop.interactable = isInteractable;
		}
		foreach(TMP_InputField input in copyGO.GetComponentsInChildren<TMP_InputField>()){
			input.interactable = isInteractable;
		}
		foreach(ForAction forAct in copyGO.GetComponentsInChildren<ForAction>()){
			
			if(!isInteractable){
				forAct.nbFor = int.Parse(forAct.transform.GetChild(0).transform.GetChild(1).GetComponent<TMP_InputField>().text);
				forAct.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().text = (forAct.currentFor).ToString() + " / " + forAct.nbFor.ToString();
			}
				
			else{
				forAct.currentFor = 0;
				forAct.gameObject.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().text = forAct.nbFor.ToString();
			}

			foreach(BaseElement act in forAct.GetComponentsInChildren<BaseElement>()){
				if(!act.Equals(forAct)){
					forAct.firstChild = act.gameObject;
					break;
				}
			}
		}
		foreach(ForeverAction loopAct in copyGO.GetComponentsInChildren<ForeverAction>()){
			foreach(BaseElement act in loopAct.GetComponentsInChildren<BaseElement>()){
				if(!act.Equals(loopAct)){
					loopAct.firstChild = act.gameObject;
					break;
				}
			}
		}
		foreach(WhileAction whileAct in copyGO.GetComponentsInChildren<WhileAction>()){
			foreach(BaseElement act in whileAct.GetComponentsInChildren<BaseElement>()){
				if(!act.Equals(whileAct)){
					whileAct.firstChild = act.gameObject;
					break;
				}
			}
		}

		foreach(IfAction IfAct in copyGO.GetComponentsInChildren<IfAction>()){
			IfAct.ifEntityType = IfAct.transform.GetChild(0).Find("DropdownEntityType").GetComponent<TMP_Dropdown>().value;
			IfAct.ifDirection = IfAct.transform.GetChild(0).Find("DropdownDirection").GetComponent<TMP_Dropdown>().value;
			IfAct.range = int.Parse(IfAct.transform.GetChild(0).Find("InputFieldRange").GetComponent<TMP_InputField>().text);
			IfAct.ifNot = (IfAct.transform.GetChild(0).Find("DropdownIsOrIsNot").GetComponent<TMP_Dropdown>().value == 1);
			foreach(BaseElement act in IfAct.GetComponentsInChildren<BaseElement>()){
				if(!act.Equals(IfAct)){
					IfAct.firstChild = act.gameObject;
					break;
				}
			}
		}

		foreach(UITypeContainer typeContainer in copyGO.GetComponentsInChildren<UITypeContainer>()){
			typeContainer.enabled = isInteractable;
		}
		foreach(PointerSensitive pointerSensitive in copyGO.GetComponentsInChildren<PointerSensitive>()){
			pointerSensitive.enabled = isInteractable;
		}

		Color actionColor;
		switch(agent.tag){
			case "Player":
				actionColor = MainLoop.instance.GetComponent<AgentColor>().playerAction;
				break;
			case "Drone":
				actionColor = MainLoop.instance.GetComponent<AgentColor>().droneAction;
				break;
			default: // agent by default = robot
				actionColor = MainLoop.instance.GetComponent<AgentColor>().playerAction;
				break;
		}

		foreach(BasicAction act in copyGO.GetComponentsInChildren<BasicAction>()){
			act.gameObject.GetComponent<Image>().color = actionColor;
		}

		return copyGO;
	}
}