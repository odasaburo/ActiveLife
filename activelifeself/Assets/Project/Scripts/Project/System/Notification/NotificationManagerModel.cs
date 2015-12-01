using System;
using System.Collections.Generic;
using ITT.System;
using UnityEngine;
using System.Collections;

public class NotificationManagerModel : MonoBehaviour
{
	public UIWidget Container;

	[HideInInspector] public RecommendationNotificationController RecommendationViewController;
	[HideInInspector] public NotificationTipsController TipsViewController;

	public Transform ParentForNatification;

	[HideInInspector] public DateTime RefreshTimeStamp;
	[HideInInspector] public DateTime TipsTimeStamp;

	[HideInInspector] public TipsController TipsLogicController;
	[HideInInspector] public Tips CurrentTips;

	[HideInInspector] public RecommendationController RecommendationLogicController;
}
