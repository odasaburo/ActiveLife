using UnityEngine;
using System.Collections;

/// <summary>
/// Used to set the bottom anchor of the detail background to the
/// bottom of the largest widget.
/// </summary>
public class DetailBottomAnchorSelector : MonoBehaviour
{
	UIWidget m_detailBackground;

	public UIWidget[] m_widgets;

	void Awake()
	{
		m_detailBackground = GetComponent<UIWidget> ();
	}

	void Update()
	{
		float size = 0;
		UIWidget bottom = null;
		foreach (var widget in m_widgets)
		{
			if (widget.localSize.y > size)
			{
				size = widget.localSize.y;
				bottom = widget;
			}
		}

		if (m_detailBackground.bottomAnchor.target != bottom.transform) {
			m_detailBackground.bottomAnchor.target = bottom.transform;
			m_detailBackground.ResetAnchors ();
			m_detailBackground.UpdateAnchors ();
		}
	}
}
