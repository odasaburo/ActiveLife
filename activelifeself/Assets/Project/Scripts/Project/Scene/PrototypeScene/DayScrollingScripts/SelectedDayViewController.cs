using ITT.Scene;
using UnityEngine;
using System.Collections;

public class SelectedDayViewController : MonoBehaviour {

	#region SelectedDayViewController model

	private SelectedDayViewModel _model;

	#endregion

	#region Other necessary member

	private DayOfWeekController _dayOfWeekController;

	#endregion

	#region MonoBehaviour actions
	// Use this for initialization
	void Start ()
	{
		_model = GetComponent<SelectedDayViewModel>();

		if (null == _model)
		{
			throw new MissingComponentException("SelectedDayViewController.Start - can't get SelectedDayViewModel component for model");
		}

		_dayOfWeekController = FindObjectOfType<DayOfWeekController>();

		if (null == _dayOfWeekController)
		{
			throw new MissingComponentException("SelectedDayViewController.Start - can't get DayOfWeekController component for _dayOfWeekController");
		}

		SubscribeEvents();

		UpdateDayOfWeekCap(0); // today
	}
	
	// Update is called once per frame
	void Update () {

	}

	private void SubscribeEvents()
	{
		_dayOfWeekController.OnSelectedDayChangeEvent += UpdateDayOfWeekCap;
	}

	private void UnsbscribeEvents()
	{
		_dayOfWeekController.OnSelectedDayChangeEvent -= UpdateDayOfWeekCap;
	}

	private void OnDestroy()
	{
		UnsbscribeEvents();
	}

	#endregion

	#region Actions

	public void UpdateDayOfWeekCap(int indexOfDay)
	{
		global::System.DateTime day = global::System.DateTime.Now.AddDays(indexOfDay);

		global::System.DateTime now = global::System.DateTime.Now;
		global::System.DateTime tomorrow = global::System.DateTime.Now.AddDays(1);

		if (day.Day == now.Day)
		{
			_model.SelectedDayOfWeekLabel.text = "TODAY";
		}
		else if (day.Day == tomorrow.Day)
		{
			_model.SelectedDayOfWeekLabel.text = "TOMORROW";
		}
		else
		{
			_model.SelectedDayOfWeekLabel.text = day.DayOfWeek.ToString().ToUpper();
		}

		_model.SelectedDayFullSmall.text = day.ToString("MMMM").ToUpper() + " " + day.Day;

	}

	#endregion
}
