using UnityEngine;
using System.Collections;
using ITT.System;

namespace ITT.Scene
{
	public class ToolTipViewController : MonoBehaviour, IScreenViewBase 
	{
		ToolTipViewModel model;

		private bool _videoIsDone = false;

		void Awake()
		{
			model = gameObject.GetComponent<ToolTipViewModel>();
			if (null == model)
			{
				throw new MissingComponentException();
			}
		}
		
		void Start()
		{
			AssignButtonDelegates();
		}
		
		public IEnumerator OnDisplay()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Tool Tip Screen");
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("FTUE - Tool Tip Screen")
			                                                     .SetEventAction("Enter - Tool Tip Screen")
			                                                     .SetEventLabel("User has entered the tool tip screen"));
			gameObject.SetActive(true);
			yield return StartCoroutine(HelperMethods.Instance.AnimateIn(gameObject));
		}
		
		public IEnumerator OnHide() 
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("FTUE - Tool Tip Screen")
			                                                     .SetEventAction("Exit - Tool Tip Screen")
			                                                     .SetEventLabel("User has exited the tool tip screen"));
			yield return StartCoroutine(HelperMethods.Instance.AnimateOut(gameObject));
		}

		public IEnumerator ForcedReverseIn()
		{
			gameObject.SetActive(true);
			yield return StartCoroutine(HelperMethods.Instance.ForcedReverseIn(gameObject));
		}
		
		public IEnumerator ForcedReverseOut()
		{
			yield return StartCoroutine(HelperMethods.Instance.ForcedReverseOut(gameObject));
			gameObject.SetActive(false);
		}
		
		public void AssignButtonDelegates()
		{
			model.skipButton.onClick.Add(new EventDelegate(this, "OnSkipPressed"));
			model.currentLocationButton.onClick.Add(new EventDelegate(this, "OnCurrentLocationPressed"));
			model.enterZipButton.onClick.Add(new EventDelegate(this, "OnEnterZipPressed"));
		}

		#region Button Response
		public void OnSkipPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("FTUE - Tool Tip Screen")
			                                                     .SetEventAction("Click - Tool Tip Skip Button")
			                                                     .SetEventLabel("User skipped the tool tip page"));
		}

		public void OnCurrentLocationPressed()
		{
			HelperMethods.AddInteractionBlocker();
			StartCoroutine(RetrieveLocation());
		}

		static public void FlagViewedTooltip()
		{
			ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.USER_FLAGS, FlagViewedTooltipCallback);
		}

		static private void FlagViewedTooltipCallback(DataEntryBase data)
		{
			UserFlagsEntry flags = data as UserFlagsEntry;
			if (null == flags)
				return;
			
			HelperMethods.UserFlags tempFlags = flags.Data;
			if (null == flags.Data)
				tempFlags = new HelperMethods.UserFlags();
			
			tempFlags.hasSeenTooltip = true;
			ITTDataCache.Instance.Data.UpdateDataEntry((int)DataCacheIndices.USER_FLAGS, tempFlags, false);
			
			ITTDataCache.Instance.Data.RemoveCallbackFromEntry((int)DataCacheIndices.USER_FLAGS, FlagViewedTooltipCallback);
		}

		private IEnumerator RetrieveLocation()
		{
			model.mainView.DynamicScrollView.ClearCarousel();
			model.mainView.NoActivitiesCard.SetActive(false);

			yield return StartCoroutine(HelperMethods.Instance.RetrieveLocationData(
				() => {
					FlagViewedTooltip();
					HelperMethods.RemoveInteractionBlocker();
					ITT.Scene.ITTMainSceneManager.Instance.currentState = ITT.Scene.ITTMainSceneManager.ITTStates.Main;
				}, 
				RetrieveLocationFail));
		}

		private void RetrieveLocationFail(string error)
		{
			HelperMethods.RemoveInteractionBlocker();
			ModalPopupOK.Spawn(error);
		}

		public void OnEnterZipPressed()
		{
			ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Location;
		}
		#endregion
	}
}