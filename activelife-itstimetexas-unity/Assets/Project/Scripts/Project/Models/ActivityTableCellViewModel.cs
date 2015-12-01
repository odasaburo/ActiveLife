using UnityEngine;
using System.Collections;
using System;
using ITT.Scene;
using ITT.System;

public class ActivityTableCellViewModel : MonoBehaviour, IActivityViewModelBase {
	#region Activity Cell Data Properties
	public UITexture mainImage;

	public UILabel companyLabel;
	public UILabel titleLabel;
	public UISprite activityTypeSprite;

	public UITexture timeDistanceBackdrop;
	public UILabel timeDistanceLabel;
	public UILabel priceLabel;
	public UISprite categorySprite;

	public UIButton detailButton;

	public Int64 nid;
	public BaseCardData data;

	public UIButton recommendButton;
	private UIEventListener recommendButtonObject;
	public GameObject preRecommendRoot;
	public GameObject loadingIndicatorRoot;
	public GameObject loadingIndicator;
	public GameObject postRecommendRoot;

	#endregion

	void Awake()
	{
		preRecommendRoot.SetActive(false);
		loadingIndicatorRoot.SetActive(false);
		postRecommendRoot.SetActive(false);
		recommendButtonObject = UIEventListener.Get(recommendButton.gameObject);
		Clear();
	}

	void Update()
	{
		if (loadingIndicatorRoot.activeSelf)
		{
			loadingIndicator.transform.Rotate(Vector3.forward * 360 * Time.deltaTime);
		}
	}
	
	public bool IsPopulated()
	{
		return !string.IsNullOrEmpty(titleLabel.text);
	}

	public void Clear()
	{
		titleLabel.text = "";
        companyLabel.text = "";
		activityTypeSprite.spriteName = "";
		timeDistanceLabel.text = "";
		priceLabel.text = "";
		categorySprite.spriteName = "";
		nid = 0;
		if (mainImage.mainTexture != null && (mainImage.mainTexture.name != "Placeholder_Logo"))
			Destroy(mainImage.mainTexture);
		preRecommendRoot.SetActive(false);
		loadingIndicatorRoot.SetActive(false);
		postRecommendRoot.SetActive(false);
	}

	public void Populate(BaseCardData data, UIEventListener.VoidDelegate onClickedDelegate = null,  bool allowRecommending = false)
	{
		this.data = data;
		titleLabel.text = data.title;
		
		global::System.DateTime date = data.ParseDateString();
        companyLabel.text = data.company;
		timeDistanceLabel.text =
			date.ToString("t", global::System.Globalization.CultureInfo.CreateSpecificCulture("en-us")) + " | " +
				data.Proximity.ToString("#.##") + " miles";
		
		nid = data.id;

		if (null != onClickedDelegate)
		{
			UIEventListener.Get(gameObject).onClick -= onClickedDelegate;
			UIEventListener.Get(gameObject).onClick += onClickedDelegate;
		}

		// change image to recommend icon for activities only, if they are past
		if (allowRecommending && ITTDataCache.Instance.HasSessionCredentials)
		{
			preRecommendRoot.SetActive(false);
			postRecommendRoot.SetActive(false);
			ITTDataCache.Instance.IsFlaggedRecommended(nid, OnCheckRecommendFlagSuccess, OnCheckRecommendFlagFailure);
		}
		if (null == data.image || string.IsNullOrEmpty(data.image.serving_url))
		{
			// Grab a placeholder image
			mainImage.mainTexture = PlaceholderImageManager.Instance.GetRandomImage((int)data.id);
		}
		else
		{
			data.StartImageDownload(UpdateImage, OnImageImportFailed);
		}

		if (data.category.Contains("hysic"))
			categorySprite.spriteName = "Category_PhysicalActivity_Inside";
		else if (data.category.Contains("ellness"))
			categorySprite.spriteName = "Category_HealthWellness_Inside";
		else if (data.category.Contains("ood"))
			categorySprite.spriteName = "Category_FoodNutrition_Inside";

		priceLabel.text = (data.admission_adults <= 0) && (data.admission_children <= 0)? "FREE" : " $ ";

		if (data.featured) {
			activityTypeSprite.spriteName = "Activity_Sponsored";
		} else {
			activityTypeSprite.spriteName = "Activity_Normal";
		}
	}

	public void SetDidRecommend(bool value)
	{
		if (null == preRecommendRoot || null == postRecommendRoot || null == recommendButton)
			return;

		// Resize and position collider
		BoxCollider col = (BoxCollider)GetComponent<Collider>();
		col.center = new Vector3(80f, 0f, 0f);
		col.size = new Vector3(380f, 160f, 0f);

		preRecommendRoot.SetActive(!value);
		loadingIndicatorRoot.SetActive(false);
		postRecommendRoot.SetActive(value);

		if (value && null != recommendButtonObject.onClick)
			recommendButtonObject.onClick -= OnClickedRecommend;

		if (!value)
			recommendButtonObject.onClick += OnClickedRecommend;
	}

	#region Handlers

	public void UpdateImage(Texture2D tex)
	{
		mainImage.mainTexture = tex;
	}

	public void OnImageImportFailed()
	{
		mainImage.mainTexture = PlaceholderImageManager.Instance.GetRandomImage ((int)this.nid);
	}

	public void OnClickedRecommend(GameObject go)
	{
		loadingIndicatorRoot.SetActive(true);
		
		ITTDataCache.Instance.RetrieveProfileActivityID(nid, OnRetrieveProfileActivitySuccess, OnRetrieveProfileActivityFailure);
	}

	public void OnRetrieveProfileActivitySuccess(string json)
	{
		HelperMethods.ProfileActivityReponseObject profileActivity = JsonFx.Json.JsonReader.Deserialize<HelperMethods.ProfileActivityReponseObject>(json);
		if (null != profileActivity.results && profileActivity.results.Length > 0)
		{
			ITTDataCache.Instance.FlagRecommended(profileActivity.results[0].id, null, null);
			recommendButtonObject.onClick -= OnClickedRecommend;
			SetDidRecommend(true);
		}
	}

	public void OnRetrieveProfileActivityFailure(string error)
	{
		Debug.LogError("ActivityTableCellViewModel.OnRetrieveProfileActivityFailure error: " + error);
	}

	public void OnCheckRecommendFlagSuccess(string json)
	{
		SetDidRecommend(json.Contains("true"));
	}

	public void OnCheckRecommendFlagFailure(string error)
	{
		Debug.LogError("ActivityTableCellViewModel.OnCheckRecommendFlagFailure error: " + error);
		preRecommendRoot.SetActive(false);
		postRecommendRoot.SetActive(false);
	}

	#endregion
}
