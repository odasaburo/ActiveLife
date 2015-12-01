using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using ITT.System;
using System.Linq;

namespace ITT.Scene
{
	public class FilterResultsViewController : MonoBehaviour, IScreenViewBase
	{
		#region Global Variables
		FilterResultsViewModel model;
		DynamicScrollView2 dynamicScrollView;
		GameObject dynamicScrollViewObject;
		public string LoadingText = "Fetching Results";
		protected bool stopTextAnimation = false;
		private bool _informationIsLoaded = false;
		public ITTFilterRequest _filterRequest;
		#endregion

		#region Unity Methods
		void Awake()
		{
			model = gameObject.GetComponent<FilterResultsViewModel>();
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
			dynamicScrollViewObject = NGUITools.AddChild(gameObject);
			dynamicScrollViewObject.name = "ScrollViewHolder";
			dynamicScrollViewObject.transform.localPosition = new Vector3(0f, -60f, 0f);
			dynamicScrollView = dynamicScrollViewObject.AddComponent<DynamicScrollView2>();

			gameObject.SetActive(true);

			StartCoroutine(PlayLoadingAnimation());
			yield return StartCoroutine(HelperMethods.Instance.AnimateIn(gameObject));

			model.noFilterCard.gameObject.SetActive(false);

			ITTDataCache.Instance.RequestCombinedActivities(OnFilterSuccess, OnFilterFailure, _filterRequest);
		}

		private void Update()
		{
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
					
				}
				ReturnToFilter();
			}
		}
		#endregion

		#region Load Animation
		IEnumerator PlayLoadingAnimation()
		{
			if (_informationIsLoaded == false)
			{
				yield return StartCoroutine (FadeInObject (model.loadingIcon.gameObject, 0.25f));
			}

			while(_informationIsLoaded == false)
			{
				model.loadingIcon.transform.Rotate(Vector3.forward * 360 * Time.deltaTime);
				yield return null;
			}
			
			yield return StartCoroutine(FadeOutObject(model.loadingIcon.gameObject, 0.25f));
		}

		void StopLoadingAnimation()
		{
			_informationIsLoaded = true;
			model.loadingLabel.text = string.Empty;
		}

		IEnumerator FadeInObject(GameObject fadeGameObject, float fadeDuration)
		{
			Debug.Log ("Started Fading In Animation");
			
			TweenAlpha fadeObject = null;
			if(null == (fadeObject = fadeGameObject.GetComponent<TweenAlpha>()))
			{
				fadeObject = fadeGameObject.AddComponent<TweenAlpha>();
			}
			
			if(null != fadeObject)
			{
				fadeObject.from = 0f;
				fadeObject.to = 1f;
				fadeObject.duration = fadeDuration;
				fadeObject.PlayForward();
			}
			yield return new WaitForSeconds(fadeObject.duration);
			Debug.Log ("Fading In Has Finished");
		}
		
		IEnumerator FadeOutObject(GameObject fadeGameObject, float fadeDuration)
		{
			Debug.Log ("Started Fading Out Animation");
			
			TweenAlpha fadeObject = null;
			if(null == (fadeObject = fadeGameObject.GetComponent<TweenAlpha>()))
			{
				fadeObject = fadeGameObject.AddComponent<TweenAlpha>();
			}
			
			if(null != fadeObject)
			{
				fadeObject.from = 0f;
				fadeObject.to = 1f;
				fadeObject.duration = fadeDuration;
				fadeObject.PlayReverse();
			}

			yield return new WaitForSeconds(fadeObject.duration);
			Debug.Log ("Fading Out Has Finished");
		}
		#endregion

		#region Helper Methods
		public IEnumerator OnHide() 
		{
			_informationIsLoaded = false;
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
			model.backButton.onClick.Add	(new EventDelegate(this, "OnBackPressed"	));
		}

		public void OnBackPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Filter - Filter Results Screen")
			                                                     .SetEventAction("Click - Filter Results Back Button")
			                                                     .SetEventLabel("User has clicked on the back button for Filter Results"));
			ReturnToFilter();
		}

		private void ReturnToFilter()
		{
			Destroy(dynamicScrollViewObject);
			model.noFilterCard.SetActive(false);
			ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Filter;
		}
		#endregion

		#region Network handlers
		public void OnFilterSuccess(string json)
		{
			try
			{
				StopLoadingAnimation();

				MasterBaseCardData masterCardData = JsonFx.Json.JsonReader.Deserialize<MasterBaseCardData>(json);
                var activityList = masterCardData.results.Cast<BaseCardData>().ToList();
                activityList.ForEach(activity => activity.ParseDateString());
                var filteredActivities = activityList.Where(card => card.dateTime >= DateTime.Now).ToList();

				if (filteredActivities.Count == 0)
				{
					model.noFilterCard.SetActive(true);
				}
				else
				{
					dynamicScrollView.Init(600, 1000, 8, null, null, 15, true, true);
					filteredActivities.Sort((a, b) => 
					                             {
						int dateDifference = a.dateTime.CompareTo(b.dateTime);
						if (dateDifference == 0)
						{
							return a.Proximity.CompareTo(b.Proximity);
						}
						else
							return dateDifference;
					});
					dynamicScrollView.Populate(filteredActivities);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("FilterResultsViewController.OnFilterSuccess: Deserialization error: \n" + e.Message + "\n" + e.StackTrace);
				model.loadingLabel.text = "An error occurred. Try again later.";
			}
		}

		public void OnFilterFailure(string error)
		{
			Debug.LogError("FilterResultsViewController.OnFilterFailure: " + error);
			if (error.Contains(HelperMethods.Instance.Error_NetworkRadioOff) || error.Contains(HelperMethods.Instance.Error_NetworkTimeOut))
			{
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.", () => {
					ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Main;
				});
			}
			model.loadingLabel.text = "Results could not be loaded. Please try again later.";
			StopLoadingAnimation();
		}

		#endregion
	}
}