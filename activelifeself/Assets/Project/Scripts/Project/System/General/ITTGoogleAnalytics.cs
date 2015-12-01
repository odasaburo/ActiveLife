using UnityEngine;
using System.Collections;

namespace ITT.System
{
	public class ITTGoogleAnalytics : ITTSingleton<ITTGoogleAnalytics> 
	{
		protected ITTGoogleAnalytics() {}

		private GoogleAnalyticsV3 _googleAnalytics;
		public GoogleAnalyticsV3 googleAnalytics
		{
			get
			{
				_googleAnalytics = _googleAnalytics ? _googleAnalytics : GameObject.Find("GAv3").GetComponent<GoogleAnalyticsV3>();
				if (null == _googleAnalytics)
				{
					Debug.LogError("Could not find the Google Analytics Object");
				}
				return _googleAnalytics;
			}

		}
	}
}

