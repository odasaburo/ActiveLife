using UnityEngine;
using System.Collections;

public class ChangeColorInheritController : MonoBehaviour {
	#region MyRegion

	private ChangeColorInheritModel _model;
	
	#endregion

	#region Monobehaviour Actions

	// Use this for initialization
	void Start ()
	{
		_model = GetComponent<ChangeColorInheritModel>();

		if (null == _model)
		{
			throw new MissingComponentException("ChangeColorInheritController.Start - cann't find ChangeColorInheritModel component");
		}

	}
	
	// Update is called once per frame
	void Update () {
	
		UpdateChildColor();
	}
	#endregion

	#region Actions
	private void UpdateChildColor()
	{
		if (_model.Childs == null || _model.Childs.Count == 0 || _model.Parent == null)
			return;

		foreach (UIWidget uiWidget in _model.Childs)
		{
			uiWidget.color = _model.Parent.color;
		}
	}
	#endregion
}
