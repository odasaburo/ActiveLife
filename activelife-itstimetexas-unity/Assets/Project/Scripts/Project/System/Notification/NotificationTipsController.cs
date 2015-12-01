using ITT.System;
using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;

public class NotificationTipsController : MonoBehaviour {
	
	

	#region Variables

	private NotificationTipsModel _tipsModel;

	private string _healtherCoachPhoneNumber = "18442626224";

	private const float VerticalThreshold = 35f;
	private const float HorizontalThreshold = 35f;
	private const float AnimationDurationTime = 0.25f;

	public float ShiftY
	{
		get
		{
			if (_tipsModel != null)
			{
				switch (_tipsModel.State)
				{
					case NotificationTipsModel.NotificationTipsState.Showed:
						return -_tipsModel.DescriptionBackgroundSprite.height;

					default:
						return -_tipsModel.BottomBackground.height*_tipsModel.BottomBackground.transform.localScale.y;

				}
			}
			return 0f;
		}
	}
	
	#endregion


	#region Mono behaviour actions

	void Awake ()
	{

		_tipsModel = GetComponent<NotificationTipsModel>();
		if (null == _tipsModel)
		{
			throw new MissingComponentException(
				"NotificationTipsController.Awake - can't find NotificationTipsModel component in _tipsModel");
		}
		SubscribeEvents();
		_tipsModel.State = NotificationTipsModel.NotificationTipsState.SmallIcon;
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

	}

	private void OnApplicationPause(bool pause)
	{
		if (!pause)

			if (_tipsModel.State == NotificationTipsModel.NotificationTipsState.OpenAndDismissed)
			{
				CloseNotification();
			}
	}

	#endregion

	#region Actions

	public void Init(LargeTips tips)
	{
		_tipsModel.Title.text = tips.title;
		_tipsModel.DescripionLabel.text = HelperMethods.Instance.StripHtmlTags(tips.Description);
		_tipsModel.WebsiteLabel.text = string.IsNullOrEmpty(tips.path) ? "N/A" : tips.path;
		_tipsModel.Id = tips.id;
	}

	public void Init(string title, string description, string url)
	{
		_tipsModel.Title.text = title;
		_tipsModel.DescripionLabel.text = description;
		_tipsModel.WebsiteLabel.text = string.IsNullOrEmpty(url) ? "N/A" : url;
	}

	private void SubscribeEvents()
	{
		_tipsModel.TopBar.onClick = OnTopBarClick;
		_tipsModel.FullScreenTrigger.onClick = OnFullTriggerClicked;
		_tipsModel.FullScreenTrigger.onDrag = OnTopBarDrag;
		_tipsModel.CallButton.onClick = OnPhoneCallClick;
		_tipsModel.WebsiteButton.onClick = OnWebSiteClick;

		_tipsModel.TopBar.onDrag = OnTopBarDrag;

		_tipsModel.DescriptionBackgroundButton.onDrag = OnBackgroundDrag;

		_tipsModel.DescriptionBackgroundButton.onClick = OnFullTriggerClicked;
	}

	private void UnsubscribeEvents()
	{
		
	}

	#region Event action

	private void OnTopBarClick(GameObject sender)
	{
		Debug.Log("NotificationTipsController.OnTopBarButton - OK");

		if (_tipsModel.State == NotificationTipsModel.NotificationTipsState.SmallIcon)
		{
			_tipsModel.State = NotificationTipsModel.NotificationTipsState.Showed;
			OpenNotification();
		}
	}

	private void OnTopBarDrag(GameObject sender, Vector2 delta)
	{
		if (_tipsModel.State == NotificationTipsModel.NotificationTipsState.SmallIcon)
		{

			if (- UICamera.currentTouch.totalDelta.y > VerticalThreshold &&
			    Mathf.Abs(UICamera.currentTouch.totalDelta.x) < HorizontalThreshold)
			{
				OnTopBarClick(gameObject);
			}

			else if (- UICamera.currentTouch.totalDelta.x > HorizontalThreshold &&
			         Mathf.Abs(UICamera.currentTouch.totalDelta.y) < VerticalThreshold)
			{
				_tipsModel.State = NotificationTipsModel.NotificationTipsState.Dismissed;
				OnSwipeLeft();
			}

			else if ((UICamera.currentTouch.totalDelta.x > HorizontalThreshold &&
			          Mathf.Abs(UICamera.currentTouch.totalDelta.y) < VerticalThreshold))
			{
				_tipsModel.State = NotificationTipsModel.NotificationTipsState.Dismissed;
				OnSwipeRight();
			}
		}
		else if (_tipsModel.State == NotificationTipsModel.NotificationTipsState.Showed)
		{
			if (UICamera.currentTouch.totalDelta.y > VerticalThreshold &&
			    Mathf.Abs(UICamera.currentTouch.totalDelta.x) < HorizontalThreshold)
			{
				OnPushUp();
			}
			else if (- UICamera.currentTouch.totalDelta.x > HorizontalThreshold &&
			         Mathf.Abs(UICamera.currentTouch.totalDelta.y) < VerticalThreshold)
			{
				_tipsModel.State = NotificationTipsModel.NotificationTipsState.Dismissed;
				OnSwipeLeft();
			}
			else if (UICamera.currentTouch.totalDelta.x > HorizontalThreshold &&
			         Mathf.Abs(UICamera.currentTouch.totalDelta.y) < VerticalThreshold)
			{
				_tipsModel.State = NotificationTipsModel.NotificationTipsState.Dismissed;
				OnSwipeRight();
			}
		}
	}

	private void OnFullTriggerClicked(GameObject sender)
	{
		Debug.Log("NotificationTipsController.OnFullTriggerClicked - OK");

		switch (_tipsModel.State)
		{
			case NotificationTipsModel.NotificationTipsState.SmallIcon:
				CloseNotification();
				break;

			case NotificationTipsModel.NotificationTipsState.Showed:
				OnPushUp();
				break;
		}

	}

	private void OnBackgroundDrag(GameObject sender, Vector2 delta)
	{
		//Debug.Log("NotificationTipsController.OnBackgroundDrag - OK");

		if (_tipsModel.State ==NotificationTipsModel.NotificationTipsState.Showed)
		{
			if (UICamera.currentTouch.totalDelta.y > VerticalThreshold &&
			    Mathf.Abs(UICamera.currentTouch.totalDelta.x) < HorizontalThreshold)
			{
				OnPushUp();
			}
		}
	}

	private void OnWebSiteClick(GameObject sender)
	{
		Debug.Log("NotificationTipsController.OnWebSiteClick - OK");

		OnPushUp();

		if (string.IsNullOrEmpty(_tipsModel.WebsiteLabel.text) || _tipsModel.WebsiteLabel.text.Equals("N/A"))
		{
			return;
		}

		string website = _tipsModel.WebsiteLabel.text;
		string pattern = "https?://";
		if (string.IsNullOrEmpty(Regex.Match(website, pattern).ToString()))
		{
			website = "http://" + website;
		}

		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Notification - Healthy Tip")
		                                                     .SetEventAction("Click - Website Button")
		                                                     .SetEventLabel("User clicked website button for tip id: " + _tipsModel.Id + " " + _tipsModel.Title));

		Application.OpenURL(website);
	}

	private void OnPhoneCallClick(GameObject sender)
	{
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Notification - Healthy Tip")
		                                                     .SetEventAction("Click - Phone Button")
		                                                     .SetEventLabel("User clicked Healthy Tips coach hotline button"));

		HelperMethods.Instance.FormatAndCallNumber(_healtherCoachPhoneNumber);
	}

	#endregion

	private void OpenNotification(bool force = false)
	{
		if (_tipsModel.State != NotificationTipsModel.NotificationTipsState.Showed && !force) 
			return;

		TweenScale tween = _tipsModel.BottomBackground.GetComponent<TweenScale>();

		if (null == tween)
		{
			throw new MissingComponentException(
				"NotificationTipsController.OpenNotification - Missing TweenScale component");
		}

		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Notification - Healthy Tip")
		                                                     .SetEventAction("Click - Tip Opened")
		                                                     .SetEventLabel("User opened tip id: " + _tipsModel.Id + " " + _tipsModel.Title));

		tween.PlayForward();
		tween.SetOnFinished(ShowEntireComponents);
	}

	private void ShowEntireComponents()
	{
		if (_tipsModel.State != NotificationTipsModel.NotificationTipsState.Showed)
			return;

		TweenAlpha tween = _tipsModel.BottomContainer.GetComponent<TweenAlpha>();

		if (null == tween)
		{
			throw new MissingComponentException(
				"NotificationTipsController.OpenNotification - Missing TweenScale component");
		}

		tween.PlayForward();
	}

	private void HideEntireComponents()
	{
		TweenAlpha tween = _tipsModel.BottomContainer.GetComponent<TweenAlpha>();

		if (null == tween)
		{
			throw new MissingComponentException(
				"NotificationTipsController.OpenNotification - Missing TweenScale component");
		}

		_tipsModel.State = NotificationTipsModel.NotificationTipsState.OpenAndDismissed;

		tween.PlayReverse();
		tween.SetOnFinished(HideAndCloseNotification);
	}

	private void HideAndCloseNotification()
	{
		if (_tipsModel.State != NotificationTipsModel.NotificationTipsState.OpenAndDismissed &&
			_tipsModel.State != NotificationTipsModel.NotificationTipsState.OpenSiteAndDismissed &&
			_tipsModel.State != NotificationTipsModel.NotificationTipsState.CallToCoachAndDissmissed) 
			return;

		TweenScale tween = _tipsModel.BottomBackground.GetComponent<TweenScale>();

		if (null == tween)
		{
			throw new MissingComponentException(
				"NotificationTipsController.OpenNotification - Missing TweenScale component");
		}

		tween.PlayReverse();
		tween.SetOnFinished(CloseNotification);
	}

	public void CloseNotification()
	{
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Notification - Healthy Tip")
		                                                     .SetEventAction("Click - Tip Closed")
		                                                     .SetEventLabel("User closed tip id: " + _tipsModel.Id + " " + _tipsModel.Title));

		_tipsModel.State = NotificationTipsModel.NotificationTipsState.SmallIcon;
		Destroy(gameObject);
	}

	
	private void OnSwipeLeft()
	{
		TweenPosition tweenPos = gameObject.AddComponent<TweenPosition>();

		tweenPos.animationCurve = ColorManagementSystemController.EasyInOutAnimCurve;

		Vector3 curerntPos = transform.localPosition;

		tweenPos.from = curerntPos;

		tweenPos.to = new Vector3( - _tipsModel.BottomBackground.width * 1.2f, curerntPos.y, curerntPos.z);

		tweenPos.duration = AnimationDurationTime;

		tweenPos.SetOnFinished(() => Destroy(tweenPos));

		TweenAlpha tween = gameObject.AddComponent<TweenAlpha>();

		tween.to = 0f;

		tween.duration = AnimationDurationTime;

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

		tweenPos.to = new Vector3( _tipsModel.BottomBackground.width * 1.2f, curerntPos.y, curerntPos.z);

		tweenPos.duration = AnimationDurationTime;

		tweenPos.SetOnFinished(() => Destroy(tweenPos));

		TweenAlpha tween = gameObject.AddComponent<TweenAlpha>();

		tween.to = 0f;

		tween.duration = AnimationDurationTime;

		tween.SetOnFinished(() => { 
			Destroy(tween);
			CloseNotification();
		});
	}

	private void OnPushUp()
	{
		HideEntireComponents();
	}

	private void OnPulledDown()
	{
		OpenNotification();
	}

	public bool IsOpen()
	{
		return _tipsModel.State == NotificationTipsModel.NotificationTipsState.Showed;
	}

	#endregion
}
// Значит смотри: по поводу  длл. Там есть солюшн файл NavyFightClient.Mono.sln. В него заходишь через visual studio и нажимаешь ctrl+shift+b
// и длл собирётся