using UnityEngine;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using ITT.Scene;
using ITT.System;

public class ActivityDealCardViewModel : MonoBehaviour, IActivityViewModelBase {

	#region Card Data Points
	public UITexture backdropLayer;
	public UITexture primaryImage;
	public UISprite gradientLayer;
	public UILabel companyLabel;
	public UILabel titleLabel;
	public UITexture bottomDetail;
	public UILabel timeDistanceLabel;
	public UILabel costLabel;
	public UISprite categoryIcon;
	public UISprite cardIcon;

	public Int64 nid;
	public BaseCardData data;

	private bool isPopulated = false;

	public enum CardType {NONE = 0, ACTIVITY, SPONSOR };
	private CardType cardType = CardType.NONE;
	public bool IsActivity { get { return cardType == CardType.ACTIVITY; } }

	// HACK: Sponsor members
	public UITexture sponsorImage;
	public string sponsorWebURL;

	public GameObject activityParent;
	public GameObject sponsorParent;
	#endregion

	#region Methods
	void Awake()
	{
		Clear();
	}

	void Start()
	{
	}

	public void ShowSponsors(bool show)
	{
		cardType = (show ? CardType.SPONSOR : CardType.ACTIVITY);
		activityParent.SetActive(!show);
		sponsorParent.SetActive(show);
	}

	public void UpdatePrimaryImage(Texture2D tex)
	{
		if (null != activityParent && activityParent.activeSelf)
		{
			primaryImage.mainTexture = tex;
		}
		else
		{
			sponsorImage.mainTexture = tex;
		}
	}
	public void SetCategoryIcon(string category)
	{
		if (category.Contains("hysic"))
			categoryIcon.spriteName = "Category_PhysicalActivity_Inside";
		else if (category.Contains("ellness"))
			categoryIcon.spriteName = "Category_HealthWellness_Inside";
		else if (category.Contains("ood"))
			categoryIcon.spriteName = "Category_FoodNutrition_Inside";	
	}

	public void SetCardIcon(bool featured)
	{
		if (featured) {
			cardIcon.spriteName = "Activity_Sponsored";
		} else {
			cardIcon.spriteName = "Activity_Normal";
		}

		if (ITTDataCache.Instance.HasSessionCredentials)
			ITTDataCache.Instance.IsFlaggedSaved(data.id, OnItemSaveCheckSuccess, OnItemSaveCheckFailure);
	}

	public bool IsPopulated()
	{
		return isPopulated;
	}

	public void Clear()
	{
		titleLabel.text = "";
		companyLabel.text = "";
		timeDistanceLabel.text = "";
		nid = 0;
		costLabel.text = "";
		categoryIcon.spriteName = "";
		cardIcon.spriteName = "";
		sponsorWebURL = "";

		if (primaryImage.mainTexture != null && (primaryImage.mainTexture.name != "Placeholder_Logo"))
			Destroy(primaryImage.mainTexture);
		if (sponsorImage.mainTexture != null && (sponsorImage.mainTexture.name != "Placeholder_Logo"))
			Destroy(sponsorImage.mainTexture);

		activityParent.SetActive(false);
		sponsorParent.SetActive(false);

		isPopulated = false;
		cardType = CardType.NONE;

		// Change my game object name based on my parent's transform order
		if (null != transform.parent)
		{
			for (int i = 0; i < transform.parent.childCount; i++)
			{
				if (transform == transform.parent.GetChild(i))
				{
					name = "Card_" + i;
					break;
				}
			}
		}
	}

	public void Populate(BaseCardData data, UIEventListener.VoidDelegate onClickedDelegate = null,  bool allowRecommending = false)
	{
		this.data = data;
		ShowSponsors(null != data as SponsorDataModel);

		if (activityParent.activeSelf)
		{

			titleLabel.text = data.title;
			
			global::System.DateTime date = data.ParseDateString();
			companyLabel.text = data.company;
			timeDistanceLabel.text = date.ToString("t", global::System.Globalization.CultureInfo.CreateSpecificCulture("en-us")) + " | " +
					data.Proximity.ToString("#.##") + " miles";
			nid = data.id;
			costLabel.text = (data.admission_adults <= 0) && (data.admission_children <= 0) ? "FREE" : " $ ";
			SetCategoryIcon(data.category);
			SetCardIcon(data.featured);

			if (null != data.image && !string.IsNullOrEmpty(data.image.serving_url))
			{
				data.StartImageDownload(UpdatePrimaryImage, null);
			}
			// Grab a placeholder image during the download.
			primaryImage.mainTexture = PlaceholderImageManager.Instance.GetRandomImage((int)data.id);

			UIEventListener myListener = UIEventListener.Get(gameObject);
			myListener.onClick -= OnSponsorWebsitePressed;
			if (null != onClickedDelegate)
			{
				myListener.onClick -= onClickedDelegate;
				myListener.onClick += onClickedDelegate;
			}
		}
		else
		{
			SponsorDataModel sponsorData = data as SponsorDataModel;
			sponsorWebURL = sponsorData.website;

			if (null != sponsorData.image && !string.IsNullOrEmpty(sponsorData.image.serving_url))
			{
				sponsorData.StartImageDownload(UpdatePrimaryImage, null);

			}
			// Grab a placeholder image during the download.
			sponsorImage.mainTexture = PlaceholderImageManager.Instance.GetRandomImage((int)data.id);

			UIEventListener myListener = UIEventListener.Get(gameObject);
			// Sponsors have a different onClick
			myListener.onClick -= onClickedDelegate;
			myListener.onClick -= OnSponsorWebsitePressed;
			myListener.onClick += OnSponsorWebsitePressed;
		}

		isPopulated = true;
	}

	void OnSponsorWebsitePressed(GameObject go)
	{
		string pattern = "https?://";
		if (string.IsNullOrEmpty(Regex.Match(sponsorWebURL, pattern).ToString()))
		{
			sponsorWebURL = "http://" + sponsorWebURL;
		}
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Detail - Detail Card")
		                                                     .SetEventAction("Click - Sponsor Card")
		                                                     .SetEventLabel("User has tapped a sponsor card. Website: " + sponsorWebURL));
		Application.OpenURL(sponsorWebURL);
	}

	public void OnItemSaveCheckSuccess(string json)
	{
		// Item is saved
		HelperMethods.ResultReponseObject result = JsonFx.Json.JsonReader.Deserialize<HelperMethods.ResultReponseObject>(json);
		if (result.total_count > 0) {
			cardIcon.spriteName = "Activity_Saved";
		}
	}
	
	public void OnItemSaveCheckFailure(string error)
	{
		// Unable to check if the item is saved
		Debug.LogError ("Unable to check if item is saved: " + error);
	}

	#endregion
}
