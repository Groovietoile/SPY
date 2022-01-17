using UnityEngine;
using FYFY;

[ExecuteInEditMode]
public class TitleScreenSystem_wrapper : MonoBehaviour
{
	private void Start()
	{
		this.hideFlags = HideFlags.HideInInspector; // Hide this component in Inspector
	}

	public void saveUserName()
	{
		MainLoop.callAppropriateSystemMethod ("TitleScreenSystem", "saveUserName", null);
	}

	public void userNameChanged()
	{
		MainLoop.callAppropriateSystemMethod ("TitleScreenSystem", "userNameChanged", null);
	}

	public void setOpacity(UnityEngine.GameObject bouton)
	{
		MainLoop.callAppropriateSystemMethod ("TitleScreenSystem", "setOpacity", bouton);
	}

	public void changeOpacity()
	{
		MainLoop.callAppropriateSystemMethod ("TitleScreenSystem", "changeOpacity", null);
	}

	public void showCampagneMenu()
	{
		MainLoop.callAppropriateSystemMethod ("TitleScreenSystem", "showCampagneMenu", null);
	}

	public void backFromCampagneMenu()
	{
		MainLoop.callAppropriateSystemMethod ("TitleScreenSystem", "backFromCampagneMenu", null);
	}

	public void quitGame()
	{
		MainLoop.callAppropriateSystemMethod ("TitleScreenSystem", "quitGame", null);
	}

}
