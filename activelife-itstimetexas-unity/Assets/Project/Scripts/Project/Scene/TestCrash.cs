using UnityEngine;
using System.Collections;

public class TestCrash : MonoBehaviour {

	private string[] testArray = {"one", "two"};

	// Use this for initialization
	void Start () {
		Debug.Log("Explosion incoming");
		crashMe().ToString();
		throw new System.Exception("Testing crash logging");
	}

	// Update is called once per frame
	void Update () {
		
	}

	void MakeCrash()
	{
		string kaboom = testArray[3];
		Debug.Log(kaboom);
	}

	public Object crashMe()
	{
		return null;
	}
}

