using UnityEngine;
using System.Collections;
using ITT.System;

namespace ITT.Scene
{
	public class TooltipPaginationDots : MonoBehaviour 
	{
		public ToolTipViewController tooltipController;
		public UISprite[] dotSprites;
		public UIScrollView targetScrollView;
		private UICenterOnChild _centerOnChild;

		private Color activeColor = Color.white;
		private Color inactiveColor = new Color(1f, 1f, 1f, 0.5f);

		void Start () 
		{
			if (null == targetScrollView)
			{
				Debug.LogError("TooltipPaginationDots: target scroll view not set!");
				enabled = false;
				return;
			}

			_centerOnChild = targetScrollView.gameObject.GetComponent<UICenterOnChild>();
			if (null == _centerOnChild)
			{
				Debug.LogError("TooltipPaginationDots: target scroll view has no UICenterOnChild component!");
				enabled = false;
				return;
			}

			_centerOnChild.OnCenteredObjectChanged = UpdatePageDot;

			if (null != dotSprites && dotSprites.Length > 0)
				dotSprites[0].color = activeColor;
		}

		void UpdatePageDot(GameObject centeredObject)
		{

			if (null == centeredObject)
			{
				foreach (UISprite sprite in dotSprites)
					sprite.color = inactiveColor;
			}
			else if (centeredObject.name == "ToolTipExploreCard")
			{
				ToggleDot(0);
			}
			else if (centeredObject.name == "ToolTipDiscoverCard")
			{
				ToggleDot(1);
			}
			else if (centeredObject.name == "ToolTipGetHelpCard")
			{
				ToggleDot(2);
			}
			else if (centeredObject.name == "ToolTipKeepTrackCard")
			{
				ToggleDot(3);
			}
			else if (centeredObject.name == "ToolTipGetLocationCard")
			{
				ToggleDot(4);
			}
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("FTUE - Tool Tip Screen")
			                                                     .SetEventAction("Swipe - Tool Tip Screen")
			                                                     .SetEventLabel("User is on card:" + centeredObject.name));
		}

		void ToggleDot(int index)
		{
			for (int i = 0; i < dotSprites.Length; i++)
			{
				dotSprites[i].color = (i == index ? activeColor : inactiveColor);
			}
		}

		void OnSwipedToFinalPage()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("FTUE - Tool Tip Screen")
			                                                     .SetEventAction("Final Swipe - Tool Tip Screen")
			                                                     .SetEventLabel("User has exited the tool tip screen"));

			ToolTipViewController.FlagViewedTooltip();
		}
	}
}