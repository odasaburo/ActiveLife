using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ITT.Scene
{
	public class CardViewDataModel
	{
		public DateTime date;
		public void ParseDateString(string dateString)
		{	
			if (string.IsNullOrEmpty(dateString))
				return;

			string[] subs = dateString.Split(',');
			List<string> temp = new List<string>();
			foreach (string sub in subs)
			{
				string newstr = sub.Substring(0, sub.Length - 6);
				temp.Add(newstr);
			}

			try
			{
				date = DateTime.Parse(temp[0]);
			}
			catch (Exception e)
			{
				Debug.LogError("Error parsing date: " + e.Message);
			}
		}
		public string yearLabel;
		public string titleLabel;
		public string timeDistanceLabel;
		public string costLabel;
		public string prefabName;
		public Texture2D primaryImage;

		protected Int64 nid;
		public Int64 Nid
		{
			get { return nid; }
			private set { nid = value; }
		}

		public delegate void ImageImportCallback(Texture2D myTex);
		public ImageImportCallback OnImageImported;

		public virtual void Populate(ITT.System.BaseCardData data) {}
		public virtual void ImportImage(Texture2D tex) 
		{
			if (null == tex)
			{
				Debug.LogError("ImportImage failure for card " + titleLabel);
			}
			else
			{
				primaryImage = tex;
			}
		}

		public virtual void ImportImageFail(string errorMessage) {
			Debug.LogError(errorMessage);
		}
	}


	public class ActivityDealViewDataModel : CardViewDataModel
	{
		public override void Populate (ITT.System.BaseCardData data)
		{
			ITT.System.ActivityDataModel a = (ITT.System.ActivityDataModel)data;
			if (null == a)
			{
				Debug.LogError("Cannot populate: invalid BaseData type");
				return;
			}

			ParseDateString(a.RetrieveEventDate());
			yearLabel = date.Year.ToString();
			timeDistanceLabel = date.ToString("t", global::System.Globalization.CultureInfo.CreateSpecificCulture("en-us")) + " | " + a.Proximity.ToString("#.##") + " miles";
			titleLabel = global::System.Web.HttpUtility.HtmlDecode(a.title);
			prefabName = "ActivityDealCard";

			if (!string.IsNullOrEmpty(a.image.serving_url))
			{
				string imageUrl = ITT.System.ITTNetworkManager.SanitizeImageURL(a.image.serving_url);
				if (!string.IsNullOrEmpty(imageUrl))
					ITT.System.ITTDataCache.Instance.StartDownloadImage(imageUrl, ImportImage, ImportImageFail);
			}

			nid = a.id;
		}
	}

	public class DetailViewDataModel : CardViewDataModel
	{
		public string detailLabel;
		public string suggestedAudienceTextLabel;
		public string suggestedSkillTextLabel;
	}

	public class ToolTipViewDataModel : CardViewDataModel
	{
		public string toolTitleLabel;
		public string firstSpriteText;
		public string secondSpriteText;
		public string thirdSpriteText;
		public string toolDetailLabel;
	}

	public class SponsorViewDataModel : CardViewDataModel
	{
		public Texture2D logo;
		public string websiteText;
	}
}

