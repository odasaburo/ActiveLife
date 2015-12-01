using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class ColorUpdateHelper : MonoBehaviour
{

	#region Variables

	[SerializeField] private List<UIWidget> _uiWidgets;

	#endregion


	#region MonoBehaviour actions
	
	// Use this for initialization
	private void Start()
	{
		Init();
		SubscribeEvents();
	}

	// Update is called once per frame
	private void Update()
	{

	}

	private void OnDestroy()
	{
		UnsibscribeEvents();
	}
	#endregion

	#region Actions

	private void SubscribeEvents()
	{
		ColorManagementSystemController.Instance.OnCurrentTimeStateChangeEvent += ChangeColor;
	}

	private void UnsibscribeEvents()
	{
		ColorManagementSystemController.Instance.OnCurrentTimeStateChangeEvent -= ChangeColor;
	}

	private void Init()
	{
		if (null == _uiWidgets)
		{
			throw new NullReferenceException("Used component without initializing");
		}

		foreach (UIWidget uiWidget in _uiWidgets)
		{
			uiWidget.color = ColorManagementSystemController.GetCurrentTextColor();
		}
	}

	public void ChangeColor()
	{
//		Debug.Log("ColorUpdateHelper.ChangeColor - OK");

		if (null == _uiWidgets)
		{
			throw new NullReferenceException("Used component without initializing");
		}

		foreach (UIWidget uiWidget in _uiWidgets)
		{
			ColorManagementSystemController.UpdateColor(uiWidget);
		}
	}

	#endregion
}
