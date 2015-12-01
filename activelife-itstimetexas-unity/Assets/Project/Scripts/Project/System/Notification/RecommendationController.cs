using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ITT.System;
using ITT.Scene;
using UnityEngine;

public enum RecommendationState
{
	None,
	Skip,
	NeedToShow,
	Recommended,
}
[Serializable]
public class RecommendationNotification
{
	public string ActivitiesName;
	public Int64 Nid;
	public RecommendationState State;
	public DateTime Date;
	
	public RecommendationNotification()
	{
		ActivitiesName = string.Empty;
		Nid = 0;
		State = RecommendationState.None;
		Date = DateTime.MinValue;
	}

	public RecommendationNotification(string name, Int64 nid, RecommendationState state, DateTime date)
	{
		ActivitiesName = name;
		Nid = nid;
		State = state;
		Date = date;
	}

	public override string ToString()
	{
		string str = string.Format("Card: {0}, nid: {1}, state: {2}, date: {3}", ActivitiesName, Nid, State, Date);
		return str;
	}
}

public class RecommendationController
{
	#region Constans

	private const string RecommendationPrefs = "RecommendationPrefs";

	#endregion

	#region Variables

	private List<BaseCardData> historyCardList = new List<BaseCardData>();

	private List<RecommendationNotification> RecommendationList;

	public event Action<BaseCardData> OnAddRecommendationToView;

	public bool _currentlyShowing = false;

	private const int RefreshTimeInMinutes = 30;
	public HelperMethods.RecommendationTracker recTracker;
	#endregion

	#region constructor
	public RecommendationController()
	{
		RecommendationList = new List<RecommendationNotification>();

		ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.RECOMMENDATIONTRACKER, OnGotRecTracker);
		ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.RECOMMENDATIONS, OnGotRecommendations);
	}
	
	void OnGotRecTracker(DataEntryBase data)
	{
		RecommendationTrackerEntry rData = data as RecommendationTrackerEntry;
		if (null == rData) return;
		recTracker = rData.Data ?? new HelperMethods.RecommendationTracker();
	}
	
	void OnGotRecommendations(DataEntryBase _data)
	{
		RecommendationEntry recEntry = _data as RecommendationEntry;
		if (null == _data) return;
		if (null == recEntry.Data) return;

		RecommendationList = recEntry.Data.ToList();
	}

	#endregion


	#region Actions

	#region General Action

	public bool IsTimeToRefresh()
	{
		return DateTime.UtcNow.Subtract(recTracker.timeLastShown).TotalMinutes >= RefreshTimeInMinutes;
	}

	public void Update()
	{
		if (!Equals(ITTMainSceneManager.Instance.currentState, (Enum)ITTMainSceneManager.ITTStates.Main))
		{
			// Do nothing unless we're in the main state.
			return;
		}

		if (!ITTDataCache.Instance.HasSessionCredentials)
		{
			Debug.Log("RecommendationController.UpdateRecommendation - The user is not connected");
			return;
		}

		Debug.Log("RecommendationController.UpdateRecommendation - OK");

		ITTDataCache.Instance.RetrieveUserSavedItems(OnSavedListSuccess, OnSavedListFailure);
	}


	#endregion

	private void SetOtherActivitiesToSkip(RecommendationNotification recommendation)
	{
		foreach (RecommendationNotification notification in RecommendationList)
		{
			if (recommendation == notification ||
			    recommendation.State == RecommendationState.Recommended)
				continue;

			recommendation.State = RecommendationState.Skip;
		}
	}

	private void OnSavedListSuccess(string json)
	{
		Debug.Log("RecommendationController.OnSavedListSucces - OK ");

		List<UserSavedActivityType> rawList = null;
		try
		{
			HelperMethods.ResultReponseObject result = JsonFx.Json.JsonReader.Deserialize<HelperMethods.ResultReponseObject>(json);
			if (result.total_count == 0)
			{
				Debug.Log("Empty list");

			}
			else
			{
				historyCardList.Clear();

				UserSavedActivityData userSavedActivityData = JsonFx.Json.JsonReader.Deserialize<UserSavedActivityData>(json);
				UserSavedActivityType[] tempRawList = userSavedActivityData.results;
				if (tempRawList.Length > 0)
				{
					global::System.DateTime now = global::System.DateTime.Now;
					rawList = tempRawList.ToList();
					rawList.RemoveAll(x => null == x.activity); // if prior saved activities were since deleted, skip them.
					rawList.ForEach(x => PopulatePastActivities(x) );
				}

				// Sort lists into proper order by date
				historyCardList.Sort((a, b) => b.dateTime.CompareTo(a.dateTime));

				//_connectionState = NotificationManager.ConnectionState.Connected;

				CheckStoredData(historyCardList);
			}

		}
		catch (Exception e)
		{
			Debug.LogError("RecommendationController.OnSavedListSucces: " + e.Message);
		}
	}

	private void PopulatePastActivities(UserSavedActivityType x)
	{
		if (null == x.activity)
		{
			Debug.Log("<color=magenta>Should not reach this</color>");
			return;
		}

		DateTime now = DateTime.Now;
		x.activity.ParseDateString();
		
		// If we have a valid date, continue populating
		if (x.activity.dateTime != global::System.DateTime.MinValue)
		{
			global::System.DateTime activityLocalTime = x.activity.dateTime.ToLocalTime();
			global::System.TimeSpan span = now.Subtract(activityLocalTime);
			if (activityLocalTime < now)
			{
				// Past list
				historyCardList.Add(x.activity);
			}
		}
	}

	private void OnSavedListFailure(string error)
	{
		Debug.LogError("RecommendationController.OnSavedListFailure - Failed saved list: " + error);
		//_connectionState = NotificationManager.ConnectionState.NoNetworkConnection;
	}

	private void GetCardAndShowIt()
	{
		//Debug.Log("RecommendationController.GetCardAndShowIt - trying to get neccesary card for reccomendation: ");

		RecommendationNotification recommendation = null;

		double minMinutes = double.MaxValue;
		DateTime now = DateTime.UtcNow;
		foreach (RecommendationNotification notification in RecommendationList)
		{
			//Debug.Log("RecommendationController.GetCardAndShowIt - min minutes:" + minMinutes);
			string nState = Enum.GetName(typeof(RecommendationState), notification.State);

			if (notification.Date < now && now.Subtract(notification.Date).TotalMinutes < minMinutes &&
			    notification.State == RecommendationState.NeedToShow)
			{
				minMinutes = now.Subtract(notification.Date).TotalMinutes;
				recommendation = notification;
				break;

				//Debug.Log("RecommendationController.GetCardAndShowIt - recommendation" + recommendation);
			}
		}
		if (recommendation != null)
		{
			//Debug.Log("RecommendationController.GetCardAndShowIt - card title:" + recommendation.ActivitiesName + " date:" +
			//		  recommendation.Date);

			AddRecommendationToView(GetBaseCardData(recommendation));
		}
		else
		{
			Debug.Log("RecommendationController.GetCardAndShowIt - Cann't find the card:");
		}

		
	}

	private void FillDataNecessaryState(List<BaseCardData> savedCard)
	{
		int counter = 0;
		foreach (BaseCardData baseCardData in savedCard)
		{
			Debug.Log("RecommendationController.GetCardAndShowIt - card title:" + baseCardData.title + " date:" +
			          baseCardData.dateTime);

			if (baseCardData.Type.ToLower().Contains("activit"))
			{
				BaseCardData data = baseCardData;
				ITTDataCache.Instance.IsFlaggedRecommended((Int64)baseCardData.id,
				                                           s => OnCheckRecommendFlagSuccessFillData(s, data, counter++),
				                                           s => OnCheckRecommendFlagFailureFillData(s, data));
			}
		}
	}

	private void OnCheckRecommendFlagSuccessFillData(string json, BaseCardData baseCard, int counter)
	{
		//Debug.Log("NotificationManager.OnCheckRecommendFlagSuccess -  Activity: " + baseCard.nid + " name: " + baseCard.title);

		if ("{\"result\":[]}" == json)
			return;

		RecommendationNotification recommendation = GetRecommendation(baseCard);

		if (recommendation == null)
		{
			throw new NullReferenceException(
				"RecommendationController.OnCheckRecommendFlagSuccessFillData - cann't find activities: " + baseCard.title);
		}

		if (json.Contains("false"))
		{
			if (recommendation.State != RecommendationState.Skip &&
			    recommendation.State != RecommendationState.NeedToShow)
				recommendation.State = RecommendationState.NeedToShow;
		}
		else
		{
			recommendation.State = RecommendationState.Recommended;
		}

		int lastIndex = historyCardList.Count - 1;

		if (baseCard.id == historyCardList[lastIndex].id)
		{
			GetCardAndShowIt();
		}
	}

	private void OnCheckRecommendFlagFailureFillData(string error, BaseCardData baseCard)
	{
		Debug.LogError("RecommendationController.OnCheckRecommendFlagFailure error: " + error + " uid:" + baseCard.id +
		               " " +
		               baseCard.title);
	}

	private void OnCheckRecommendFlagSuccess(string json, BaseCardData baseCard)
	{
		if (json.Contains("false"))
		{

			RecommendationNotification recommendation = GetRecommendation(baseCard);
			if (null == recommendation)
			{
				throw new NullReferenceException("RecommendationController.OnCheckRecommendFlagSuccess - can't get card: " +
				                                 baseCard.title);
			}

			recommendation.State = RecommendationState.NeedToShow;

		}
	}

	private void AddRecommendationToView(BaseCardData cardData)
	{
		//AddRecommendationToView(baseCard);

		if (OnAddRecommendationToView != null)
		{
			OnAddRecommendationToView(cardData);
		}

		_currentlyShowing = true;
	}

	public void OnRecommendButton(BaseCardData baseCard)
	{
		Debug.Log("RecommendationController.OnRecommendButton - OK");

		RecommendationNotification recommendation = GetRecommendation(baseCard);
		if (null == recommendation)
		{
			throw new NullReferenceException("RecommendationController.OnRecommendButton - can't get card: " +
			                                 baseCard.title);
		}

		recommendation.State = RecommendationState.Recommended;
		_currentlyShowing = false;
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Notification - Recommendation")
		                                                     .SetEventAction("Click - Recommend Button")
		                                                     .SetEventLabel("User clicked recommend button for card id: " + baseCard.id + " " + baseCard.title));

		ITTDataCache.Instance.RetrieveProfileActivityID((Int64)baseCard.id, OnRetrieveProfileActivitySuccess, OnRetrieveProfileActivityFailure);
	}

	public void OnRetrieveProfileActivitySuccess(string json)
	{
		HelperMethods.ProfileActivityReponseObject profileActivity = JsonFx.Json.JsonReader.Deserialize<HelperMethods.ProfileActivityReponseObject>(json);
		if (null != profileActivity.results && profileActivity.results.Length > 0)
		{
			ITTDataCache.Instance.FlagRecommended(profileActivity.results[0].id, null, null);
		}
	}
	
	public void OnRetrieveProfileActivityFailure(string error)
	{
		Debug.LogError("RecommendationController.OnRetrieveProfileActivityFailure error: " + error);
	}

	public void OnDismissedCard(BaseCardData baseCard)
	{
		_currentlyShowing = false;

        if (baseCard == null)
            return;

		RecommendationNotification recommendation = GetRecommendation(baseCard);
		if (recommendation != null)
		{
			recommendation.State = RecommendationState.Skip;
		}
	}

	private RecommendationNotification GetRecommendation(BaseCardData baseCard)
	{
		return RecommendationList.FirstOrDefault(card => card.Nid == baseCard.id);
	}

	private BaseCardData GetBaseCardData(RecommendationNotification notification)
	{
		return historyCardList.FirstOrDefault(baseCard => baseCard.id == notification.Nid);
	}

	private void OnCheckRecommendFlagFailure(string error, BaseCardData baseCard)
	{
		Debug.LogError("RecommendationController.OnCheckRecommendFlagFailure error: " + error + " uid:" + baseCard.id +
		               " " +
		               baseCard.title);
	}

	private void CheckStoredData(List<BaseCardData> baseCard)
	{
		Debug.Log("RecommendationController.CheckStoredData - OK");

		CheckAndAddData(baseCard);

		FillDataNecessaryState(baseCard);

	}

	private void CheckAndAddData(List<BaseCardData> baseCard)
	{
		Debug.Log("RecommendationController.CheckAndAddData - OK");

		foreach (BaseCardData baseCardData in baseCard)
		{
			if (RecommendationList.Any(card => card.ActivitiesName == baseCardData.title && card.Nid == baseCardData.id))
			{
				continue;
			}

			RecommendationNotification recommendation =
				new RecommendationNotification(baseCardData.title, (Int64)baseCardData.id,
				                               RecommendationState.NeedToShow,
				                               baseCardData.dateTime);
			RecommendationList.Add(recommendation);

		}

		if (null != RecommendationList)
		{
			// Update data cache
			ITTDataCache.Instance.Data.UpdateDataEntry((int)DataCacheIndices.RECOMMENDATIONS, RecommendationList.ToArray(), false);
		}
	}

	#endregion
	
}
