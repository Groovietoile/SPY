using UnityEngine;
using FYFY;

[ExecuteInEditMode]
public class SendStatements_wrapper : MonoBehaviour
{
	private void Start()
	{
		this.hideFlags = HideFlags.HideInInspector; // Hide this component in Inspector
	}

	public void initGBLXAPI()
	{
		MainLoop.callAppropriateSystemMethod ("SendStatements", "initGBLXAPI", null);
	}

	public void testSendStatement()
	{
		MainLoop.callAppropriateSystemMethod ("SendStatements", "testSendStatement", null);
	}

	public void startLevelSendStatement(UnityEngine.GameObject go)
	{
		MainLoop.callAppropriateSystemMethod ("SendStatements", "startLevelSendStatement", go);
	}

	public void endLevelSendStatement(UnityEngine.GameObject go)
	{
		MainLoop.callAppropriateSystemMethod ("SendStatements", "endLevelSendStatement", go);
	}

}
