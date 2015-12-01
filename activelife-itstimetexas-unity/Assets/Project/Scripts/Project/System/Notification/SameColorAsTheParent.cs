using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class SameColorAsTheParent : MonoBehaviour {

	#region Variables

	[SerializeField] private UIWidget _parent;

	[SerializeField] private List<UIWidget> _childs;

	#endregion

	// Use this for initialization
	void Start ()
	{

		if (null == _parent)
		{
			throw new MissingComponentException("SameColorAsTheParent.Start - can't find component UIWidget for _parent");
		}

		if (null == _childs)
		{
			throw new MissingComponentException("SameColorAsTheParent.Start - can't find component UIWidget for _child");
		}

	}
	
	// Update is called once per frame
	void Update () {

		foreach (UIWidget child in _childs)
		{
			child.color = _parent.color;
		}
	
	}
}
