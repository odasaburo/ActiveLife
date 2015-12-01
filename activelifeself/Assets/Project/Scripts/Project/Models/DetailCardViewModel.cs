using UnityEngine;
using System.Collections;
using System;
using ITT.System;
using ITT.Scene;
using Facebook.Unity;

public class DetailCardViewModel : MonoBehaviour {
	#region Card Detail Data Properties
	public UIWidget mainImageContainer;
	public UITexture mainImage;

	public UILabel titleLabel;
	public UILabel companyLabel;
	public UILabel dateLabel;
	public UILabel timeLabel;

	public UILabel recommendedCountLabel;
	public UIButton recommendButton;

	public UIButton shareButton;

	public UISprite categoryIcon;
	public UILabel categoryName;

	public UILabel detailLabel;
	public UILabel suggestedAudienceTextLabel;
	public UILabel suggestedSkillTextLabel;

	public UILabel adultPriceTextLabel;
	public UILabel childPriceTextLabel;
	public UILabel cardDiscountTextLabel;

	public UILabel locationNameTextLabel;
	public UILabel locationAddressTextLabel;

	public UITexture mapLocationImage;
	public UIButton mapButton;

	public UILabel phoneNumberTextLabel;
	public UIButton phoneNumberButton;

	public UILabel websiteLabel;
	public UIButton websiteButton;
	
	public GameObject saveButtonPivot;
	private bool _hasBeenOrCurrentlyRotated = false;
	public UIButton saveToProfileButton;
	public UIButton savedToProfileButton;

	public UIScrollView scrollView;
	public UISprite releaseToCloseSprite;

	public GameObject detailCardInformation;
	public UIWidget loadingIcon;
	
	public delegate void NetworkFailed();
	public NetworkFailed onNetworkFailed;
	#endregion

	#region Data
	public Int64 Nid { get; set; }
	public string Website { get; set; }
	public string imageServingURL;
	private double latitude, longitude;
	#endregion
	
	#region MethodsSaved

	void Start()
	{
		UIEventListener.Get(shareButton.gameObject).onClick += OnSharePressed;
	}

	void OnDestroy(){

		if (mainImage.mainTexture != null && (mainImage.mainTexture.name != "Placeholder_Logo")) {
			Destroy (mainImage.mainTexture);
		}
	}

	public void SetTexture(UITexture txt){

		if (null == txt.mainTexture) {
			mainImage.mainTexture = PlaceholderImageManager.Instance.GetRandomImage ((int)Nid) as Texture2D;
		} else {
			mainImage.mainTexture = Instantiate (txt.mainTexture) as Texture2D;
		}
		mainImage.transform.position = txt.transform.position;
		mainImage.transform.localScale = new Vector3 ((txt.localSize.x / Screen.height),  // active screen width
		                                              (txt.localSize.y / Screen.height),  // active screen height
		                                              (0f)
			);
	}
	
	public void SetBaseCardData(BaseCardData ddm)
	{
		ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Detail Card");
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Detail - Detail Card")
		                                                     .SetEventAction("Detail Clicked - Detail Card")
		                                                     .SetEventLabel("Detail Card clicked Nid: " + ddm.id + " Name: " + ddm.title));

		if (Application.platform == RuntimePlatform.Android)
			RecommendCountByDefault();

		ITTDataCache.Instance.GetRecommendedFlagCount(Nid, OnRecommendCountReceived, OnRecommendCountFailure);
		ITTDataCache.Instance.IsFlaggedSaved(Nid, OnItemSaveCheckSuccess, OnItemSaveCheckFailure);

		titleLabel.text = string.IsNullOrEmpty(ddm.title) ? "N/A" : ddm.title;
		categoryName.text = string.IsNullOrEmpty(ddm.category) ? "N/A" : ddm.category;
		if (null != ddm.image && !string.IsNullOrEmpty(ddm.image.serving_url))
			imageServingURL = ddm.image.serving_url;
		NGUITools.SetActive(categoryIcon.gameObject, true);
		if (categoryName.text.Contains("hysic"))
			categoryIcon.spriteName = "Category_PhysicalActivity_Inside";
		else if (categoryName.text.Contains("ellness"))
			categoryIcon.spriteName = "Category_HealthWellness_Inside";
		else if (categoryName.text.Contains("ood"))
			categoryIcon.spriteName = "Category_FoodNutrition_Inside";

		detailLabel.text = HelperMethods.Instance.StripHtmlTags(ddm.Description);
		if (string.IsNullOrEmpty(detailLabel.text))
			detailLabel.text = "N/A";

		companyLabel.text = ddm.company;

		DateTime localDate = ddm.ParseDateString();
		if (null != localDate && DateTime.MinValue != localDate)
		{
			dateLabel.text = localDate.DayOfWeek.ToString() + " " +  localDate.Month + "/" + localDate.Day;
			timeLabel.text = localDate.ToString("t", global::System.Globalization.CultureInfo.CreateSpecificCulture("en-us"));
		}

		if(string.IsNullOrEmpty(ddm.phone)) {
			phoneNumberButton.GetComponent<UIWidget>().bottomAnchor.absolute = 0;
			NGUITools.SetActiveChildren(phoneNumberButton.gameObject, false);
		} else {
			phoneNumberTextLabel.text = ddm.phone;
			UIEventListener.Get(phoneNumberButton.gameObject).onClick += OnPhoneButtonPressed;
		}

		if(string.IsNullOrEmpty(ddm.website)) {
			Website = "";
			websiteLabel.text = "No Website";
		} else {
			Website = ddm.website;
			UIEventListener.Get(websiteButton.gameObject).onClick += OnWebsiteButtonPressed;
		}

		locationNameTextLabel.text = string.IsNullOrEmpty(ddm.company) ? "N/A" : ddm.company;
		locationAddressTextLabel.text = string.IsNullOrEmpty(ddm.address.address) ? "N/A" : ddm.address.address;

		latitude = ddm.address.lat;
		longitude = ddm.address.lon;
		UIEventListener.Get(mapButton.gameObject).onClick += OnMapButtonPressed;
		ITTDataCache.Instance.GetStaticMapImage(locationAddressTextLabel.text, OnStaticMapImageReceived, OnStaticMapImageFailure);

		suggestedAudienceTextLabel.text = GenerateAudienceLabel(ddm);
		suggestedSkillTextLabel.text = GenerateSkillLabel(ddm);

		if (ddm.admission_adults > 0)
		{
			string price = ddm.admission_adults.ToString();
			string currencySymbol = !price.StartsWith("$") ? "$" : "";
			adultPriceTextLabel.text = currencySymbol + price;
		}
		else
		{
			adultPriceTextLabel.text = (ddm.audience_adults) ? "FREE" : "N/A";
		}

		if (ddm.admission_children > 0)
		{
			string price = ddm.admission_children.ToString();
			string currencySymbol = !price.StartsWith("$") ? "$" : "";
			childPriceTextLabel.text = currencySymbol + price;
		}
		else
		{
			childPriceTextLabel.text = (ddm.audience_kids) ? "FREE" : "N/A";
		}

		if (ddm.card_discount > 0)
			cardDiscountTextLabel.text = ddm.card_discount.ToString() + "%";
		else 
			cardDiscountTextLabel.text = "N/A";
	}


	private void OnSharePressed(GameObject go)
	{
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Facebook - Detail Card")
		                                                     .SetEventAction("Click - Facebook Share Button")
		                                                     .SetEventLabel("User is trying to share activity Nid: " + ((Nid > 0) ? Nid.ToString() : "")));
		if (!FacebookWrapper.IsInitialized)
			return;

		if (!FacebookWrapper.IsLoggedIn)
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Facebook - Facebook Login Flow")
			                                                     .SetEventAction("Login - Facebook Login Flow")
			                                                     .SetEventLabel("Prompting user to log into Facebook"));
			StartCoroutine(LogIntoFacebookAndShare());
		}
		else
		{
			ShareToFacebook();
		}
	}

	private IEnumerator LogIntoFacebookAndShare()
	{
		yield return StartCoroutine(FacebookWrapper.Login());

		if (FacebookWrapper.IsLoggedIn)
		{
			ShareToFacebook();
		}
	}

	private void ShareToFacebook()
	{
		
		//I found <insert activity name here> on the Choose Healther app. See what you can find to make a healthier choice! #ichoosehealthier <link to choosehealthier.org>

		string message = "I found " + titleLabel.text + " on the Choose Healthier app. See what you can find to make a healthier choice! #ichoosehealthier";

		/*FB.ShareLink(
			contentURL: "http://choosehealthier.org",
			contentTitle: titleLabel.text,
			contentDescription: "Hundreds of Businesses. Thousands of Healthy Activities. One FREE app.",
			photoURL: imageServingURL,
			callback: OnShareSuccess
			);*/
	}
	#endregion
	#region Network response handlers 

	private void OnShareSuccess(IGraphResult result)
	{
		MainAppViewController mainAppView = ITTMainSceneManager.Instance.CurrentScreen as MainAppViewController;

		if (null != mainAppView)
		{
			if (!string.IsNullOrEmpty(result.Error)) 
			{
				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Facebook - Facebook Share")
				                                                     .SetEventAction("Failed - Facebook Share Flow")
				                                                     .SetEventLabel("User has failed to share a new post on Facebook: " + result.Error));
				Debug.LogError("Share to FB error: " + result.Error);
				mainAppView.OnFacebookSharePressed("FAILED");
			}
			else if (result.ToString() == "{\"cancelled\":true}")
			{
				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Facebook - Facebook Share")
				                                                     .SetEventAction("Cancelled - Facebook Share Flow")
				                                                     .SetEventLabel("User has cancelled their attempt to share on Facebook."));
				mainAppView.OnFacebookSharePressed("CANCELLED");
			}
			else
			{
				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Facebook - Facebook Share")
				                                                     .SetEventAction("Share - Facebook Share Flow")
				                                                     .SetEventLabel("User has shared Nid " + Nid + " to Facebook"));
				mainAppView.OnFacebookSharePressed("SUCCESS");
			}
		}

	}

	public void OnStaticMapImageReceived(byte[] bytes)
	{
		try
		{
			Texture2D staticMap = new Texture2D(2, 2);
			staticMap.LoadImage(bytes);
			mapLocationImage.mainTexture = staticMap;
		}
		catch
		{
			Debug.LogError("DetailCardViewModel.OnStaticMapImageReceived: error loading texture (corrupt download?)");
		}

	}

	public void OnStaticMapImageFailure(string error)
	{
		Debug.LogError("DetailCardViewModel.OnStaticMapImageFailure: " + error);
	}

	private class CountHelper
	{
		public int total_count {get; set;}
	}

	public void OnRecommendCountReceived(string json)
	{
		UIEventListener.Get(recommendButton.gameObject).onClick = OnRecommendPressed;

		try 
		{
			CountHelper obj = JsonFx.Json.JsonReader.Deserialize<CountHelper>(json);
			recommendedCountLabel.text = obj.total_count.ToString();
		}
		catch
		{
			Debug.LogError("DetailCardViewModel.OnRecommendCountReceived: invalid JSON format");
		}
	}

	public void OnRecommendCountFailure(string error)
	{
		UIEventListener.Get(recommendButton.gameObject).onClick = OnRecommendPressed;
		if (error.Contains("406"))
		{
			recommendedCountLabel.text = "0";
		}
		else
		{
			Debug.LogError("DetailCardViewModel Error: " + error);
		}
	}

	public void OnItemSaveCheckSuccess(string json)
	{
		HelperMethods.ResultReponseObject result = JsonFx.Json.JsonReader.Deserialize<HelperMethods.ResultReponseObject>(json);
		if (result.total_count == 0)
		{
			UIEventListener.Get(saveToProfileButton.gameObject).onClick += OnSaveToProfilePressed;
		}
		else
		{
			if(saveButtonPivot.transform.eulerAngles != Vector3.right * 90)
			{
				saveButtonPivot.transform.eulerAngles = Vector3.right * 90;
			}

			if(false == _hasBeenOrCurrentlyRotated)
			{
				_hasBeenOrCurrentlyRotated = true;
			}
		}
	}

	public void OnItemSaveCheckFailure(string error)
	{
		// Item is not saved.
		UIEventListener.Get(saveToProfileButton.gameObject).onClick += OnSaveToProfilePressed;
	}
	#endregion

	#region Button handlers

	public void OnRecommendPressed(GameObject go)
	{
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Detail - Detail Card")
		                                                     .SetEventAction("Click - Recommend Button")
		                                                     .SetEventLabel("User has clicked on the recommended button for Nid: " + Nid.ToString()));
		ITTDataCache.Instance.IsFlaggedRecommended(Nid, OnFlagStatusReturned, null);
		UIEventListener.Get(recommendButton.gameObject).onClick -= OnRecommendPressed;
	}

	public void OnFlagStatusReturned(string json)
	{
		// Only allow recommendation if they haven't recommended it
		HelperMethods.ResultReponseObject result = JsonFx.Json.JsonReader.Deserialize<HelperMethods.ResultReponseObject>(json);
		if (result.total_count == 0) {
			ITTDataCache.Instance.RetrieveProfileActivityID (this.Nid, OnRetrieveProfileActivitySuccess, OnRetrieveProfileActivityFailure);
		}
	}

	public void OnRetrieveProfileActivitySuccess(string json)
	{
		HelperMethods.ProfileActivityReponseObject profileActivity = JsonFx.Json.JsonReader.Deserialize<HelperMethods.ProfileActivityReponseObject>(json);
		if (null != profileActivity.results && profileActivity.results.Length > 0)
		{
			ITTDataCache.Instance.FlagRecommended(profileActivity.results[0].id, null, null);
			int recommendCount = global::System.Convert.ToInt32(recommendedCountLabel.text) + 1;
			recommendedCountLabel.text = recommendCount.ToString();
		}
	}
	
	public void OnRetrieveProfileActivityFailure(string error)
	{
		Debug.LogError("DetailCardViewModel.OnRetrieveProfileActivityFailure error: " + error);
	}
	
	public void OnMapButtonPressed(GameObject obj)
	{
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Detail - Detail Card")
		                                                     .SetEventAction("Click - Map Button")
		                                                     .SetEventLabel("User has clicked on the map button for Nid: " + Nid.ToString()));
		string addressLocation = ("N/A" == locationAddressTextLabel.text) ? latitude + ", " + longitude : locationAddressTextLabel.text;
		HelperMethods.Instance.OpenMap(addressLocation);
	}

	public void OnPhoneButtonPressed(GameObject obj)
	{
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Detail - Detail Card")
		                                                     .SetEventAction("Click - Phone Button")
		                                                     .SetEventLabel("User has clicked on the phone button for Nid: " + Nid.ToString()));
		HelperMethods.Instance.FormatAndCallNumber(phoneNumberTextLabel.text);

	}

	public void OnWebsiteButtonPressed(GameObject go)
	{
		if (!Website.ToLower().Contains("http://"))
			Website = "http://" + Website;
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Detail - Detail Card")
		                                                     .SetEventAction("Click - Website Button")
		                                                     .SetEventLabel("User has clicked on the website button for Nid: " + Nid.ToString() + "url: " + Website));
		Application.OpenURL(Website);
	}

	public void OnSaveToProfilePressed(GameObject go)
	{
		if (!ITTDataCache.Instance.HasNetworkConnection)
		{
			ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.");
			return;
		}

		if(false == _hasBeenOrCurrentlyRotated) 
		{
			StartCoroutine(RotateEffect());
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Detail - Detail Card")
			                                                     .SetEventAction("Click - Save to Profile Button")
			                                                     .SetEventLabel("User has clicked on the Save to Profile button for Nid: " + Nid.ToString()));
			ITTDataCache.Instance.FlagSaved(Nid, null, null);
		}
	}

	IEnumerator RotateEffect()
	{
		if(null != saveButtonPivot)
		{
			if(false == _hasBeenOrCurrentlyRotated)
			{
				_hasBeenOrCurrentlyRotated = true;
			}

			while(saveButtonPivot.transform.eulerAngles.x < 90)
			{
				saveButtonPivot.transform.Rotate(Vector3.right * 180 * Time.deltaTime);
				yield return null;
			}

			if(saveButtonPivot.transform.eulerAngles.x >= 90)
			{
				saveButtonPivot.transform.eulerAngles = Vector3.right * 90;
			}
		}
	}

	#endregion

	#region Helpers

	private string GenerateAudienceLabel(BaseCardData ddm)
	{
		string str = null;
		if (ddm.audience_kids)
		{
			str += (str != null) ? ", " : "";
			str += HelperMethods.Instance.StringTable["audience_kids"];
		}
		if (ddm.audience_teenagers)
		{
			str += (str != null) ? ", " : "";
			str += HelperMethods.Instance.StringTable["audience_teenagers"];
		}
		if (ddm.audience_adults)
		{
			str += (str != null) ? ", " : "";
			str += HelperMethods.Instance.StringTable["audience_adults"];
		}
		if (ddm.audience_seniors)
		{
			str += (str != null) ? ", " : "";
			str += HelperMethods.Instance.StringTable["audience_seniors"];
		}
		return str;
	}
	
	private string GenerateSkillLabel(BaseCardData ddm)
	{
		string str = null;
		if (ddm.level_beginner)
		{
			str += (str != null) ? ", " : "";
			str += HelperMethods.Instance.StringTable["level_beginner"];
		}
		if (ddm.level_intermediate)
		{
			str += (str != null) ? ", " : "";
			str += HelperMethods.Instance.StringTable["level_intermediate"];
		}
		if (ddm.level_advanced)
		{
			str += (str != null) ? ", " : "";
			str += HelperMethods.Instance.StringTable["level_advanced"];
		}
		if (ddm.level_expert)
		{
			str += (str != null) ? ", " : "";
			str += HelperMethods.Instance.StringTable["level_expert"];
		}
		return str;
	}

	#endregion
	

	#region Andriod Get Recommendation fix

	private void RecommendCountByDefault()
	{
		UIEventListener.Get(recommendButton.gameObject).onClick = OnRecommendPressed;

		recommendedCountLabel.text = "0";
	}

	#endregion
}
