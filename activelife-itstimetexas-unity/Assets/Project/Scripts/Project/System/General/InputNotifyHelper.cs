using UnityEngine;
using System.Collections;

/// <summary>
/// Helper class containing methods that can be notified
/// to affect the UIInput component.
/// </summary>
public class InputNotifyHelper : MonoBehaviour
{
	UIInput _inputField;

	void Start()
	{
		_inputField = _inputField ?? GetComponent<UIInput> ();
	}

	public void ClearField()
	{
		_inputField.value = "";
	}
}
