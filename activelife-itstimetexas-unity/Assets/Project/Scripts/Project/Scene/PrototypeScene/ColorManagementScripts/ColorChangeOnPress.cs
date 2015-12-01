using UnityEngine;
using System.Collections;

public class ColorChangeOnPress : MonoBehaviour {
	private UILabel _buttonLabel;
	
	void Start(){
		_buttonLabel = GetComponentInChildren<UILabel>();
	}
	
	void OnPress(bool isPressed)
	{
		if (isPressed) {
			_buttonLabel.color = ColorManagementSystemController.GetCurrentTextColor();
			
		}
		else {
			_buttonLabel.color = Color.white;
		}
		
	}
}
