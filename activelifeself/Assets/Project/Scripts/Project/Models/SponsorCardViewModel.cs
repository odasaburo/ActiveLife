using UnityEngine;
using System.Collections;

public class SponsorCardViewModel : MonoBehaviour {

	#region Sponsor Card Data Points
	public UITexture sponsorImage;
	public UIButton websiteButton;
	#endregion


	void Start()
	{
	}

	void OnPressedWebsiteButton(GameObject go)
	{
		Application.OpenURL("");
	}
}
