using UnityEngine;
using System.Collections;

public class FilterViewModel : MonoBehaviour 
{
	public UIButton cancelButton;
	public UIButton applyButton;
	public UIToggle activitiesToggle;
	public UIToggle dealsToggle;
	public UIToggle physicalActivityToggle;
	public UIToggle healthWellnessToggle;
	public UIToggle foodNutritionToggle;
	public UIToggle audienceKidsToggle;
	public UIToggle audienceTeensToggle;
	public UIToggle audienceAdultsToggle;
	public UIToggle audienceSeniorsToggle;
	public UIToggle skillBeginnerToggle;
	public UIToggle skillIntermediateToggle;
	public UIToggle skillAdvancedToggle;
	public UIToggle skillExpertToggle;

	public UISlider distanceSlider;
	public UILabel distanceLabel;
	public float distanceMin = 1f;
	public float distanceMax = 24f;

	public UIToggle priceFreeToggle;
	public UIToggle pricePaidToggle;

	void Awake()
	{
		physicalActivityToggle.optionCanBeNone = true;
		healthWellnessToggle.optionCanBeNone = true;
		foodNutritionToggle.optionCanBeNone = true;
	}
}
