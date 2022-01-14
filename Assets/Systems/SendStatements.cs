using UnityEngine;
using FYFY;
using DIG.GBLXAPI;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
public class SendStatements : FSystem {

    private Family f_actionForLRS = FamilyManager.getFamily(new AllOfComponents(typeof(ActionPerformedForLRS)));

    private Family f_levelButtonsLRS = FamilyManager.getFamily(new AllOfComponents(typeof(LRS_levelButton)));
    private Family newEnd_f = FamilyManager.getFamily(new AllOfComponents(typeof(NewEnd)));
    public static SendStatements instance;
    private GameData gameData;
    private string currentLevelName;


    public SendStatements()
    {
        gameData = GameObject.Find("GameData").GetComponent<GameData>();
        currentLevelName = "";

        if (Application.isPlaying)
        {
            initGBLXAPI();
        }
        instance = this;

        f_levelButtonsLRS.addEntryCallback(startLevelSendStatement);
        newEnd_f.addEntryCallback(endLevelSendStatement);
    }

    public void initGBLXAPI()
    {
        if (!GBLXAPI.IsInit)
            GBLXAPI.Init(GBL_Interface.lrsAddresses);

        GBLXAPI.debugMode = false;

        string sessionID = Environment.MachineName + "-" + DateTime.Now.ToString("yyyy.MM.dd.hh.mm.ss");
        //Generate player name unique to each playing session (computer name + date + hour)
        //GBL_Interface.playerName = String.Format("{0:X}", sessionID.GetHashCode());

        //Generate a UUID from the player name
        GBL_Interface.userUUID = GBLUtils.GenerateActorUUID(GBL_Interface.playerName);
    }

	// Use to process your families.
	protected override void onProcess(int familiesUpdateCount) {
        // Do not use callbacks because in case in the same frame actions are removed on a GO and another component is added in another system, family will not trigger again callback because component will not be processed
        foreach (GameObject go in f_actionForLRS)
        {
            ActionPerformedForLRS[] listAP = go.GetComponents<ActionPerformedForLRS>();
            int nb = listAP.Length;
            ActionPerformedForLRS ap;
            if (!this.Pause)
            {
                for (int i = 0; i < nb; i++)
                {
                    ap = listAP[i];
                    //If no result info filled
                    if (!ap.result)
                    {
                        GBL_Interface.SendStatement(ap.verb, ap.objectType, ap.objectName, ap.activityExtensions);
                    }
                    else
                    {
                        bool? completed = null, success = null;

                        if (ap.completed > 0)
                            completed = true;
                        else if (ap.completed < 0)
                            completed = false;

                        if (ap.success > 0)
                            success = true;
                        else if (ap.success < 0)
                            success = false;

                        GBL_Interface.SendStatementWithResult(ap.verb, ap.objectType, ap.objectName, ap.activityExtensions, ap.resultExtensions, completed, success, ap.response, ap.score, ap.duration);
                    }
                }
            }
            for (int i = nb - 1; i > -1; i--)
            {
                GameObjectManager.removeComponent(listAP[i]);
            }
        }
	}

    public void testSendStatement()
    {
        Debug.Log(GBL_Interface.playerName + " asks to send statement...");
        GameObjectManager.addComponent<ActionPerformedForLRS>(MainLoop.instance.gameObject, new
        {
            verb = "interacted",
            objectType = "menu",
            objectName = "myButton"
        });
    }

    public void startLevelSendStatement(GameObject unused){ //TO DO : recommencer niveau et passer au niveau suivant
        //Debug.Log(go.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text);
        //currentLevelName = go.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;

        GBL_Interface.playerName = gameData.userName;


        //get current level name
        currentLevelName = Path.GetFileNameWithoutExtension(gameData.levelList[gameData.levelToLoad.Item1][gameData.levelToLoad.Item2]);
        Debug.Log("currentLevelName = " + currentLevelName);

        Debug.Log(GBL_Interface.playerName + " asks to send statement...");

        //send statement
        GameObjectManager.addComponent<ActionPerformedForLRS>(MainLoop.instance.gameObject, new
        {
            verb = "started",
            objectType = "level",
            objectName = currentLevelName

        });
        
    }

    public void endLevelSendStatement(GameObject go) {
        currentLevelName = Path.GetFileNameWithoutExtension(gameData.levelList[gameData.levelToLoad.Item1][gameData.levelToLoad.Item2]);
        Debug.Log("player won level : " + currentLevelName);
        //if player won, send statement

        if(go.GetComponent<NewEnd>().endType == NewEnd.Win) {
            //worked actions and concepts 
            GameObjectManager.addComponent<ActionPerformedForLRS>(MainLoop.instance.gameObject, new {
                verb = "completed",
                objectType = "level",
                objectName = currentLevelName,
                activityExtensions = gameData.tagsDictionary
                /***
                new Dictionary<string, List<string>> {
                    { "action", new List<string>{"myContent1", "myContent2"} }
                }***/
            });

        }
    }
}