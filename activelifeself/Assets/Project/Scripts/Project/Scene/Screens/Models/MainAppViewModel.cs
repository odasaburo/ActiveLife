using UnityEngine;
using System.Collections;

public class MainAppViewModel : MonoBehaviour {

	public UILabel LocationLabel;
	public UIButton profileButton;
	public UIButton filterButton;
	public UIButton locationButton;
	public DynamicScrollView2 DynamicScrollView;
	public UILabel NoActivitiesLabel;

	public GameObject NoActivitiesCard;
	public UILabel NoActivitiesDetailsLabel;
	public string NoActivitiesDefaultText = "Choose Healthier is currently only in Central Texas, but we will be adding new locations in the future! Go to [b][u][url=http://www.chooseHealthier.org]Choosehealthier.org[/url][/u][/b] to request the app for your city.";
	public string NoActivitiesEndOfDayText = "Check tomorrow's schedule for more healthy activities and events near you!";
	public GameObject FacebookShareConfirmation;
}
