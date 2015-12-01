using UnityEngine;
using System.Collections;

public class NotificationTipsModel : MonoBehaviour {
	#region Variables

	public UILabel Title;
	public UIEventListener TopBar;


	public UIWidget BottomContainer;
	public UISprite BottomBackground;

	public UILabel DescripionLabel;

	public UIEventListener WebsiteButton;
	public UILabel WebsiteLabel;
	public UIEventListener CallButton;

	public UIEventListener FullScreenTrigger;

	public UIEventListener DescriptionBackgroundButton;
	public UISprite DescriptionBackgroundSprite;

	public System.Int64 Id;

	public enum NotificationTipsState
	{
		SmallIcon,
		Showed,
		OpenAndDismissed,
		OpenSiteAndDismissed,
		CallToCoachAndDissmissed,
		Dismissed
	}

	[HideInInspector] public NotificationTipsState State;

	#endregion
}
