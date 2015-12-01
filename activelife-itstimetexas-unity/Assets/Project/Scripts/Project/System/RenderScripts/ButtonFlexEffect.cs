using UnityEngine;
using System.Collections;

public class ButtonFlexEffect : MonoBehaviour {

	private UIWidget _buttonWidget;
	private const float _flexAmount = 10f;
	private const float _animationTime = 0.25f;

	void Start(){
		_buttonWidget = GetComponent<UIWidget> ();
	}

	void OnPress(bool isPressed)
	{
		if (isPressed) {
			float flexWidth = 1f + (_flexAmount / _buttonWidget.width);
			float flexHeight = 1f + (_flexAmount / _buttonWidget.height);

			TweenScale.Begin (gameObject, _animationTime, new Vector3(flexWidth, flexHeight, 1f) );

		}
		else {
			TweenScale.Begin (gameObject, _animationTime, Vector3.one );
		}

	}
}