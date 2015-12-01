using UnityEngine;
using System.Collections.Generic;
using System;
using System.Web;

namespace ITT.System
{
	[global::System.Serializable]
	public class MasterBaseCardData
	{
		public ActivityDataModel[] results;
	}

	[global::System.Serializable]
	public class MasterSponsorCardData
	{
		public SponsorDataModel[] results;
	}

	[global::System.Serializable]
	public class UserSavedActivityData
	{
		public UserSavedActivityType[] results;
	}

	[global::System.Serializable]
	public class UserSavedActivityType
	{
		public ActivityDataModel activity;
	}

	[global::System.Serializable]
	public abstract class BaseCardData 
	{
		public string title;

		private string _description;
		public string Description
		{
			get
			{
				return _description;
			}

			set
			{
				_description = HelperMethods.ScrubHtml(value);
			}
		}

		private float? _proximity = null;
		public float Proximity
		{
			get
			{
                if (_proximity.HasValue)
                    return _proximity.Value;

				LocationDataModel currentLocation = ITTDataCache.Instance.RetrieveLocationData();
				if (null == currentLocation)
				{
					currentLocation = ITTDataCache.Instance.DefaultLocationData();
				}

				_proximity = HaversineCalc.Distance((float)currentLocation.lat, (float)currentLocation.lon, (float)address.lat, (float)address.lon, DistanceType.Miles);
				return _proximity.Value;
			}
		}

		public string category
		{
			get
			{
				if (category_physical_activities)
				{
					return "Physical Activity";
				}
				else if (category_health_wellness)
				{
					return "Health & Wellness";
				} 
				else
				{
					return "Food & Nutrition";
				}

			}
		}

		public string company;
		public string country;
		public AddressBody address;
		public Int64 admission_adults;
		public Int64 admission_children;
		public ActivityEventDate event_date;
		public ActivityEventDate[] next_dates;
		public ActivityEventDate first_event;
		public ActivityImageData image;

		public Int64 id;
		public string phone;
		public string website;
		public Int64 card_discount;
		public virtual string Type {get; set;}

		#region Boolean Values
		public bool audience_seniors;
		public bool audience_adults;
		public bool audience_teenagers;
		public bool audience_kids;
		public bool level_beginner;
		public bool level_intermediate;
		public bool level_advanced;
		public bool level_expert;
		public bool moderated;
		public bool featured;
		public bool category_physical_activities;
		public bool category_health_wellness;
		public bool category_food_nutrition;
		public bool recommended;
		#endregion

		[global::System.Serializable]
		public class AddressBody
		{
			public string address;
			public double lat;
			public double lon;

		}

		[global::System.Serializable]
		public class ActivityEventDate
		{
			public string value;
		}

		[global::System.Serializable]
		public class ActivityImageData
		{
			public string key;
			public string mime_type;
			public string name;
			public string serving_url;
			public Int64 size;
		}

		#region Helper Fields & Methods
		public DateTime dateTime;
		
		[global::System.NonSerialized]
		public Action<Texture2D> OnImageDownloaded;
		[global::System.NonSerialized]
		public Action OnImageDownloadFailed;

		public DateTime ParseDateString()
		{	
			if (null == first_event && null == event_date)
			{
				return new DateTime();
			}
			if (string.IsNullOrEmpty(RetrieveEventDate()))
				return new DateTime();
			
			if (dateTime != DateTime.MinValue)
				return dateTime;
			
			try
			{
				dateTime = DateTime.Parse(RetrieveEventDate());
			}
			catch
			{
				string[] subs = RetrieveEventDate().Split(',');
				List<string> temp = new List<string>();
				foreach (string sub in subs)
				{
					string newstr = sub.Substring(0, sub.Length - 6);
					temp.Add(newstr);
				}
				try
				{
					dateTime = DateTime.Parse(temp[0]);
				}
				catch (Exception e)
				{
					Debug.LogError("Error parsing date: " + e.Message);
					
					dateTime = DateTime.MinValue;
				}
			}
			
			return dateTime;
		}
		
		public void StartImageDownload(Action<Texture2D> onImageDownloaded,
		                               Action onImageDownloadFailed,
		                               string imageURL = null)
		{
			string url;
			if (null == imageURL)
			{
				if (null == image)
				{
					return;
				}
				if (!string.IsNullOrEmpty(image.serving_url))
				{
					url = image.serving_url;
				}
				else
				{
					return;
				}
			}
			else
			{
				url = imageURL;
			}
			OnImageDownloaded = onImageDownloaded;
			OnImageDownloadFailed = onImageDownloadFailed;
			string sanitizedImageURL = ITTNetworkManager.SanitizeImageURL(url);
			if (!string.IsNullOrEmpty (sanitizedImageURL))
				ITTDataCache.Instance.StartDownloadImage (sanitizedImageURL, ImportImage, ImportFailed);
			else
				ImportFailed ("URL is null");
		}
		
		private void ImportImage(Texture2D tex) 
		{
			if (null == tex)
			{
				Debug.LogError("ImportImage failure for card " + title);
				OnImageDownloadFailed();
			}
			else if (null != OnImageDownloaded)
			{
				OnImageDownloaded(tex);
			}
		}
		
		private void ImportFailed(string error)
		{
			Debug.LogError("Error importing image for card " + this.id + ": " + error);
			OnImageDownloadFailed();
		}

//		public bool OccursOnDate(DateTime date)
//		{
//			// Check today's date
//			ParseDateString(); // In case this hasn't happened yet. 
//			if (date.Date == this.dateTime.Date)
//			{
//				return true;
//			}
//
//			// Check repeat dates
//			foreach (var dateString in next_dates)
//			{
//				var dateTime = ParseDateString (dateString.value);
//
//				if(date.Date == dateTime.Date) {
//					return true;
//				}
//			}
//
//			return false;
//		}

		public string RetrieveEventDate()
		{
			if (null != event_date)
			{
				return event_date.value;
			}
			else if (null != first_event)
			{
				return first_event.value;
			}
			return null;
		}

		#endregion
	}
}