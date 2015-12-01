using System;
using ITT.System;
using UnityEngine;
using System.Collections;

public class RecommendationNotificationController : MonoBehaviour {
	#region Variables

	private RecommendationNotificationModel _notificationModel;

	public float ShiftY
	{
		get
		{
			if (_notificationModel != null)
			{
				switch (_notificationModel.State)
				{
					case RecommendationNotificationModel.RecommendationNotificationState.Recommend:
					case RecommendationNotificationModel.RecommendationNotificationState.RecommendAndClose:
						return -_notificationModel.RecommendedContainer.height;

					default:
						return -_notificationModel.MainBackground.height*_notificationModel.MainBackground.transform.localScale.y;

				}
			}
			return 0f;
		}
	}

	private BaseCardData _baseCard;

	public event Action<BaseCardData> OnRecommendEvent;
	public event Action<BaseCardData> OnDismissedEvent;

	#endregion

	#region MonoBehaviour

	void Awake ()
	{
		_notificationModel = GetComponent<RecommendationNotificationModel>();
		if (null == _notificationModel)
		{
			throw new MissingComponentException(
				"RecommendationNotificationController.Start - can't find RecommendationNotificationModel component in _notificationModel");
		}
	
		SubscribeEvents();
		_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.SmallIcon;
	}

	void OnDestroy()
	{
		UnsubscruibeEvents();
	}

	#endregion

	#region Actions

	private void SubscribeEvents()
	{
		_notificationModel.RecommendButton.onClick = OnRecommendButton;
		_notificationModel.DidnotGoButton.onClick = OnDidnotGoButton;
		_notificationModel.NoThanksButton.onClick = OnNoThanksButton;
		_notificationModel.TitleButton.onClick = OnTitleButton;
		_notificationModel.FullScreenTrigger.onClick = OnFullScreenTrigger;

		_notificationModel.FullScreenTrigger.onDrag = OnTitleButtonDrag;

		_notificationModel.TitleButton.onDrag = OnTitleButtonDrag;
		_notificationModel.MainBackgroundButton.onDrag = OnBackgroundDrag;
		_notificationModel.MainBackgroundButton.onClick = OnBackgroundClick;

		NotificationManager manager = FindObjectOfType<NotificationManager>();

		if (null == manager)
		{
			throw new MissingComponentException("RecommendationNotificationController.SubscribeEvents - cann't find NotificationManager");
		}

		OnRecommendEvent -= manager.OnRecommendButton;
		OnRecommendEvent += manager.OnRecommendButton;

		OnDismissedEvent -= manager.OnCardDismissed;
		OnDismissedEvent += manager.OnCardDismissed;

	}

	private void UnsubscruibeEvents()
	{
		NotificationManager manager = FindObjectOfType<NotificationManager>();

		if (null == manager)
		{
			return;
		}

		OnRecommendEvent -= manager.OnRecommendButton;
		OnDismissedEvent -= manager.OnCardDismissed;
	}

	public void Init(BaseCardData cardData)
	{
		//need to set the notification name


		_baseCard = cardData;

		_notificationModel.ActivitiesTitle.text = _baseCard.title;
		_notificationModel.RecommendedActiviteesTitle.text = _baseCard.title;
		_notificationModel.Id = cardData.id;
	}

	public void Init(string title)
	{
		_baseCard = null;

		_notificationModel.ActivitiesTitle.text = title;
		_notificationModel.RecommendedActiviteesTitle.text = title;
	}

	#region Recommendation window action

	private void OnRecommendButton(GameObject sender)
	{
		if (null != _baseCard)
		{
			Debug.Log("RecommendationNotificationController.OnRecommendButton - OK, card: " + _baseCard.title);

			ITTDataCache.Instance.RetrieveProfileActivityID(_baseCard.id, OnRetrieveProfileActivitySuccess, OnRetrieveProfileActivityFailure);

			if (null != OnRecommendEvent)
			{
				OnRecommendEvent(_baseCard);
			}
		}

		_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.Recommend;

		HideRecommendationWindow();
		ShowRecommendedWindow();
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
		Debug.LogError("DetailCardViewModel.OnRetrieveProfileActivityFailure error: " + error);
	}

	private void OnDidnotGoButton(GameObject sender)
	{
		Debug.Log("RecommendationNotificationController.OnDidnotGoButton - OK");
		_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.DidnotGo;
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Notification - Recommendation")
		                                                     .SetEventAction("Click - Didn't Go Button")
		                                                     .SetEventLabel("User clicked Didn't Go button for card id: " + _notificationModel.Id + " " + _notificationModel.ActivitiesTitle));

		HideNotificationElements();
	}

	private void OnNoThanksButton(GameObject sender)
	{
		Debug.Log("RecommendationNotificationController.OnNoThanks - OK");

		_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.NoThanks;
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Notification - Recommendation")
		                                                     .SetEventAction("Click - No Thanks Button")
		                                                     .SetEventLabel("User clicked No Thanks button for card id: " + _notificationModel.Id + " " + _notificationModel.ActivitiesTitle));

		HideNotificationElements();
	}

	private void OnTitleButton(GameObject sender)
	{
		Debug.Log("RecommendationNotificationController.OnTitleButton - OK");

		if (_notificationModel.State == RecommendationNotificationModel.RecommendationNotificationState.Showed)
		{
			_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.OpenAndDismissed;

			HideNotificationElements();
		}

		if (_notificationModel.State != RecommendationNotificationModel.RecommendationNotificationState.SmallIcon)
			return;

		TweenScale tween = _notificationModel.MainBackground.GetComponent<TweenScale>();

		if (null == tween)
		{
			throw new MissingComponentException(
				"RecommendationNotificationController.OnTitleButton - Missing TweenScale component");
		}

		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Notification - Recommendation")
		                                                     .SetEventAction("Click - Recommendation Opened")
		                                                     .SetEventLabel("User opened recommendation for card id: " + _notificationModel.Id + " " + _notificationModel.ActivitiesTitle));

		tween.PlayForward();
		_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.Showed;
	}
	
	public void ShowNotificationElements()
	{
		if (_notificationModel.State == RecommendationNotificationModel.RecommendationNotificationState.Showed)
		{

			TweenAlpha tween = _notificationModel.NotificationElements.GetComponent<TweenAlpha>();

			if (null == tween)
			{
				throw new MissingComponentException(
					"RecommendationNotificationController.ShowNotificationElements - Missing TweenAlpha component");
			}

			tween.PlayForward();
		}
	}

	private void HideNotificationElements()
	{
		if (OnDismissedEvent != null)
		{
			if (null != _baseCard)
				OnDismissedEvent(_baseCard);
		}

		TweenAlpha tween = _notificationModel.NotificationElements.GetComponent<TweenAlpha>();

		if (null == tween)
		{
			throw new MissingComponentException(
				"RecommendationNotificationController.HideNotificationElements - Missing TweenAlpha component");
		}

		tween.PlayReverse();

		tween.SetOnFinished(HideAndCloseNotification);
	}

	private void HideAndCloseNotification()
	{
		if (_notificationModel.State != RecommendationNotificationModel.RecommendationNotificationState.DidnotGo &&
			_notificationModel.State != RecommendationNotificationModel.RecommendationNotificationState.NoThanks &&
			_notificationModel.State != RecommendationNotificationModel.RecommendationNotificationState.OpenAndDismissed)
			return;

		TweenScale tween = _notificationModel.MainBackground.GetComponent<TweenScale>();

		if (null == tween)
		{
			throw new MissingComponentException(
				"RecommendationNotificationController.HideAndCloseNotification - Missing TweenScale component");
		}

		tween.PlayReverse();

		tween.SetOnFinished(CloseNotification);
	}

	private void HideRecommendationWindow()
	{
		TweenAlpha tween = _notificationModel.RecommendationNotificationContainer.GetComponent<TweenAlpha>();

		if (null == tween)
		{
			throw new MissingComponentException(
				"RecommendationNotificationController.HideRecommendationWindow - Missing TweenAlpha component");
		}

		tween.PlayForward();

		tween.SetOnFinished(
			() => NGUITools.SetActive(_notificationModel.RecommendationNotificationContainer.gameObject, false));
	}

	public void CloseNotification()
	{
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Notification - Recommendation")
		                                                     .SetEventAction("Click - Recommendation Close")
		                                                     .SetEventLabel("User closed recommendation for id: " + _notificationModel.Id + " " + _notificationModel.ActivitiesTitle));

		Destroy(gameObject);
	}

	#endregion

	#region Recommended window actions

	private void ShowRecommendedWindow()
	{
		NGUITools.SetActive(_notificationModel.RecommendedContainer.gameObject, true);
	}

	public void StartMainAnimation()
	{
		TweenScale tween = _notificationModel.AnimateContainerRecommendation.GetComponent<TweenScale>();

		if (null == tween)
		{
			throw new MissingComponentException(
				"RecommendationNotificationController.StartMainAnimation - Missing TweenScale component");
		}

		tween.PlayForward();
		StartBounceAnimation();
	}

	private void StartBounceAnimation()
	{
		TweenScale tween = _notificationModel.RecommendIcon.GetComponent<TweenScale>();

		if (null == tween)
		{
			throw new MissingComponentException(
				"RecommendationNotificationController.StartBounceAnimation - Missing TweenScale component");
		}

		tween.PlayForward();
		tween.SetOnFinished(() =>
			{
				tween.from = Vector3.one;
				tween.duration = 0.2f;
				tween.PlayReverse();
				_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.RecommendAndClose;
			});
	}
	#endregion

	#region FullScreenTrigger

	private void OnFullScreenTrigger(GameObject sender)
	{
		Debug.Log("RecommendationNotificationController.OnFullScreenTrigger - OK");
		switch (_notificationModel.State)
		{
			case RecommendationNotificationModel.RecommendationNotificationState.SmallIcon:
				_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.Dismissed;
				CloseNotification();
				break;

			case RecommendationNotificationModel.RecommendationNotificationState.RecommendAndClose:
				
				CloseNotification();
				break;

			case RecommendationNotificationModel.RecommendationNotificationState.Showed:
				_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.OpenAndDismissed;

				HideNotificationElements();
				break;
		}
	}

	#endregion

	#region Swipe actions

	private const float VerticalThreshold = 35f;
	private const float HorizontalThreshold = 35f;
	private const float AnimationDurationTime = 0.25f;

	private void OnTitleButtonDrag(GameObject sender, Vector2 delta)
	{
		//Debug.Log("RecommendationNotificationController.OnTitleButtonDrag - OK, delta is: " +
		//		  UICamera.currentTouch.totalDelta + "state: " + _notificationModel.State);

		if (_notificationModel.State == RecommendationNotificationModel.RecommendationNotificationState.SmallIcon)
		{

			if (- UICamera.currentTouch.totalDelta.y > VerticalThreshold &&
			    Mathf.Abs(UICamera.currentTouch.totalDelta.x) < HorizontalThreshold)
			{
				OnTitleButton(gameObject);
			}

			else if (- UICamera.currentTouch.totalDelta.x > HorizontalThreshold &&
			         Mathf.Abs(UICamera.currentTouch.totalDelta.y) < VerticalThreshold)
			{
				_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.Dismissed;
				OnSwipeLeft();
			}
			else if (UICamera.currentTouch.totalDelta.x > HorizontalThreshold &&
			         Mathf.Abs(UICamera.currentTouch.totalDelta.y) < VerticalThreshold)
			{
				_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.Dismissed;
				OnSwipeRight();
			}
		}
		else if (_notificationModel.State == RecommendationNotificationModel.RecommendationNotificationState.Showed)
		{
			if (UICamera.currentTouch.totalDelta.y > VerticalThreshold &&
			    Mathf.Abs(UICamera.currentTouch.totalDelta.x) < HorizontalThreshold)
			{
				OnPushUp();
			}
			else if (- UICamera.currentTouch.totalDelta.x > HorizontalThreshold &&
			         Mathf.Abs(UICamera.currentTouch.totalDelta.y) < VerticalThreshold)
			{
				_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.OpenAndDismissed;
				OnSwipeLeft();
			}
			else if (UICamera.currentTouch.totalDelta.x > HorizontalThreshold &&
			         Mathf.Abs(UICamera.currentTouch.totalDelta.y) < VerticalThreshold)
			{
				_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.OpenAndDismissed;
				OnSwipeRight();
			}
		}
		else if (_notificationModel.State == RecommendationNotificationModel.RecommendationNotificationState.RecommendAndClose)
		{
			if (- UICamera.currentTouch.totalDelta.x > HorizontalThreshold &&
			    Mathf.Abs(UICamera.currentTouch.totalDelta.y) < VerticalThreshold)
			{
				_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.RecommendAndClose;
				OnSwipeLeft();
			}
			else if (UICamera.currentTouch.totalDelta.y > VerticalThreshold &&
			         Mathf.Abs(UICamera.currentTouch.totalDelta.x) < HorizontalThreshold)
			{
				CloseNotification();
			}
			else if (UICamera.currentTouch.totalDelta.x > HorizontalThreshold &&
			         Mathf.Abs(UICamera.currentTouch.totalDelta.y) < VerticalThreshold)
			{
				_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.RecommendAndClose;
				OnSwipeRight();
			}
		}

	}

	private void OnBackgroundDrag(GameObject sender, Vector2 delta)
	{
		if (_notificationModel.State == RecommendationNotificationModel.RecommendationNotificationState.Showed)
		{
			if (UICamera.currentTouch.totalDelta.y > VerticalThreshold &&
			    Mathf.Abs(UICamera.currentTouch.totalDelta.x) < HorizontalThreshold)
			{
				OnPushUp();
			}
			else if (- UICamera.currentTouch.totalDelta.x > HorizontalThreshold &&
			         Mathf.Abs(UICamera.currentTouch.totalDelta.y) < VerticalThreshold)
			{
				_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.OpenAndDismissed;
				OnSwipeLeft();
			}
			else if ( UICamera.currentTouch.totalDelta.x > HorizontalThreshold &&
			         Mathf.Abs(UICamera.currentTouch.totalDelta.y) < VerticalThreshold)
			{
				_notificationModel.State = RecommendationNotificationModel.RecommendationNotificationState.OpenAndDismissed;
				OnSwipeRight();
			}
		}
	}

	private void OnBackgroundClick(GameObject sender)
	{
		if (_notificationModel.State == RecommendationNotificationModel.RecommendationNotificationState.Showed)
		{
			OnPushUp();
		}
	}

	private void OnSwipeLeft()
	{
		TweenPosition tweenPos = gameObject.AddComponent<TweenPosition>();

		tweenPos.animationCurve = ColorManagementSystemController.EasyInOutAnimCurve;

		Vector3 curerntPos = transform.localPosition;

		tweenPos.from = curerntPos;

		tweenPos.to = new Vector3( - _notificationModel.MainBackground.width * 1.2f, curerntPos.y, curerntPos.z);

		tweenPos.duration = AnimationDurationTime;

		tweenPos.SetOnFinished(() => Destroy(tweenPos));

		TweenAlpha tween = gameObject.AddComponent<TweenAlpha>();

		tween.to = 0f;

		tween.duration = AnimationDurationTime;

		if (null != OnDismissedEvent)
			OnDismissedEvent(_baseCard);

		tween.SetOnFinished(() => { 
			Destroy(tween);
			CloseNotification();
		});
	}

	private void OnSwipeRight()
	{
		TweenPosition tweenPos = gameObject.AddComponent<TweenPosition>();

		tweenPos.animationCurve = ColorManagementSystemController.EasyInOutAnimCurve;

		Vector3 curerntPos = transform.localPosition;

		tweenPos.from = curerntPos;

		tweenPos.to = new Vector3( _notificationModel.MainBackground.width * 1.2f, curerntPos.y, curerntPos.z);

		tweenPos.duration = AnimationDurationTime;

		tweenPos.SetOnFinished(() => Destroy(tweenPos));

		TweenAlpha tween = gameObject.AddComponent<TweenAlpha>();

		tween.to = 0f;

		tween.duration = AnimationDurationTime;

		if (null != OnDismissedEvent)
			OnDismissedEvent(_baseCard);

		tween.SetOnFinished(() => { 
			Destroy(tween);
			CloseNotification();
		});
	}

	private void OnPushUp()
	{
		OnNoThanksButton(gameObject);
	}
	#endregion

	public bool IsOpen()
	{
		return _notificationModel.State == RecommendationNotificationModel.RecommendationNotificationState.Showed;
	}

	#endregion
}
