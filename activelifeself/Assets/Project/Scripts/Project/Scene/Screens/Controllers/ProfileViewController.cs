using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ITT.System;
using System;

namespace ITT.Scene
{
	public class ProfileViewController : System.ITTStateMachine, IScreenViewBase 
	{
		ProfileViewModel model;

		public enum ProfileStates
		{
			Main = 0,
			History = 1
		}

		void Awake()
		{
			model = gameObject.GetComponent<ProfileViewModel>();
			if (null == model)
			{
				throw new MissingComponentException();
			}
		}

		#region State machine methods
		override protected void Update() 
		{
			base.Update();

			if (Input.GetKeyDown(KeyCode.Escape))
			{

				DynamicScrollView2 scrollView = FindObjectOfType<DynamicScrollView2>();
				if (null != scrollView)
				{
					if (scrollView.IsDetailedCardActive)
					{
						scrollView.CloseDetailCard();
						return;
					}
					else if(model.mainStateParent.GetComponent<UIWidget>().alpha != 1) // checks to see if the main state or history state for the profile view is seen 
					{
						OnHistoryBackPressed();
					}
					else
					{
						ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Main;
					}
				}

			}

			model.loadingIcon.transform.Rotate(Vector3.forward * 360 * Time.deltaTime);
			
			if(model.historyScrollView != null && model.historyScrollView.transform.localScale != Vector3.one) 
				model.historyScrollView.transform.localScale = Vector3.one;
		}

		override protected void LateUpdate() 
		{
			base.LateUpdate();
		}

		IEnumerator Main_EnterState()
		{
			model.mainStateParent.SetActive(true);
			model.screenLabel.text = "Profile";
			model.screenIcon.spriteName = "Profile";
			yield break;
		}

		IEnumerator Main_ExitState()
		{
			model.mainStateParent.SetActive(false);
			yield break;
		}

		IEnumerator History_EnterState()
		{
			model.historyStateParent.SetActive(true);
			model.screenLabel.text = "History";
			model.screenIcon.spriteName = "Activity_Pasty";
			if (null != model.historyScrollView)
			{
				UIScrollView historyScrollView = model.historyScrollView.GetComponentInChildren<UIScrollView>();
				if (null != historyScrollView)
					historyScrollView.ResetPosition();
			}
			yield break;
		}

		IEnumerator History_ExitState()
		{
			model.historyStateParent.SetActive(false);
			yield break;
		}

		#endregion


		#region ScreenViewBase methods
		void Start()
		{
			AssignButtonDelegates();
		}

		public IEnumerator OnDisplay()
		{
			if (null == currentState)
			{
				base.OnAwake();
			}
			gameObject.SetActive(true);
			yield return StartCoroutine(HelperMethods.Instance.AnimateIn(gameObject));
			currentState = ProfileStates.Main;

			model.historyScrollListParent = NGUITools.AddChild(gameObject);
			model.historyScrollListParent.name = "HistoryScrollViewHolder";
			model.historyScrollListParent.AddComponent<UIPanel>();
			model.historyScrollView = model.historyScrollListParent.AddComponent<DynamicScrollView2>();

			
			model.mainScrollListParent = NGUITools.AddChild(gameObject);
			model.mainScrollListParent.name = "MainScrollViewHolder";
			model.mainScrollListParent.AddComponent<UIPanel>();
			model.mainScrollView = model.mainScrollListParent.AddComponent<DynamicScrollView2>();
			
			model.historyScrollView.Init(600, 1000, 8, null, null, 15, true, true, true);
			model.historyScrollView.gameObject.transform.localPosition = new Vector3(0f, -65f, 0f);
			model.mainScrollView.Init(600, 692, 8, null, null, 15, true, true);
			model.mainScrollView.gameObject.transform.localPosition = new Vector3(0f, -205f, 0f);

			model.historyScrollListParent.transform.parent = model.historyStateParent.transform;
			model.mainScrollListParent.transform.parent = model.mainStateParent.transform;
			
			model.mainScrollListParent.transform.localPosition = new Vector3(0f, -218f, 0f);

			model.loadingLabel.gameObject.SetActive (false);
			model.loadingIcon.gameObject.SetActive (true);

			model.historyScrollView.HideCarousel(true);
			model.mainScrollView.HideCarousel(true);

			// Pull saved activities
			ITTDataCache.Instance.RetrieveUserSavedItems(OnSavedListSuccess, OnSavedListFailure);
			yield break;
		}

		public IEnumerator OnHide() 
		{
			// Delete all lists
			Destroy(model.historyScrollListParent);
			Destroy(model.mainScrollListParent);

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
			// Main
			model.mainBackButton.onClick.Add	(new EventDelegate(this, "OnMainBackPressed"	 ));
			model.settingsButton.onClick.Add	(new EventDelegate(this, "OnSettingsPressed" 	 ));
			model.callButton.onClick.Add		(new EventDelegate(this, "OnCallPressed" 	 	 ));
			model.historyButton.onClick.Add		(new EventDelegate(this, "OnHistoryPressed"  	 ));

			// History
			model.historyBackButton.onClick.Add	(new EventDelegate(this, "OnHistoryBackPressed"	 ));
		}

		#endregion

		#region Data methods
		private void OnSavedListSuccess(string json)
		{
			List<UserSavedActivityType> rawList = null;
			try
			{
				UserSavedActivityData userSavedActivityData = JsonFx.Json.JsonReader.Deserialize<UserSavedActivityData>(json);
				UserSavedActivityType[] tempRawList = userSavedActivityData.results;

				if (tempRawList.Length == 0)
				{
					model.loadingLabel.gameObject.SetActive (true);
					model.loadingLabel.text = "No saved items.";
					model.loadingIcon.gameObject.SetActive(false);

					model.historyScrollView.HideCarousel(true);
					model.mainScrollView.HideCarousel(true);

				}
				else
				{
					model.historyCardList.Clear();
					model.mainCardList.Clear();

					model.historyScrollView.HideCarousel(false);
					model.mainScrollView.HideCarousel(false);

					if (tempRawList.Length > 0) {
						rawList = tempRawList.ToList();
						rawList.RemoveAll(x => null == x.activity); // if prior saved activities were since deleted, skip them.
						rawList.ForEach(x => SortPredicate(x) );
					}

					// Sort lists into proper order by date
					model.historyCardList.Sort((a, b) => b.dateTime.CompareTo(a.dateTime));
					model.mainCardList.Sort((a, b) => a.dateTime.CompareTo(b.dateTime));

					model.historyScrollView.Populate(model.historyCardList);
					model.mainScrollView.Populate(model.mainCardList);

					model.loadingIcon.gameObject.SetActive(false);
					model.loadingLabel.gameObject.SetActive (false);
				}

			}
			catch (Exception e)
			{
				Debug.LogError("ProfileViewController.OnReceivedList: error = " + e.Message + "\n" + e.StackTrace);
				model.loadingIcon.gameObject.SetActive(false);
				model.loadingLabel.gameObject.SetActive(true);
				model.loadingLabel.text = "Error retrieving data. Please try again later.";
			}
		}

		private void SortPredicate(UserSavedActivityType x)
		{
			if (null != x && null != x.activity)
			{
				x.activity.ParseDateString();
				
				// If we have a valid date, continue populating
				if (x.activity.dateTime != global::System.DateTime.MinValue)
				{
					global::System.DateTime now = global::System.DateTime.Now;
					global::System.DateTime activityLocalTime = x.activity.dateTime.ToLocalTime();
					global::System.TimeSpan span = now.Subtract(activityLocalTime);
					if (activityLocalTime < now)
					{
						// Past list
						model.historyCardList.Add(x.activity);
					}
					else
					{
						// Future list
						model.mainCardList.Add(x.activity);
					}
				}
			}
						
		}

		private void OnSavedListFailure(string error)
		{
			Debug.LogError("Failed saved list: " + error);
			model.loadingLabel.text = "Error retrieving data. Please try again later.";
			model.loadingIcon.gameObject.SetActive(false);
			if (error.Contains(HelperMethods.Instance.Error_NetworkRadioOff) || error.Contains(HelperMethods.Instance.Error_NetworkTimeOut))
			{
				
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.", () => {
					ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Main;
				});
			}
		}

		#endregion

		#region Main state methods

		public void OnMainBackPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Profile Screen")
			                                                     .SetEventAction("Click - Main Profile Back Button")
			                                                     .SetEventLabel("User has clicked on the back button from the main profile screen"));
			ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Main;
		}


		public void OnSettingsPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Profile Screen")
			                                                     .SetEventAction("Click - Settings Button")
			                                                     .SetEventLabel("User has clicked on the settings button"));
			ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Settings;
		}

		public void OnCallPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Profile Screen")
			                                                     .SetEventAction("Click - Call Button")
			                                                     .SetEventLabel("User has clicked on the call coach button"));
			HelperMethods.Instance.FormatAndCallNumber(model.phoneNumber);
		}

		public void OnHistoryPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Profile Screen")
			                                                     .SetEventAction("Click - History Button")
			                                                     .SetEventLabel("User has clicked on the history button"));
			currentState = ProfileStates.History;
			StartCoroutine(HelperMethods.Instance.AnimateIn(model.historyStateParent.gameObject));
			StartCoroutine(HelperMethods.Instance.AnimateOut(model.mainStateParent.gameObject));
		}
		#endregion

		#region History state methods
		
		public void OnHistoryBackPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - History Screen")
			                                                     .SetEventAction("Click - Back Button")
			                                                     .SetEventLabel("User has clicked on the back button from this history screen."));
			currentState = ProfileStates.Main;
			StartCoroutine(HelperMethods.Instance.ForcedReverseIn(model.mainStateParent.gameObject));
			StartCoroutine(HelperMethods.Instance.ForcedReverseOut(model.historyStateParent.gameObject));
		}

		#endregion


	}
}