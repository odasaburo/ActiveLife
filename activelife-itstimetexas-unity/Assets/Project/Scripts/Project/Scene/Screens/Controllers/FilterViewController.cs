using UnityEngine;
using System.Collections;
using ITT.System;
using System;

namespace ITT.Scene
{
	public class FilterViewController : MonoBehaviour, IScreenViewBase 
	{
		private FilterViewModel model;
		public FilterResultsViewController filterResultsController;
		
		void Awake()
		{
			model = gameObject.GetComponent<FilterViewModel>();
			if (null == model)
			{
				this.enabled = false;
				throw new MissingComponentException();

			}
			else
			{
				ResetFilter();
			}

			if (null == filterResultsController)
			{
				Debug.LogError("FilterViewController: missing results controller reference");
			}
		}
		
		void Start()
		{
			AssignButtonDelegates();
		}

		void ResetFilter()
		{
			model.activitiesToggle.value = false;
			model.dealsToggle.value = false;

			model.physicalActivityToggle.value = false;
			model.healthWellnessToggle.value = false;
			model.foodNutritionToggle.value = false;

			model.audienceAdultsToggle.value = model.audienceKidsToggle.value = model.audienceSeniorsToggle.value = model.audienceTeensToggle.value = false;

			model.skillAdvancedToggle.value = model.skillBeginnerToggle.value = model.skillExpertToggle.value = model.skillIntermediateToggle.value = false;

			model.distanceSlider.value = .2f;
			model.distanceSlider.ForceUpdate();
			DisplayDistance();

			model.priceFreeToggle.value = model.pricePaidToggle.value = false;
		}
		
		public IEnumerator OnDisplay()
		{
			if (null != model)
				ResetFilter();

			gameObject.SetActive(true);
			yield return StartCoroutine(HelperMethods.Instance.AnimateIn(gameObject));
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

		public void AssignButtonDelegates()
		{
			model.cancelButton.onClick.Add	(new EventDelegate(this, "OnCancelPressed"	 ));
			model.applyButton.onClick.Add	(new EventDelegate(this, "OnApplyPressed" 	 ));
		}
		
		public void OnCancelPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Filter - Filter Screen")
			                                                     .SetEventAction("Click - Filter Cancel Button")
			                                                     .SetEventLabel("User has clicked on the cancel button"));
			ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Main;
		}

		public void OnResetButtonPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Filter - Filter Screen")
			                                                     .SetEventAction("Click - Filter Reset Button")
			                                                     .SetEventLabel("User has clicked on the reset button"));
			ResetFilter();
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

				ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Main;
			}
		}

		public int CalculateDistance()
		{
			return (int)(model.distanceMin + (model.distanceMax * model.distanceSlider.value));
		}

		public void DisplayDistance()
		{
			model.distanceLabel.text = "" + CalculateDistance() + " miles";
		}

		public void OnApplyPressed()
		{
			// Item type
			TypeValue typeFilter = new TypeValue(Type_Value.Activities);

			// Category
			Category_Value cv = Category_Value.All;
			int numSet = 0;
			if (model.physicalActivityToggle.value)
			{
				numSet++;
				cv |= Category_Value.Physical_Activities;
			}
			if (model.healthWellnessToggle.value)
			{
				numSet++;
				cv |= Category_Value.Health_Wellness;
			}
			if (model.foodNutritionToggle.value)
			{
				numSet++;
				cv |= Category_Value.Food_Nutrition;
			}
			
			if (numSet >= global::System.Enum.GetNames(typeof(Category_Value)).Length-1)
			{
				cv = Category_Value.All;
			}

			CategoryValue categoryFilter = new CategoryValue(cv);


			// Audience
			Audience_Value av = Audience_Value.All;
			numSet = 0;
			if (model.audienceKidsToggle.value)
			{
				numSet++;
				av |= Audience_Value.Children;
			}
			if (model.audienceTeensToggle.value)
			{
				numSet++;
				av |= Audience_Value.Teenagers;
			}
			if (model.audienceAdultsToggle.value)
			{
				numSet++;
				av |= Audience_Value.Adults;
			}
			if (model.audienceSeniorsToggle.value)
			{
				numSet++;
				av |= Audience_Value.Senior_Citizen;
			}

			if (numSet >= global::System.Enum.GetNames(typeof(Audience_Value)).Length-1)
			{
				av = Audience_Value.All;
			}

			AudienceValue audienceFilter = new AudienceValue(av);


			// Skill
			SkillLevel_Value sv = SkillLevel_Value.All;
			numSet = 0;
			if (model.skillBeginnerToggle.value)
			{
				numSet++;
				sv |= SkillLevel_Value.Beginner_Friendly;
			}
			if (model.skillIntermediateToggle.value)
			{
				numSet++;
				sv |= SkillLevel_Value.Intermediate;
			}
			if (model.skillAdvancedToggle.value)
			{
				numSet++;
				sv |= SkillLevel_Value.Advanced;
			}
			if (model.skillExpertToggle.value)
			{
				numSet++;
				sv |= SkillLevel_Value.Expert;
			}

			if (numSet >= global::System.Enum.GetNames(typeof(SkillLevel_Value)).Length-1)
			{
				sv = SkillLevel_Value.All;
			}

			SkillLevelValue skillFilter = new SkillLevelValue(sv);

			//Distance
			int distance = CalculateDistance();
			DistanceValue distanceFilter = new DistanceValue(distance);

			//Admission
			Admission_Value admissionType = Admission_Value.All;
			if (model.priceFreeToggle.value)
				admissionType = Admission_Value.Free;
			if (model.pricePaidToggle.value)
				admissionType = Admission_Value.Fee;

			if (model.priceFreeToggle.value == model.pricePaidToggle.value)
				admissionType = Admission_Value.All;

			AdmissionValue admissionFilter = new AdmissionValue(admissionType);

			ITTFilterRequest filterRequest = new ITTFilterRequest();
			filterRequest.AddFilterValue(typeFilter);
			filterRequest.AddFilterValue(categoryFilter);
			filterRequest.AddFilterValue(audienceFilter);
			filterRequest.AddFilterValue(skillFilter);
			filterRequest.AddFilterValue(distanceFilter);
			filterRequest.AddFilterValue(admissionFilter);
			filterRequest.AddFilterValue(new MinDateValue(DateTime.Today));

			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Filter - Filter Screen")
			                                                     .SetEventAction("Click - Filter Apply Button")
			                                                     .SetEventLabel("User has clicked on the apply button with the following url values: " + filterRequest.RetrieveFilterURLString()));
			filterResultsController._filterRequest = filterRequest;
			ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.FilterResults;

		}
	}
}