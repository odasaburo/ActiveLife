using System;
using UnityEngine;
using System.Collections;

public class SwipeScripts : MonoBehaviour {
	#region Constants

	private const float TouchDragThreshold = 40f;

	#endregion

	#region Variables

	
	private UIScrollView _uiScrollView;

	public UIWidget ScrollViewContainer;

	private UIPanel _panel;

	private Vector3 _minVisibleScale = new Vector3(0.4f, 0.4f, 0.4f);

	private float _currentDistance;

	public float CurrentDistance
	{
		get { return _currentDistance; }
	}

	private float _thresholdDistanceToCloseCard;
	private float _thresholdDistanceVerticalToCloseCard;

	private float _startingMomentum;

	private Vector3 _currentScale;
	private Vector3 _begintScrollContainerPos;
	private Vector3 _begintScrollViewPos;
	private Vector3 _currentScrollContainerPos;

	private Vector2 _currentPanelClipOffset;

	private Vector3 _startPosDragging;

	private bool _isNeedResetToDefault = false;

	private UIScrollView.Movement _previousMovementState;

	private Vector3 _currentPos
	{
		get
		{
			if (Input.touchCount > 0)
			{
				return Input.GetTouch(0).position;
			}
			return Input.mousePosition;
		}
	}

	private Vector3 DeltaTouchPos
	{
		get { return _startTouchPos - _currentPos; }
	}

	private Vector3 _startTouchPos;

	private enum SwipeState
	{
		None,
		UseVerticalScroll,
		UseHorizontalSwipe,
	}

	private SwipeState _swipeState;

	public bool _isFirstTouch;

	#endregion
	
	#region MonoBehaviours actions

	// Use this for initialization
	void Start ()
	{
		_swipeState = SwipeState.None;

		_isFirstTouch = true;

		_uiScrollView = GetComponentInChildren<UIScrollView>();

		if (null == _uiScrollView)
		{
			throw new MissingComponentException("TestSwipeScripts.Start - can't get UIScrollView component for _uiScrollView");
		}

		_startingMomentum = _uiScrollView.momentumAmount;
		_uiScrollView.panel.clipping = UIDrawCall.Clipping.None;

		_panel = _uiScrollView.GetComponent<UIPanel>();

		if (null == _panel)
		{
			throw new MissingComponentException("TestSwipeScripts.Start - can't get UIPanel component for _panel");
		}

		DynamicScrollView2 dynamicScrollView = FindObjectOfType<DynamicScrollView2>();

		if (null == dynamicScrollView)
		{
			throw new NullReferenceException("TestSwipeScripts.Start - can't find DynamicScrollView object");
		}

		_thresholdDistanceToCloseCard = dynamicScrollView.closeDetailCardThresholdHorizontal;
		_thresholdDistanceVerticalToCloseCard = dynamicScrollView.closeDetailCardThreshold;


		ScrollViewContainer = NGUITools.FindInParents<UIWidget>(_uiScrollView.transform);

		if (null == ScrollViewContainer)
		{
			throw new MissingComponentException("TestSwipeScripts.Start - can't get UIWidget component for _scrollViewContainer");
		}

		_currentScale = Vector3.one;

		_previousMovementState = UIScrollView.Movement.Custom;

	}
	
	// Update is called once per frame
	void Update () {
		UpdateScrollPosition();
	}

	#endregion

	#region Actions
	private void UpdateScrollPosition()
	{
		if (null == _uiScrollView)
			return;

		if (_uiScrollView.isDragging)
		{
			if (_isFirstTouch)
			{
				_startTouchPos = _currentPos;
				_isFirstTouch = false;
			}

			float verticalSwipeDistance =  Mathf.Abs(DeltaTouchPos.y);
			float horizontalSwipeDistance =  Mathf.Abs(DeltaTouchPos.x);

			if (_swipeState == SwipeState.None)
			{
				if (verticalSwipeDistance >= TouchDragThreshold)
				{
					_swipeState = SwipeState.UseVerticalScroll;

					_uiScrollView.movement = UIScrollView.Movement.Vertical;
					_begintScrollContainerPos = _uiScrollView.transform.localPosition;
				}
				else if (horizontalSwipeDistance >= TouchDragThreshold)
				{
					_swipeState = SwipeState.UseHorizontalSwipe;
					SetBasicDataBeforeDragging();
				}
			}

			UpdateScrollViewScale();
		}
		else
		{
			if (_uiScrollView.movement != UIScrollView.Movement.Vertical)
			{
				_previousMovementState = _uiScrollView.movement;

				_uiScrollView.customMovement = new Vector2(1, 1);

				_uiScrollView.movement = UIScrollView.Movement.Vertical;

				if (Mathf.Abs(_currentDistance) > _thresholdDistanceToCloseCard )
				{
					_isNeedResetToDefault = false;
				}
				else
				{
					_isNeedResetToDefault = true;
				}

			}

			_isFirstTouch = true;

			_swipeState = SwipeState.None;

			ResetScaleToDefault();
		}
	}

	private void SetBasicDataBeforeDragging()
	{
		_uiScrollView.movement = UIScrollView.Movement.Horizontal;
		_begintScrollViewPos = new Vector3(0, _uiScrollView.transform.localPosition.y,
		                                   _uiScrollView.transform.localPosition.z);

		_begintScrollContainerPos = ScrollViewContainer.transform.localPosition;

		_currentScrollContainerPos = _begintScrollContainerPos;

		_currentPanelClipOffset = new Vector2(0, _panel.clipOffset.y);

		_startPosDragging = _currentPos;
	}

	private void UpdateScrollViewScale()
	{
		if (_uiScrollView.movement != UIScrollView.Movement.Horizontal)
			return;

		_currentDistance = _startPosDragging.x - _currentPos.x;

		_currentScrollContainerPos = _startPosDragging - _currentPos;

		_uiScrollView.transform.localPosition = _begintScrollViewPos;
		_uiScrollView.panel.clipping = UIDrawCall.Clipping.SoftClip;

		ScrollViewContainer.transform.localPosition =
			new Vector3((_begintScrollContainerPos.x - _currentScrollContainerPos.x), _begintScrollContainerPos.y,
			            _begintScrollContainerPos.z);

		Vector2 verticalClipOffset = _currentPanelClipOffset;
		verticalClipOffset.x = 0f;
		_panel.clipOffset = verticalClipOffset;
		
		_currentScale = ScrollViewContainer.transform.localScale;

		ScrollViewContainer.transform.localScale = Vector3.Lerp(Vector3.one, _minVisibleScale,
											Mathf.Abs(_currentDistance)/(_thresholdDistanceToCloseCard * 3.5f));

	}

	private void ResetScaleToDefault()
	{
		if (_previousMovementState == UIScrollView.Movement.Vertical)
			return;

		if (!_isNeedResetToDefault)
			return;

		_uiScrollView.panel.clipping = UIDrawCall.Clipping.None;
		ScrollViewContainer.transform.localScale = Vector3.Lerp(_currentScale, Vector3.one, Time.deltaTime*7);
		_currentScale = ScrollViewContainer.transform.localScale;

		Vector3 currentPos = ScrollViewContainer.transform.localPosition;
		TweenPosition.Begin(ScrollViewContainer.gameObject, 0.15f, _begintScrollContainerPos);
		TweenPosition.Begin(_uiScrollView.gameObject, 0.15f, _begintScrollViewPos);

		if (Mathf.Abs(ScrollViewContainer.transform.localScale.x - Vector3.one.x)< 0.005 || (currentPos-_begintScrollContainerPos).sqrMagnitude < 0.005f  )
		{
			_isNeedResetToDefault = false;
		}
	}

	private void ResetMomentum()
	{
		_uiScrollView.momentumAmount = _startingMomentum;
	}
	#endregion
}
