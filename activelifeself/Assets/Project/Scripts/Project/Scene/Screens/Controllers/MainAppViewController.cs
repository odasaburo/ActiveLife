using UnityEngine;
using System.Collections;
using ITT.System;

namespace ITT.Scene
{
	public class MainAppViewController : MonoBehaviour, IScreenViewBase 
	{
		#region Model Member
		private MainAppViewModel model;
		#endregion

		#region Local Logic Variables
		public string LoadingText;
		private int dotCount = 0;
		#endregion

		void Awake()
		{
			model = gameObject.GetComponent<MainAppViewModel>();
			if (null == model)
			{
				throw new MissingComponentException();
			}

			model.DynamicScrollView.OnActivitiesPopulated += DisableLoadingText;

		}
		
		IEnumerator Start()
		{
			AssignButtonDelegates();
			if (null == model.DynamicScrollView.dayOfWeekController.SelectedDay)
				yield return null;

			model.DynamicScrollView.Init(620, 600, 18f, HideNoActivitiesCard, ShowEndOfDayCard, -1, false);
			StartCoroutine(model.DynamicScrollView.RefreshCarousel());
		}

		
		void Update()
		{
			if (Input.GetKeyDown (KeyCode.Escape))
			{
				if (!ITTMainSceneManager.Instance.OverlayIsOpen)
				{
					Application.Quit ();
				}
			}
		}

		private void DisableLoadingText()
		{
			HideNoActivitiesCard(model.DynamicScrollView.GetCurrentCardCount() != 0);
		}

		public void HideNoActivitiesCard(bool hide)
		{
			if (hide && model.NoActivitiesCard.activeSelf == true)
			{
				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Main - Main Screen")
				                                                     .SetEventAction("Hide - No Activities Card")
				                                                     .SetEventLabel("The No Activities notification card is hidden"));
			}
			else if (!hide && model.NoActivitiesCard.activeSelf != false)
			{
				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Main - Main Screen")
				                                                     .SetEventAction("Show - No Activities Card")
				                                                     .SetEventLabel("The No Activities notification card is shown"));
			}

			model.NoActivitiesDetailsLabel.text = model.NoActivitiesDefaultText;
			model.NoActivitiesCard.SetActive(!hide);
		}

		public void ShowEndOfDayCard(bool show)
		{
			if (show)
			{
				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Main - Main Screen")
				                                                     .SetEventAction("Show - End of Day Card")
				                                                     .SetEventLabel("The End of Day notification card is shown"));
			}
			else
			{
				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Main - Main Screen")
				                                                     .SetEventAction("Hide - End of Day Card")
				                                                     .SetEventLabel("The End of Day notification card is hidden"));
			}

			model.NoActivitiesDetailsLabel.text = model.NoActivitiesEndOfDayText;
			model.NoActivitiesCard.SetActive(show);
		}

		public int CurrentCardCount()
		{
			return model.DynamicScrollView.GetCurrentCardCount();
		}

		public IEnumerator OnDisplay()
		{
			gameObject.SetActive(true);

			ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.USER_FLAGS, OnRetrievedUserFlags);
			yield return StartCoroutine(HelperMethods.Instance.AnimateIn(gameObject));

		}

		void OnRetrievedUserFlags(DataEntryBase data)
		{
			UserFlagsEntry flags = data as UserFlagsEntry;
			
			if (null != flags || null != flags.Data)
			{
				if (flags.Data.hasToggledLocation == true)
				{
					model.LocationLabel.text = "Current Location";
				}
				else
				{
					LocationDataModel locationData = ITTDataCache.Instance.RetrieveLocationData();
					if (null == locationData) {
						locationData = ITTDataCache.Instance.DefaultLocationData();
					}
					model.LocationLabel.text = (!string.IsNullOrEmpty(locationData.zipCode)) ? locationData.zipCode : (!string.IsNullOrEmpty(locationData.cityState)) ? locationData.cityState : "Austin, TX";
				}
			}
			
			ITTDataCache.Instance.Data.RemoveCallbackFromEntry((int)DataCacheIndices.USER_FLAGS, OnRetrievedUserFlags);
		}

		public IEnumerator OnHide()
		{
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
				
		public void AssignButtonDelegates ()
		{
			model.profileButton.onClick.Add		(new EventDelegate(this, "OnProfilePressed"	 ));
			model.filterButton.onClick.Add		(new EventDelegate(this, "OnFilterPressed"	 ));
			model.locationButton.onClick.Add	(new EventDelegate(this, "OnLocationPressed" ));
		}
		
		private void OnProfilePressed()
		{
			if (ITTDataCache.Instance.HasNetworkConnection)
			{
				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Main - Main Screen")
				                                                     .SetEventAction("Click - Profile Button")
				                                                     .SetEventLabel("The user has pressed on the profile button"));
				StartCoroutine(ITTMainSceneManager.Instance.AttemptAuthenticatedStateChange(ITTMainSceneManager.ITTStates.Profile));
			}
			else
			{
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.");
			}
		}
		private void OnFilterPressed()
		{
			if (ITTDataCache.Instance.HasNetworkConnection)
			{
				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Main - Main Screen")
				                                                     .SetEventAction("Click - Filter Button")
				                                                     .SetEventLabel("The user has pressed on the filter button"));
				ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Filter;
			}
			else
			{
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.");
			}
		}
		private void OnLocationPressed()
		{
			if (ITTDataCache.Instance.HasNetworkConnection)
			{
				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Main - Main Screen")
				                                                     .SetEventAction("Click - Location Button")
				                                                     .SetEventLabel("The user has pressed on the location button"));
				ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Location;
			}
			else
			{
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.");
			}
		}

		public void HideScrollView(bool hide, bool checkCardCountFirst = false)
		{
			bool shouldHide = hide;
			if (checkCardCountFirst)
				shouldHide = model.DynamicScrollView.GetCurrentCardCount() == 0;

			model.DynamicScrollView.HideCarousel(shouldHide);
		}

		public void ClearScrollView()
		{
			model.DynamicScrollView.ClearCarousel();
		}

		public void HideNoActivitiesText()
		{
			model.NoActivitiesLabel.gameObject.SetActive(false);
		}

		public void ShowNoActivitiesText()
		{
			model.NoActivitiesLabel.gameObject.SetActive(true);
		}

		public void OnFacebookSharePressed(string labelText)
		{
			StartCoroutine(AnimateFacebookShareConfirmation(labelText));
		}

		private IEnumerator AnimateFacebookShareConfirmation(string labelText)
		{
			// Update colors
			UILabel label = model.FacebookShareConfirmation.GetComponentInChildren<UILabel>();
			if (null != label)
			{
				label.color = ColorManagementSystemController.GetCurrentTextColor();
				label.text = labelText;
			}
			UISprite sprite = model.FacebookShareConfirmation.GetComponentInChildren<UISprite>();
			if (null != sprite)
			{
				switch (labelText)
				{
				case "SUCCESS":
					sprite.spriteName = "Checkmark";
					sprite.transform.localPosition = new Vector3(-90.0f , sprite.transform.position.y, sprite.transform.position.z);
					break;
				case "CANCELLED":
					sprite.spriteName = "ClearSearch"; 
					sprite.transform.localPosition = new Vector3(-110.0f , sprite.transform.position.y, sprite.transform.position.z);
					break;
				case "FAILED":
					sprite.spriteName = "ClearSearch"; 
					sprite.transform.localPosition = new Vector3(-80.0f , sprite.transform.position.y, sprite.transform.position.z);
					break;
				}
				sprite.color = ColorManagementSystemController.GetCurrentTextColor();
			}

			Vector3 visiblePos = new Vector3(0f, -480f, 0f);
			Vector3 invisiblePos = new Vector3(0f, -600f, 0f);
			model.FacebookShareConfirmation.transform.localPosition = invisiblePos;
			TweenPosition.Begin(model.FacebookShareConfirmation, 0.25f, visiblePos);
			yield return new WaitForSeconds(0.25f);
			yield return new WaitForSeconds(2.0f); // hold for 2 seconds
			TweenPosition.Begin(model.FacebookShareConfirmation, 0.25f, invisiblePos);
		}
	}
}