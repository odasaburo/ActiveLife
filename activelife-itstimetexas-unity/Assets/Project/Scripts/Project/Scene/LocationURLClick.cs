using UnityEngine;
using System.Collections;

public class LocationURLClick : MonoBehaviour {

	void OnClick ()
	{
		UILabel lbl = GetComponent<UILabel>();
		string url = lbl.GetUrlAtPosition(UICamera.lastWorldPosition);
		Application.OpenURL(url);
	}
}
