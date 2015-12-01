using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Web;


namespace ITT.System
{
	public class HelperMethods : ITTSingleton<HelperMethods>
	{
		#region HTML tag strip regex
		private static Regex _commentRegex = new Regex("<!--.*?-->", RegexOptions.Compiled | RegexOptions.Singleline);
		private static Regex _htmlRegex = new Regex("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);
		private static Regex _whitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled | RegexOptions.Multiline);
		private static Regex _emptyLineRegex = new Regex(@"^\s*$[\r\n]*", RegexOptions.Compiled | RegexOptions.Multiline);
		#endregion

		protected HelperMethods() {}
		public  readonly int DaysInAWeek = 6;
		public readonly string Error_EmptyResults = "EMPTY_RESULTS";
		public readonly string Error_NetworkTimeOut = "NETWORK_TIMEOUT";
		public readonly string Error_NetworkRadioOff = "NETWORK_RADIO_OFF";
		public static readonly string Und = "und";
		public static readonly float meterMileConversion = 1609.34f;
		public static readonly Vector3 CloseToZeroVec =  new Vector3 (0.01f, 0.01f, 0.01f); // This variable is used to aviod a bug that occurs with the distance slider button snapping to the center of the screen if Vector3.zero was used for the scale during scene transitions.
		public static readonly Vector3 BigScreenSizeVec = new Vector3 (2f, 2f, 2f);
		private static float _animationInDelay = 0.1f;
		private static float _lerpDuration = 0.4f;

		private float _timePressedPhoneButton = 0f;

		public Dictionary<string, string> StringTable = new Dictionary<string, string>()
		{
			{ "category_physical_activity", "Physical Activity" },
			{ "category_health_wellness", "Health & Wellness" },
			{ "category_food_nutrition", "Food & Nutrition" },
			{ "audience_seniors", "Senior Citizens" },
			{ "audience_adults", "Adults" },
			{ "audience_teenagers", "Teenagers" },
			{ "audience_kids", "Children" },
			{ "toddlers", "Toddlers" },
			{ "level_expert", "Expert" },
			{ "level_advanced", "Advanced" },
			{ "level_intermediate", "Intermediate" },
			{ "level_beginner", "Beginner Friendly" }
		};

		public enum TypeOfAnimation {AnimationIn = 0, AnimationOut = 1}

		#region String Parsers
		public DateTime? ParseDateString(string dateString)
		{
			if (string.IsNullOrEmpty(dateString))
				return null;
		
			DateTime? dateTime = null;
			try
			{
				dateTime = DateTime.Parse(dateString);
			}
			catch
			{
				string[] subs = dateString.Split(',');
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
				}
			}
			
			return dateTime;
		}

		public string StripHtmlTags(string str)
		{
			str = _commentRegex.Replace (str, string.Empty);
			str = _htmlRegex.Replace (str, string.Empty);
			str = str.Replace ("&nbsp;", " ");
			str = str.Replace (",", ", ");
			str = str.Replace (";", "; ");
			str = str.Replace (".", ". ");
			str = str.Replace ("!", "! ");
			str = str.Replace ("?", "? ");
			str = str.Replace ("&amp;", "&");
			str = _whitespaceRegex.Replace (str, " ");
			str = _emptyLineRegex.Replace (str, string.Empty);
			str.Trim ();
			return str;
		}

		public void FormatAndCallNumber(string number)
		{		
			if (string.IsNullOrEmpty(number))
				return;

			if (Time.realtimeSinceStartup > _timePressedPhoneButton + 1f)
			{
				_timePressedPhoneButton = Time.realtimeSinceStartup;

				string telFormat = number;
				
				telFormat = telFormat.Replace("(", "");
				telFormat = telFormat.Replace(")", "");
				telFormat = telFormat.Replace(" ", "");
				telFormat = telFormat.Replace("-", "");
				telFormat = telFormat.Replace("+", "");
				
				if (Regex.IsMatch(telFormat, "[0-9]{7,11}"))
				{
					#if UNITY_ANDROID
					Application.OpenURL("tel:" + telFormat);
					#elif UNITY_IPHONE
					Application.OpenURL("telprompt://" + telFormat);
					#endif
				}
			}
		}

		public void OpenMap(string address)
		{
#if UNITY_IPHONE
			string url = "http://maps.apple.com/";
#else
			string url = "http://maps.google.com/";
#endif

			url += "?q=" + HttpUtility.UrlEncode(address);
			Debug.Log (url);
			Application.OpenURL (url);
		}

		#endregion
		[Serializable]
		public class UserFlags
		{
			public bool hasSeenTooltip = false;
			public bool hasToggledLocation = false;
		}

		[Serializable]
		public class TipTracker
		{
			public int tipOffset = 0;
			public DateTime tipTimeLastShown = DateTime.MinValue;
		}

		[Serializable]
		public class RecommendationTracker
		{
			public DateTime timeLastShown = DateTime.MinValue;
		}

		#region API response helper classes
		public class LoginResponseObject
		{
			public string username;
			public string session_token;
			public Int64 id;
			public string email;
		}

		public class ChangeEmailRequestObject
		{
			public string email;
		}

		public class ChangePasswordRequestObject
		{
			public string password;
		}

		public class RefreshTokenResponseObject
		{
			public string token;
		}

		public class ProfileActivityReponseObject
		{


			public ProfileActivityResponse[] results;
			public int total_count;
		}

		public class ProfileActivityResponse
		{
			public ProfileActivity activity;
			public ProfileActivityDateTime created_at;
			public Int64 creator_id;
			public Int64 id;
			public string recommended;
			public ProfileActivityDateTime updated_at;
		}
		
		public class ProfileActivity
		{
			public string __type;
			public string kind;
			public Int64 id;
		}
		
		public class ProfileActivityDateTime
		{
			public string __type;
			public string value;
		}

		public class ResultReponseObject
		{
			public object[] results;
			public int total_count;
		}
		#endregion

		#region Flag User Status
		public void FlagCurrentLocation(DataEntryBase data) {
			UserFlagsEntry flags = data as UserFlagsEntry;
			if (null == flags)
				return;
			
			HelperMethods.UserFlags tempFlags = flags.Data;
			if (flags == null) {
				return;
			} else {
				if (tempFlags == null) {
					tempFlags = new HelperMethods.UserFlags();
				}
				
				tempFlags.hasToggledLocation = true;
				ITTDataCache.Instance.Data.UpdateDataEntry((int)DataCacheIndices.USER_FLAGS, tempFlags, false);
			}
		}

		public void UnFlagCurrentLocation(DataEntryBase data) {
			UserFlagsEntry flags = data as UserFlagsEntry;
			if (null == flags)
				return;
			
			HelperMethods.UserFlags tempFlags = flags.Data;
			if (flags == null) {
				return;
			} else {
				if (tempFlags == null) {
					tempFlags = new HelperMethods.UserFlags();
				}
				
				tempFlags.hasToggledLocation = false;
				ITTDataCache.Instance.Data.UpdateDataEntry((int)DataCacheIndices.USER_FLAGS, tempFlags, false);
			}
		}

		public IEnumerator RetrieveAndRefreshLocationData(Action<string> errorCallback)
		{
			yield return StartCoroutine(ITTDataCache.Instance.UpdateLocation(
				() => {
					RefreshLocationData();

					// Note: I really dislike this side effect, leaving it for backwards compatibility
					ITT.Scene.ITTMainSceneManager.Instance.currentState = ITT.Scene.ITTMainSceneManager.ITTStates.Main;
				}, 
				(o) => {
					RefreshLocationData();
					errorCallback(o);
				}));
		}

		public IEnumerator RetrieveLocationData(Action successCallback, Action<string> errorCallback)
		{
			yield return StartCoroutine(ITTDataCache.Instance.UpdateLocation(
				() => {
					RefreshLocationData();
					successCallback();
				}, 
				errorCallback));
		}

		private void RefreshLocationData()
		{
			ITTDataCache.Instance.Data.ClearDataEntry((int)DataCacheIndices.ACTIVITY_LIST);
			ITTDataCache.Instance.Data.RefreshDataEntry((int)DataCacheIndices.ACTIVITY_LIST);
		}

		#endregion
		#region Animation helpers

		public IEnumerator AnimateIn(GameObject go)
		{
			yield return new WaitForSeconds(_animationInDelay);

			FadeObject(go, TypeOfAnimation.AnimationIn);

			if(ITT.Scene.ITTMainSceneManager.Instance.currentState.CompareTo(ITT.Scene.ITTMainSceneManager.Instance.previousState) < 0)
			{
				yield return StartCoroutine(InterpolateScale(go, CloseToZeroVec, Vector3.one, _lerpDuration));
			}
			else
			{
				yield return StartCoroutine(InterpolateScale(go, BigScreenSizeVec, Vector3.one, _lerpDuration));
			}

			RemoveInteractionBlocker();
		}

		public IEnumerator AnimateOut(GameObject go)
		{
			AddInteractionBlocker();

			FadeObject(go, TypeOfAnimation.AnimationOut);

			if(ITT.Scene.ITTMainSceneManager.Instance.currentState.CompareTo(ITT.Scene.ITTMainSceneManager.Instance.previousState) < 0)
			{
				yield return StartCoroutine(InterpolateScale(go, Vector3.one, BigScreenSizeVec, _lerpDuration));
			}
			else
			{
				yield return StartCoroutine(InterpolateScale(go, Vector3.one, CloseToZeroVec, _lerpDuration));
			}
		}

		public IEnumerator ForcedReverseIn(GameObject go)
		{
			yield return new WaitForSeconds(_animationInDelay);

			FadeObject(go, TypeOfAnimation.AnimationIn);
			yield return StartCoroutine(InterpolateScale(go, CloseToZeroVec, Vector3.one, _lerpDuration));

			RemoveInteractionBlocker();

			yield break;
		}

		public IEnumerator ForcedReverseOut(GameObject go)
		{
			AddInteractionBlocker();
            
			FadeObject(go, TypeOfAnimation.AnimationOut);
			yield return StartCoroutine(InterpolateScale(go, Vector3.one, BigScreenSizeVec, _lerpDuration));

			yield break;
		}

		#endregion

		public static string ScrubHtml(string value) {
			var step1 = Regex.Replace(value, @"<[^>]+>|&nbsp;", "").Trim();
			var step2 = Regex.Replace(step1, @"\s{2,}", " ");
			return step2;
		}

		public static IEnumerator InterpolateScale(GameObject thisObject, Vector3 initialScale, Vector3 finalScale , float lerpDuration)
		{
			float lerpTime = 0f;

			thisObject.transform.localScale = initialScale;
			while(lerpTime < lerpDuration)
			{
                lerpTime += Time.deltaTime;

				thisObject.transform.localScale = Vector3.Lerp(initialScale, finalScale, (lerpTime/lerpDuration) );
				yield return null;
			}

			thisObject.transform.localScale = finalScale;
		}

		public static void FadeObject(GameObject thisObject, TypeOfAnimation animationEnum, float duration = -1f)
		{
            if (duration == -1f)
                duration = _lerpDuration;

			TweenAlpha fadeObject = thisObject.GetComponent<TweenAlpha>();
            if (fadeObject == null)
                fadeObject = thisObject.AddComponent<TweenAlpha>();

			if(fadeObject  != null)
			{
				fadeObject.from = 0f;
				fadeObject.to = 1f;
				fadeObject.duration = duration;

                if (animationEnum == TypeOfAnimation.AnimationIn)
                {
                    fadeObject.PlayForward();
                }
                else
                {
                    // NOTE: work around of what seams to be a NGUI bug that causing "cold" reverse runs to end prematurely
                    fadeObject.tweenFactor = 1f;
                    fadeObject.PlayReverse();
                }
			}
		}

		public static void AddInteractionBlocker()
		{
			UICamera nGuiCamera = null;
			if(null != (nGuiCamera = FindObjectOfType<UICamera>()))
			{
				nGuiCamera.enabled = false;
			}
		}

		public static void RemoveInteractionBlocker()
		{
			UICamera nGuiCamera = null;
			if(null != (nGuiCamera = FindObjectOfType<UICamera>()))
			{
				nGuiCamera.enabled = true;
			}
		}
	}
}