using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ITT.System;
using JsonFx.Json;
using UnityEngine;
using System.Web;
using Random = System.Random;

[Serializable]
public class MasterTips
{
	public Tips[] results;
}

[Serializable]
public class MasterShortTips
{
	public ShortTips[] results;
}

[Serializable]
public class Tips
{
	public Int64 id;
	public string title;
	private string _description;
	public string Description
	{
		get
		{
			return _description;
		}
		
		set
		{
			_description = value;
		}
	}
	public int tip;
	public string website;
}

[Serializable]
public class ShortTips
{
	public Int64 id;
	public string title;
	public string website;

	public TipsState State;

	public ShortTips()
	{
		State = TipsState.NeedToShow;
	}

	public override string ToString()
	{
		string str = string.Format("Tips nid: {0}, title: {1}, uri: {2}, state: {3}", id, title, website, State);
		return str;
	}
}

public enum TipsState
{
	NeedToShow,
	Showed,
}

[Serializable]
public class LargeTips
{

	public string title;
	public Int64 id;

	private string _description;
	public string Description
	{
		get
		{
			return _description;
		}
		
		set
		{
			_description = value;
		}
	}
	public string path;

	
	public LargeTips()
	{
		
	}

	public override string ToString ()
	{
		string str = string.Format("Tips nid: {0}, title: {1}, body: {3}, uri: {2}", id, title, path,
		                           Description);
		return str;
	}
}

public class TipsController 
{

	#region Constants

	private const string TipsSave = "HealthyTipsSave";

	private const int TotalHourPassToNextTipView = 48;

	private const int OffsetStep = 10;
	private const int OffsetMax = 1000;

	#endregion

	#region Variables

	public List<ShortTips> Tipses;
	private bool _firstDataCall = false;
	private bool _waitingForServer = false;
	public DateTime LastTipsShowTimeStamp;

	private DataBuffer _buffer;

	public TipsController()
	{
		Tipses = new List<ShortTips>();
		ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.TIPS, OnGotTips);		
		ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.TIPTRACKER, OnGotTipTracker);
	}

	void OnGotTips(DataEntryBase _data)
	{
		TipsEntry data2 = _data as TipsEntry;
		if (null == data2) return;
		ShortTips[] data = data2.Data;
		if (null != data)
		{
			Tipses = data.ToList();
			_firstDataCall = true;
		}
	}

	void OnGotTipTracker(DataEntryBase _data)
	{
		TipTrackerEntry trackerEntry = _data as TipTrackerEntry;
		if (null == trackerEntry) return;
		HelperMethods.TipTracker data = trackerEntry.Data;
		if (null != data)
		{
			LastTipsShowTimeStamp = data.tipTimeLastShown;
		}
	}

	void SetTrackingVars()
	{
		HelperMethods.TipTracker t = new HelperMethods.TipTracker();
		t.tipTimeLastShown = LastTipsShowTimeStamp;

		ITTDataCache.Instance.Data.UpdateDataEntry((int)DataCacheIndices.TIPTRACKER, t, false);
	}


	public event Action<LargeTips> OnAddHealthyTips;

	#endregion

	#region Actions

	#region General actions

	public void Update()
	{
		if (_firstDataCall)
		{
			if (ItsTimeToShowNextTips() && !_waitingForServer)
			{
				GetHealthyTipToView();
			}
		}

	}

	public void GetHealthyTipToView()
	{
		bool isNeedToGetMoreTips = true;
		foreach (ShortTips shortTipse in Tipses)
		{
			if (shortTipse.State == TipsState.NeedToShow)
			{
				GetDetailHealthyTipDataByNid(shortTipse.id);
				_waitingForServer = true;
				isNeedToGetMoreTips = false;

				break;
			}
		}

		if (isNeedToGetMoreTips)
		{
			ITTDataCache.Instance.Data.RefreshDataEntry((int)DataCacheIndices.TIPS);
		}
	}

	#endregion

	private bool ItsTimeToShowNextTips()
	{
		return DateTime.UtcNow.Subtract(LastTipsShowTimeStamp).TotalHours >= TotalHourPassToNextTipView;
	}

	public void GetHealthyTipsList(int offset)
	{
		ITTDataCache.Instance.GetHealthyTipsList(offset, OnGetHealthyTipsListSuccess, OnGetHealthyTipsListFailure);
	}

	private void OnGetHealthyTipsListSuccess(string json)
	{
		HelperMethods.ResultReponseObject result = JsonFx.Json.JsonReader.Deserialize<HelperMethods.ResultReponseObject>(json);
		// The retrieved list could be empty
		if (result.total_count == 0)
		{
			Debug.Log("Empty list: resetting offset and fetching more tips");
			// The user should start seeing the old tips again.
			Tipses.Clear();
			GetHealthyTipsList(0);
		}
		else
		{

			MasterShortTips shortResults = JsonReader.Deserialize<MasterShortTips>(json);
			GetHealthyTipToView();
			
		}
		if ("[]" == json)
		{

		}
		else
		{

		}
	}

	private void OnGetHealthyTipsListFailure(string error)
	{
		Debug.LogError("TipsController.OnGetHealthyTipsListFailure - error: " + error);
	}

	private void GetDetailHealthyTipData(int tip)
	{
		Debug.Log("TipsController.GetDetailHealthyTipData - trying to get helthy tip data");
		ITTDataCache.Instance.GetDetailHealthyTipData(tip, OnGetDetailHealthyTipDataSuccess, OnGetDetailHealthyTipDataFailure);
	}

	private void OnGetDetailHealthyTipDataSuccess(string json)
	{
		Debug.Log("TipsController.OnGetDetailHealthyTipDataSuccess - OK, json: \n" + json);

		MasterTips tipsResult = JsonReader.Deserialize<MasterTips>(json);
		Tips[] tips = tipsResult.results;
	}

	private void OnGetDetailHealthyTipDataFailure(string error)
	{
		Debug.LogError("TipsController.OnGetDetailHealthyTipDataFailure - error: " + error);
	}

	private void GetDetailHealthyTipDataByNid(Int64 nid)
	{
		Debug.Log("TipsController.GetDetailHealthyTipDataByNid - trying to get helthy tip data, nid: " + nid);

		ITTDataCache.Instance.GetDetailHealthyTipDataByNid(nid, OnGetDetailHealthyTipDataByNidSuccess,
		                                                   OnGetDetailHealthyTipDataByNidFailure);
	}

	private void OnGetDetailHealthyTipDataByNidSuccess(string json)
	{
		Debug.Log("TipsController.OnGetDetailHealthyTipDataByNidSuccess - OK, json: \n" + json);

		LargeTips tips = JsonReader.Deserialize<LargeTips>(json);

		Debug.Log(tips);

		AddHealthyTipsToView(tips);
	}

	private void AddHealthyTipsToView(LargeTips tips)
	{
		LastTipsShowTimeStamp = DateTime.UtcNow;
		SetTrackingVars();

		Showed(tips);
		_waitingForServer = false;
		if (OnAddHealthyTips != null)
		{
			OnAddHealthyTips(tips);
		}
	}

	private void Showed(LargeTips tips)
	{
		ShortTips shortTips = Tipses.FirstOrDefault(tipss => tipss.id == tips.id);
		if (null == shortTips)
		{
			throw new NullReferenceException("TipsController.Showed - error: cann't find necessary shortTips");
		}
		shortTips.State = TipsState.Showed;
	}

	private void OnGetDetailHealthyTipDataByNidFailure(string error)
	{
		Debug.LogError("TipsController.OnGetDetailHealthyTipDataByNidFailure - error: " + error);
	}


	#endregion

}
