using UnityEngine;
using System;

public class SwipeDetector : MonoBehaviour 
{
	public float minVerticalSwipe;
	public float minHorizontalSwipe;
	private Action<GameObject> showNotificationDetail;
	private Action<GameObject> DismissNotification;

	void Awake()
	{
		showNotificationDetail = transform.parent.GetComponent<NotificationViewController>().PresentNotificationDetail;
		DismissNotification = transform.parent.GetComponent<NotificationViewController>().closeAndDismiss;
	}

	void OnPress(bool isPressed)
	{
		if(!isPressed)
		{
			float verticalSwipeDistance = UICamera.currentTouch.totalDelta.y;
			float horizontalSwipeDistance = UICamera.currentTouch.totalDelta.x;

			bool isVerticalSwipe = Mathf.Abs(verticalSwipeDistance) > minVerticalSwipe;
			bool isHorizontalSwipe = Mathf.Abs(horizontalSwipeDistance) > minHorizontalSwipe;
			
			if( isVerticalSwipe && !isHorizontalSwipe )
			{
				//isDownSwipe
				if( verticalSwipeDistance < 0 ) {
					showNotificationDetail(null); 
				}
				//isUpSwipe
				else {
					DismissNotification(null);
				}
			}
			else if( isHorizontalSwipe && !isVerticalSwipe )
			{
				//isLeftSwipe 
				if( horizontalSwipeDistance < 0 ) {
					DismissNotification(null);
				}
			}
		}
	}
}