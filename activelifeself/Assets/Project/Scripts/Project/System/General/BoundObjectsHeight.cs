using UnityEngine;
using System.Collections;

public class BoundObjectsHeight : MonoBehaviour 
{
	private UIPanel _uiPanel = null;
	private UIScrollView _uiScrollView = null;
	private Vector3 _originalUiScrollViewMomentum = Vector3.zero;

	#region Unity Methods
	void Start()
	{
		_uiPanel = GetComponent<UIPanel>();
		_uiScrollView = GetComponent<UIScrollView>();
		if(_uiScrollView != null)
			_originalUiScrollViewMomentum = _uiScrollView.currentMomentum;
	}
	
	void Update()
	{
		Bounds bounds = NGUIMath.CalculateRelativeWidgetBounds(gameObject.transform);
		if(_uiPanel != null)
		{
			if(-_uiPanel.clipOffset.y >  Mathf.Abs(bounds.center.y * 2))
				_uiPanel.clipOffset = new Vector2( _uiPanel.clipOffset.x,
				                                  -Mathf.Abs(bounds.center.y * 2));
			else if(_uiPanel.clipOffset.y > 0f)
				_uiPanel.clipOffset = new Vector2( _uiPanel.clipOffset.x,
				                                  0f);
		}
		
		if(_uiScrollView != null)
		{
			if(gameObject.transform.localPosition.y > Mathf.Abs(bounds.center.y * 2))
			{
				gameObject.transform.localPosition = new Vector3( gameObject.transform.localPosition.x,
				                                                  Mathf.Abs(bounds.center.y * 2),
				                                                  gameObject.transform.localPosition.z);
				_uiScrollView.currentMomentum = Vector3.zero;
			}
			else if(gameObject.transform.localPosition.y < 0f)
			{
				gameObject.transform.localPosition = new Vector3( gameObject.transform.localPosition.x,
				                                            	  0f,
				                                           	      gameObject.transform.localPosition.z);
				_uiScrollView.currentMomentum = Vector3.zero;
			}
		}
	}
	#endregion
}
