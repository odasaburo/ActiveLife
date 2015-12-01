using UnityEngine;
using System.Collections;

public class LoadingScreenController : MonoBehaviour {
	
	public UITexture _loadingIcon;
	public UILabel _loadingLabel;

	void Update () {
		_loadingIcon.transform.Rotate(Vector3.forward * 360 * Time.deltaTime);
	}
}
