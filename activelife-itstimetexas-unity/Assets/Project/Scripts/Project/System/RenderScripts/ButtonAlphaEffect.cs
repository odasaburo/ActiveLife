using UnityEngine;
using System.Collections;

public class ButtonAlphaEffect : MonoBehaviour
{
	private const float _animationTime = 0.25f;

	private UIToggle _toggle;
	public UIWidget _checkWidget;

	private bool _isPressed = false;

	void Start()
	{
		_toggle = GetComponent<UIToggle> ();
		_checkWidget.alpha = _toggle.startsActive ? 1 : 0;
		_isPressed = _toggle.startsActive;
	}

	void Update()
	{
		if (!_isPressed) {
			_checkWidget.alpha = _toggle.value ? 1 : 0;
		}
	}

	void OnPress(bool isPressed)
	{
		_isPressed = isPressed;

		// We have to ! the value because it doesn't change until
		// the user stops pressing the button
		float alpha = !_toggle.value ? 1 : 0;
		TweenAlpha.Begin (_checkWidget.gameObject, _animationTime, alpha);
	}
}
