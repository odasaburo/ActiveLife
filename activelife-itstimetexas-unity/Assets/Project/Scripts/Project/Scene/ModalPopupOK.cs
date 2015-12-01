using UnityEngine;
using System.Collections;
using System;
using ITT.System;

public class ModalPopupOK : MonoBehaviour {

	public UILabel label;
	public UIButton okButton;
	private Action _OKCallback;

	public static void Spawn(string labelText, Action OKCallback = null)
	{
		// Only allow one
		if (null != GameObject.FindObjectOfType<ModalPopupOK>())
			return;

		UIRoot[] roots = NGUITools.FindActive<UIRoot>();
		if (null != roots[0]) {
			GameObject instance = NGUITools.AddChild(roots[0].gameObject, (GameObject)Resources.Load("Prefabs/Production/ModalPopupOK"));
			ModalPopupOK script = instance.GetComponent<ModalPopupOK>();
			script.Init(labelText, OKCallback);
		}
	}

	public void Awake()
	{
		okButton.onClick.Add(new EventDelegate(this, "OnPressedOK"));
	}

	public void Init(string labelText, Action OKCallback)
	{
		label.text = labelText;
		_OKCallback = OKCallback;
	}

	public void OnPressedOK(GameObject go)
	{
		if (null != _OKCallback)
		{
			_OKCallback();
		}

		Destroy(gameObject);
	}
}
