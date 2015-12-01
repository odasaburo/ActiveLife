using UnityEngine;
using System.Collections;

public class TransparentBlurBackground : MonoBehaviour {
	
	private static Texture2D _dimTexture;
	private UITexture _renderTexture;

	void Awake() {
		if (null == _dimTexture) {
			_dimTexture = new Texture2D (1, 1);
			_dimTexture.SetPixel (0, 0, new Color (0, 0, 0, 0.8f));
			_dimTexture.Apply ();
		}
	}

	void Start () {
		_renderTexture = gameObject.GetComponent<UITexture>();
		_renderTexture.mainTexture = _dimTexture;
		_renderTexture.type = UIBasicSprite.Type.Sliced;
	}
}
