using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ITT.System;
using System.Linq;

namespace ITT.Scene
{
	public class LocationViewController : MonoBehaviour, IScreenViewBase 
	{
		#region Global Variables
		public UIScrollView previousLocationView;
		LocationViewModel model;
		LinkedList<string> previousSearches;
		private float _locationCardPadding = -97.0f;
		private float _locationCardStart = 240.0f;
		private LoadingScreenController _loadingScreen;
		private string _searchString;
		private bool _toolTipComplete = false;
		#endregion

		#region Unity Methods
		void Awake()
		{
			model = gameObject.GetComponent<LocationViewModel>();
			previousSearches = new LinkedList<string>();
			if (null == model)
			{
				throw new MissingComponentException();
			}
		}

		void OnEnable()
		{
			if (previousSearches.Count == 0) // if there are no recent searches
				model.recentSearchLabel.color = Color.clear;// don't show label
			else {
				model.recentSearchLabel.color = Color.white;// else if there are, show label

				previousLocationView.RestrictWithinBounds (true);
			}

#if UNITY_ANDROID
			model.clearButton.SetActive(false);
#endif
		}
		
		void Start()
		{
			AssignButtonDelegates();
		}
		#endregion

		#region Helper Methods
		public IEnumerator OnDisplay()
		{
			gameObject.SetActive(true);
			yield return StartCoroutine(HelperMethods.Instance.AnimateIn(gameObject));
		}
		
		public IEnumerator OnHide() 
		{
			HelperMethods.RemoveInteractionBlocker();
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
			model.cancelButton.onClick.Add		(new EventDelegate(this, "OnCancelPressed"	));
			model.useCurrentButton.onClick.Add	(new EventDelegate(this, "OnUseCurrentPressed" 	));
			model.inputBox.onSubmit.Add			(new EventDelegate(this, "OnSubmitPressed" ));
		}
		
		public void OnCancelPressed()
		{
			ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.USER_FLAGS, OnRetrievedUserFlags);
			
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Location - Location Screen")
			                                                     .SetEventAction("Click - Location Cancel Button")
			                                                     .SetEventLabel("The user has pressed on the cancel button in location screen."));
			if (_toolTipComplete)
				ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Main;
			else
				ModalPopupOK.Spawn("You must enter a valid location before continuing.");
		}

		public void OnSubmitPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Location - Location Screen")
			                                                     .SetEventAction("Click - Location Submit Button")
			                                                     .SetEventLabel("The user has pressed on the submit search button in location screen with value: " + model.inputBox.value));
			OnSearch(model.inputBox.value);
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				OnCancelPressed();
			}
		}

		public void OnUseCurrentPressed()
		{
			HelperMethods.AddInteractionBlocker();
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Location - Location Screen")
			                                                     .SetEventAction("Click - Current Location Button")
			                                                     .SetEventLabel("The user has pressed on the current location button in location screen."));

			model.mainAppViewModel.DynamicScrollView.ClearCarousel();
			model.mainAppViewModel.NoActivitiesCard.SetActive(false);

			StartCoroutine(HelperMethods.Instance.RetrieveAndRefreshLocationData(OnFailGeoData));

			if (!_toolTipComplete)
				ToolTipViewController.FlagViewedTooltip();
		}

		void OnRetrievedUserFlags(DataEntryBase data)
		{
			UserFlagsEntry flags = data as UserFlagsEntry;
			if (null == flags)
				return;

			HelperMethods.UserFlags tempFlags = flags.Data;
			if (null == flags.Data)
			{
				tempFlags = new HelperMethods.UserFlags();
				_toolTipComplete = tempFlags.hasSeenTooltip = false;	
				ITTDataCache.Instance.Data.UpdateDataEntry((int)DataCacheIndices.USER_FLAGS, tempFlags, false);
			}
			else
			{
				if (tempFlags.hasSeenTooltip == true)
					_toolTipComplete = true;
			}
		}

		#endregion

		#region Searching Behavior
		public void OnSearch(string searchString)
		{
			if (string.IsNullOrEmpty(searchString)) {
				return;
			}

			const string _loadingScreenPrefabPath = "Prefabs/Production/LoadingPanel";
			var go = NGUITools.AddChild(gameObject, (GameObject)Resources.Load(_loadingScreenPrefabPath));
			_loadingScreen = go.GetComponent<LoadingScreenController> ();
			_loadingScreen._loadingLabel.text = "Searching...";
			_searchString = searchString;

			HelperMethods.AddInteractionBlocker();

			ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.USER_FLAGS, HelperMethods.Instance.UnFlagCurrentLocation);
			ITTDataCache.Instance.GetGeoLocationData(searchString, OnUpdateGeoLocation, OnFailGeoData);
			model.inputBox.value = null;
		}

		private void OnUpdateGeoLocation(string geoLocationData)
		{
			LocationDataModel searchLocation = JsonFx.Json.JsonReader.Deserialize<LocationDataModel>(geoLocationData);
			if (null == searchLocation) {
				OnFailGeoData (geoLocationData);
				return;
			} 

			previousSearches.Remove (_searchString);
			previousSearches.AddFirst (_searchString);
			UpdateLocationCards();

			if (_searchString.All(c => c >= '0' && c <= '9'))
			{
				searchLocation.zipCode = _searchString;
			}
			else 
			{
				searchLocation.cityState = _searchString;
			}

			ITTDataCache.Instance.Data.UpdateDataEntry((int)DataCacheIndices.LOCATION, searchLocation, false);

			if (!_toolTipComplete)
				ToolTipViewController.FlagViewedTooltip();

			HelperMethods.RemoveInteractionBlocker();

			// TODO: clear dynamic scroll view
			model.mainAppViewModel.DynamicScrollView.ClearCarousel();
			model.mainAppViewModel.NoActivitiesCard.SetActive(false);
			
			ITTDataCache.Instance.Data.ClearDataEntry((int)DataCacheIndices.ACTIVITY_LIST);
			ITTDataCache.Instance.Data.RefreshDataEntry((int)DataCacheIndices.ACTIVITY_LIST);

			ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Main;
			GameObject.Destroy (_loadingScreen.gameObject);
		}

		private void OnFailGeoData(string error)
		{
            if (error.Contains(HelperMethods.Instance.Error_NetworkRadioOff) || error.Contains(HelperMethods.Instance.Error_NetworkTimeOut))
                ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.");
            else
                ModalPopupOK.Spawn("Your location could not be found. Please try again.");

			HelperMethods.RemoveInteractionBlocker();
            GameObject.Destroy(_loadingScreen.gameObject);
		}

		private void UpdateLocationCards()
		{
			int currentIndex = 0;
			CleanUpPreviousCards();
			foreach (string location in previousSearches) {

				GameObject locationCard = (GameObject)Resources.Load("Prefabs/Production/PreviousLocationCard");

				GameObject childLocation = NGUITools.AddChild(previousLocationView.transform.gameObject, locationCard);
				NGUITools.AddWidgetCollider(childLocation);
				UIDragScrollView dragScrollView = childLocation.AddComponent<UIDragScrollView>();
				dragScrollView.scrollView = previousLocationView;

				childLocation.transform.localPosition = new Vector3(3.0f, _locationCardStart + (_locationCardPadding * currentIndex++), 0.0f);
				var plvc = childLocation.GetComponent<PreviousLocationViewController>();
				plvc._previousLocationLabel.text = location;
				plvc._locationView = this;
			}
		}

		private void CleanUpPreviousCards()
		{
			// Clean up previous cards
			List<GameObject> currentCardsList = new List<GameObject>();
			foreach (Transform child in previousLocationView.transform)
			{
				currentCardsList.Add(child.gameObject);
			}
			
			if (currentCardsList.Count > 0)
			{
				foreach (GameObject child in currentCardsList)
				{
					NGUITools.Destroy(child);
				}
			}
		}
		#endregion
	}
}