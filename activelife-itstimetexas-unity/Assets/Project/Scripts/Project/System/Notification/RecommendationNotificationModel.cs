using UnityEngine;
using System.Collections;

public class RecommendationNotificationModel : MonoBehaviour {
	#region Variables

	public UIEventListener RecommendButton;
	public UIEventListener DidnotGoButton;
	public UIEventListener NoThanksButton;

	public UIEventListener TitleButton;

	public UIEventListener FullScreenTrigger;

	public UISprite MainBackground;
	public UIEventListener MainBackgroundButton;

	public UIWidget NotificationElements;

	public UILabel ActivitiesTitle;
	public UILabel RecommendedActiviteesTitle;

	public UIWidget RecommendationNotificationContainer;
	public UIWidget RecommendedContainer;

	public UIWidget AnimateContainerRecommendation;
	public UISprite RecommendIcon;

	public System.Int64 Id;

	public enum RecommendationNotificationState
	{
		SmallIcon,
		Showed,
		Dismissed,
		OpenAndDismissed,
		Recommend,
		DidnotGo,
		NoThanks,
		RecommendAndClose,
	}

	[HideInInspector] public RecommendationNotificationState State;

	#endregion
}
