using System;
using UnityEngine;
using System.Collections;

public class DayOfWeekController : MonoBehaviour {

	#region DayOfWeekController constants

	private const int  NumberOfTheDaysView = 7; // one week including today
	public int NumberOfDays
	{
		get { return NumberOfTheDaysView; }
	}

	private const string PrefabPath = "Prefabs/Prototype/";
	private const string PrefabName = "DayOfWeekContainer";

	#endregion

	#region DayOfWeekController member

	private UICenterOnChild _centerOnChildUi;
	private UIScrollView _scrollViewUi;
	private bool _isCentered = false;

	private float _nextChildPositionY = 0f;
	private float _childYOffset = 0f;

	private float _scrollViewStartYPos;

	private DayOfWeekModel _selectedDay;
	
	public DayOfWeekModel SelectedDay
	{
		get { return _selectedDay; }
		set
		{
			if (value == SelectedDay || value == null)
				return;

			_selectedDay = value;

			if (OnSelectedDayChangeEvent != null)
				OnSelectedDayChangeEvent(_selectedDay.IndexOfDay);
		}
	}

	#endregion

	#region DayOfWeek

	public delegate void OnSelectedDayChangeDelegate(int indexOfDay);
	public event OnSelectedDayChangeDelegate OnSelectedDayChangeEvent;

	#endregion

	#region MonoBehavoiur actions

	void Start ()
	{
		_centerOnChildUi = GetComponent<UICenterOnChild>();

		if (null == _centerOnChildUi)
		{
			throw new MissingComponentException("DayofWeekController.Start - can't get UICenterOnChild component");
		}

		_scrollViewUi = GetComponent<UIScrollView>();

		if (null == _scrollViewUi)
		{
			throw new MissingComponentException("DayofWeekController.Start - can't get UIScrollView component");
		}

		_scrollViewStartYPos = _scrollViewUi.transform.localPosition.y;

		Init();

		// HACK: Causes the dynamic fonts on children to layer correctly
		NGUITools.SetActive (gameObject, false);
		NGUITools.SetActive (gameObject, true);
	}

	#endregion

	#region actions

	private void Init()
	{
		GameObject firstChild = null;

		GameObject dayOfWeekContainerToMake = (GameObject)Resources.Load(PrefabPath + PrefabName);

		if (null == dayOfWeekContainerToMake)
		{
			Debug.LogError("DayOfWeekController.Init - can't load prefab" + PrefabPath + PrefabName);
			return;
		}

		for (int i = 0; i < NumberOfTheDaysView; i++) 
		{
			
			GameObject dayOfWeekChild = NGUITools.AddChild(gameObject, dayOfWeekContainerToMake);

			if (null == dayOfWeekChild)
			{
				Debug.LogError("DayOfWeekController.Init - can't add prefab to scroll view parent: " + PrefabPath + PrefabName);
				return;
			}

			UIWidget widget = dayOfWeekChild.GetComponent<UIWidget>();

			if (null==widget)
			{
				throw new MissingComponentException("DayOfWeekController.Init - can't get UIWidget component from " + dayOfWeekChild.name);
			}

			if (i > 0)
				_nextChildPositionY -= widget.height + _childYOffset;

			Vector3 widgetPosition = widget.transform.localPosition;

			widget.transform.localPosition = new Vector3(widgetPosition.x, _nextChildPositionY, widgetPosition.z);

			DayOfWeekModel dayOfWeekModel = dayOfWeekChild.GetComponent<DayOfWeekModel>();

			if (null == dayOfWeekModel)
			{
				throw new MissingComponentException("DayOfWeekController.Init - can't get DayOfWeekModel component from " + dayOfWeekChild.name);
			}

			dayOfWeekModel.Init(i);

			if (0 == i)
			{
				firstChild = dayOfWeekChild;
			}

			// Scale effect
			var scaleEffect = dayOfWeekChild.AddComponent<ScrollItemScale>();
			scaleEffect.ParentScrollView = _scrollViewUi;
		}

		if (null == firstChild)
		{
			throw new MissingComponentException("DayOfWeekController.Init - firstChild is not setting");
		}

		Destroy(_centerOnChildUi);

		_centerOnChildUi = gameObject.AddComponent<UICenterOnChild>();

		if (null == _centerOnChildUi)
		{
			throw new MissingComponentException("DayofWeekController.Start - can't get UICenterOnChild component");
		}
		

		_centerOnChildUi.CenterOn(firstChild.transform);

		_centerOnChildUi.onFinished = UpdateSelectedDay;

		_scrollViewUi.ResetPosition();

		UpdateSelectedDay();
	}

	private void UpdateSelectedDay()
	{
		if (null == _centerOnChildUi)
			return;

		SelectedDay = _centerOnChildUi.centeredObject.GetComponent<DayOfWeekModel>();
	}

	public void ResetToFirstDay()
	{
		if (transform.childCount > 0)
			_centerOnChildUi.CenterOn(transform.GetChild(0));

	}

	public void OnMainAnimationFinish()
	{
		// HACK: the first time we need to reset scroll position. ALITTEX-752
		if (!_isCentered) {
			_isCentered = true;
			StartCoroutine(ResetScrollPosition());
		}
	}

	private IEnumerator ResetScrollPosition()
	{	
		yield return new WaitForSeconds(0.25f);
		_scrollViewUi.ResetPosition();
		UpdateSelectedDay();
	}
	#endregion
}
