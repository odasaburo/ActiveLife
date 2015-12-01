using UnityEngine;
using System.Collections;

public class DayOfWeekModel : MonoBehaviour
{

	#region DayOfWeekModel members

	[SerializeField] private UILabel _dayOfWeekLabel;

	private int _indexOfDay;

	public int IndexOfDay
	{
		get { return _indexOfDay; }
	}

	#endregion

	#region MoonoBehaviour actions

	// Use this for initialization
	private void Start()
	{

		_dayOfWeekLabel = GetComponentInChildren<UILabel>();

		if (null == _dayOfWeekLabel)
		{
			throw new MissingComponentException();
		}
	}

	private void Awake()
	{
		_indexOfDay = 0;
	}

	#endregion

	#region actions

	public void Init(int indexOfDay)
	{
		_indexOfDay = indexOfDay;
		global::System.DateTime day = global::System.DateTime.Now.AddDays(indexOfDay);
		_dayOfWeekLabel.text = day.DayOfWeek.ToString().ToUpper();
	}

	#endregion
}
