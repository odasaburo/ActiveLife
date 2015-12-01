using UnityEngine;
using System.Collections;


namespace ITT.System 
{
	[global::System.Serializable]
	public class LocationDataModel
	{
		public double lat;
		public double lon;
		public double lastLongitude
		{
			get
			{
				return lon;
			}

		}
		public double lastLatitude
		{
			get
			{
				return lat;
			}
		}

		public float lastAltitude;
		public double locationTimeStamp;
		public string zipCode;
		public string cityState;
	}
}

