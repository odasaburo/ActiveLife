using System;
using System.Collections.Generic;
using System.Linq;
using ITT.Scene;
using ITT.System;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

public class NotificationManager : MonoBehaviour
{

	#region Constants

	private const float AnimationTime = 0.3f;

	private const string PrefabPath = "Prefabs/Production/";
	private const string RecommendationPrefabName = "NotificationForRecommendation";
	private const string TipsPrefabName = "NotificationTips";

	private Vector3 _startNotificationPos = new Vector3(0f, 128f, 0f);
	private Vector3 _startTipsPos = new Vector3(0f, 168f, 0f);

	#endregion

	#region Variables

	private NotificationManagerModel _model;

	private bool _isUseSmoothShift;

	private float _positionShiftY;

	private float PositionShiftY
	{
		get { return _positionShiftY; }
		set
		{
			_positionShiftY = value;

			if (_isUseSmoothShift)
				SmoothShiftContainer();
			else
				ShiftContainer();
		}
	}

	#endregion

	#region MonoBehaviour Actions

	private void Start()
	{
		_model = GetComponent<NotificationManagerModel>();
		if (null == _model)
		{
			throw new MissingComponentException();
		}
		_model.TipsLogicController = new TipsController();

		_model.RecommendationLogicController = new RecommendationController();

		SubscribeEvents();

		UpdateNotification();
	}

	private void SubscribeEvents()
	{
		_model.TipsLogicController.OnAddHealthyTips += AddHealthyTipsToView;
		_model.RecommendationLogicController.OnAddRecommendationToView += AddRecommendationToView;
	}

	private void UnsubscribeEvents()
	{
		_model.TipsLogicController.OnAddHealthyTips -= AddHealthyTipsToView;
		_model.RecommendationLogicController.OnAddRecommendationToView -= AddRecommendationToView;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.T))
		{
			AddTestTips();
		}

		if (Input.GetKeyDown(KeyCode.N))
		{
			UpdateNotification();
		}

		if (Input.GetKeyDown(KeyCode.R))
		{
			AddTestRecommendation();
		}

		if (Input.GetKeyDown(KeyCode.G))
		{
			_model.TipsLogicController.GetHealthyTipToView();
		}

		UpdateShiftPos();
	}

	private void LateUpdate()
	{
		UpdateHealthyTips();
		UpdateNotification();

		if (null == _model.RecommendationViewController && null == _model.TipsViewController)
		{
			if (IsPanelActive())
				DeactivatePanel();
		}
	}

	private void OnDestroy()
	{
		UnsubscribeEvents();
	}

	#endregion

	#region Actions

	#region correct notification and scroll view position

	private void UpdateShiftPos()
	{
		if (_model.RecommendationViewController != null)
		{
			if (_isUseSmoothShift)
			{
				PositionShiftY = _model.RecommendationViewController.ShiftY;
			}
			_isUseSmoothShift = false;
			PositionShiftY = _model.RecommendationViewController.ShiftY;
		}
		else if (_model.TipsViewController != null)
		{
			if (_isUseSmoothShift)
			{
				PositionShiftY = _model.TipsViewController.ShiftY;
			}
			_isUseSmoothShift = false;
			PositionShiftY = _model.TipsViewController.ShiftY;
		}
		else
		{
			if (!_isUseSmoothShift)
			{
				_isUseSmoothShift = true;
				PositionShiftY = 0;
			}

			_isUseSmoothShift = true;
		}
	}

	private void SmoothShiftContainer()
	{
		TweenPosition tweenPos = gameObject.AddComponent<TweenPosition>();

		tweenPos.duration = AnimationTime;

		tweenPos.animationCurve = ColorManagementSystemController.EasyInOutAnimCurve;

		tweenPos.from = _model.Container.transform.localPosition;

		float toY = tweenPos.to.y + PositionShiftY;

		tweenPos.to = new Vector3(tweenPos.from.x, toY, tweenPos.from.z);

		tweenPos.SetOnFinished(() => Destroy(tweenPos));
	}

	private void ShiftContainer()
	{
		Vector3 currentPos = _model.Container.transform.localPosition;
		_model.Container.transform.localPosition = new Vector3(currentPos.x, PositionShiftY, currentPos.z);
	}

	#endregion

	#region Reccomendation Notification actions

	private void AddRecommendationToView(BaseCardData cardData)
	{
		if (IsNotificationPresent() || null == cardData)
			return;

		GameObject notificationPrefabToMake = (GameObject) Resources.Load(PrefabPath + RecommendationPrefabName);

		if (null == notificationPrefabToMake)
		{
			Debug.LogError("Prefab " + RecommendationPrefabName + " could not be instantiated");
			return;
		}

		if (!IsPanelActive())
		{
			ActivatePanel();
		}

		_model.RecommendationViewController =
			NGUITools.AddChild(_model.ParentForNatification.gameObject, notificationPrefabToMake)
			         .GetComponent<RecommendationNotificationController>();

		_model.RecommendationViewController.transform.localPosition = _startNotificationPos;
		_model.RecommendationViewController.Init(cardData);
	}

	public void OnRecommendButton(BaseCardData baseCard)
	{
		_model.RecommendationLogicController.OnRecommendButton(baseCard);
	}

	public void OnCardDismissed(BaseCardData baseCard)
	{
		_model.RecommendationLogicController.OnDismissedCard(baseCard);
	}

	#endregion

	#region Notification manager general actions

	private void UpdateNotification()
	{
		if (_model == null)
			return;

		if (!_model.RecommendationLogicController.IsTimeToRefresh() || _model.RecommendationLogicController._currentlyShowing)
			return;

		if (!ITTDataCache.Instance.HasSessionCredentials)
			return;

		
		if (!Equals(ITTMainSceneManager.Instance.currentState, (Enum) ITTMainSceneManager.ITTStates.Main))
		{
			return;
		}

		_model.RecommendationLogicController.recTracker.timeLastShown = DateTime.UtcNow;

		ITTDataCache.Instance.Data.UpdateDataEntry((int)DataCacheIndices.RECOMMENDATIONTRACKER, _model.RecommendationLogicController.recTracker);

		UpdateRecommendation();
	
	}

	private void ActivatePanel()
	{
		_model.ParentForNatification.gameObject.SetActive(true);
	}

	private void DeactivatePanel()
	{
		_model.ParentForNatification.gameObject.SetActive(false);
	}

	private bool IsPanelActive()
	{
		return _model.ParentForNatification.gameObject.activeSelf;
	}

	/// <summary>
	/// Returns true if either a recommendation or a tip notification is present.
	/// </summary>
	/// <returns>
	/// <c>true</c> if a recommendataion or tip notification is present; otherwise, <c>false</c>.
	/// </returns>
	public bool IsNotificationPresent() {
		if (null != _model.RecommendationViewController) {
			return true;
		}
		if (null != _model.TipsViewController) {
			return true;
		}
		return false;
	}

	/// <summary>
	/// Returns true if either a recommendation or a tip notification is open.
	/// </summary>
	/// <returns>
	/// <c>true</c> if a recommendataion or tip notification is open; otherwise, <c>false</c>.
	/// </returns>
	public bool IsNotificationOpen() {
		bool ret = false;
		if (null != _model.RecommendationViewController) {
			ret |= _model.RecommendationViewController.IsOpen();
		}
		if (null != _model.TipsViewController) {
			ret |= _model.TipsViewController.IsOpen();
		}
		return ret;
	}

	public void CloseNotification()
	{
		if (null != _model.RecommendationViewController) {
			_model.RecommendationViewController.CloseNotification();
		}
		if (null != _model.TipsViewController) {
			_model.TipsViewController.CloseNotification();
		}
	}

	#region action for recommendation

	private void UpdateRecommendation()
	{
		_model.RecommendationLogicController.Update();
	}


	#endregion

	#region action for healthy tips

	private void UpdateHealthyTips()
	{
		_model.TipsLogicController.Update();
	}

	#endregion

	#endregion

	#region Healthy tips
	private void AddHealthyTipsToView(LargeTips tips)
	{
		if (IsNotificationPresent())
			return;

		GameObject tipsPrefabToMake = (GameObject) Resources.Load(PrefabPath + TipsPrefabName);

		if (null == tipsPrefabToMake)
		{
			Debug.LogError("Prefab " + TipsPrefabName + " could not be instantiated");
			return;
		}

		if (!IsPanelActive())
		{
			ActivatePanel();
		}

		_model.TipsViewController =
			NGUITools.AddChild(_model.ParentForNatification.gameObject, tipsPrefabToMake)
			         .GetComponent<NotificationTipsController>();

		_model.TipsViewController.transform.localPosition = _startTipsPos;
		_model.TipsViewController.Init(tips);
	}
	#endregion

	#endregion

	#region Test

	private void AddTestRecommendation()
	{
		if (IsNotificationPresent())
			return;

		GameObject notificationPrefabToMake = (GameObject) Resources.Load(PrefabPath + RecommendationPrefabName);

		if (null == notificationPrefabToMake)
		{
			Debug.LogError("Prefab " + RecommendationPrefabName + " could not be instantiated");
			return;
		}

		if (!IsPanelActive())
		{
			ActivatePanel();
		}

		_model.RecommendationViewController =
			NGUITools.AddChild(_model.ParentForNatification.gameObject, notificationPrefabToMake)
			         .GetComponent<RecommendationNotificationController>();

		_model.RecommendationViewController.transform.localPosition = _startNotificationPos;

		string title = "Recommedation Title " + Random.Range(0, 10000);
		_model.RecommendationViewController.Init(title);		
	}

	private void AddTestTips()
	{
		if (IsNotificationPresent())
			return;

		GameObject tipsPrefabToMake = (GameObject) Resources.Load(PrefabPath + TipsPrefabName);

		if (null == tipsPrefabToMake)
		{
			Debug.LogError("Prefab " + TipsPrefabName + " could not be instantiated");
			return;
		}

		if (!IsPanelActive())
		{
			ActivatePanel();
		}

		_model.TipsViewController =
			NGUITools.AddChild(_model.ParentForNatification.gameObject, tipsPrefabToMake)
			         .GetComponent<NotificationTipsController>();

		_model.TipsViewController.transform.localPosition = _startTipsPos;

		string title = "Tips Title " + Random.Range(0, 10000);
		string description = "Tips Description " + Random.Range(0, 10000);
		string url = "http://www.google.com";

		_model.TipsViewController.Init(title, description, url);
	}

	#endregion

}