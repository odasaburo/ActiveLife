using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ITT.System;

namespace ITT.Scene
{
	public class ProfileViewModel : MonoBehaviour 
	{
		#region Common items
		public UISprite screenIcon;
		public UILabel screenLabel;
		public UILabel loadingLabel;
		public UITexture loadingIcon;
		#endregion

		#region Main state items
		public GameObject mainStateParent;
		public GameObject mainScrollListParent;
		public DynamicScrollView2 mainScrollView;
		public UIButton mainBackButton;
		public UIButton settingsButton;
		public UIButton callButton;
		public string phoneNumber;
		public UIButton historyButton;
		public List<BaseCardData> mainCardList = new List<BaseCardData>();
		#endregion

		#region History state items
		public GameObject historyStateParent;
		public GameObject historyScrollListParent;
		public DynamicScrollView2 historyScrollView;
		public UIButton historyBackButton;
		public List<BaseCardData> historyCardList = new List<BaseCardData>();
		#endregion
	}
}