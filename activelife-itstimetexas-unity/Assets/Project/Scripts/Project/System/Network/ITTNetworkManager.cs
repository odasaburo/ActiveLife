using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JsonFx.Json;
using System.Net;
using System.Text;
using System.Web;
using System;
using System.IO;

namespace ITT.System {
	#region Detail Enums
	public enum DetailType
	{
		Activity = 0,
		Sponsor,
		HealthyTip
	}
	#endregion

	public class ITTNetworkManager : MonoBehaviour {


		public ITTSessionBlob SessionManager {get; set;}

		#region Server Consts
		//	Staging Server
//		private const string CHOOSE_HEALTHIER_URI = "http://test.juju-staging.io/api/v1/";
//		private const string CHOOSE_HEALTHIER_APIKEY = "bGl2ZV90ZXN0OjAuMDk2ODQ1MzkzNTg0NzpOUFdvYldtbXhPeHNEZU5DZzZ6UWRMaWduQ0E";
//		private const string CHOOSE_HEALTHIER_HOST = "test.juju-staging.io";

		//	Production Server
//		private const string CHOOSE_HEALTHIER_URI = "https://activelife.juju.io/api/v1/";
//		private const string CHOOSE_HEALTHIER_APIKEY = "bGl2ZV9hY3RpdmVsaWZlOjAuOTY5NjEzNjc2NjAyOkkyWEV0MGxJemU3SFBVMDF2blZObDhkRmRscw";
//		private const string CHOOSE_HEALTHIER_HOST = "activelife.juju.io";

		// NEW SERVER 
		private const string CHOOSE_HEALTHIER_URI = "http://api.choosehealthier.org/api/";
		//private const string CHOOSE_HEALTHIER_URI = "http://cmch.wpengine.com/api/";
		private const string CHOOSE_HEALTHIER_APIKEY = "bGl2ZV9hY3RpdmVsaWZlOjAuOTY5NjEzNjc2NjAyOkkyWEV0MGxJemU3SFBVMDF2blZObDhkRmRscw";
		private const string CHOOSE_HEALTHIER_HOST = "api.choosehealthier.org";
		
	

			#endregion

		#region ITTNetworkRequest
		class ITTNetworkRequest
		{
			public WWW www;
			public DateTime requestStartTime;

			public ITTNetworkRequest(string uri, byte[] postData, Dictionary<string, string> headers, DateTime startTime)
			{
				www = new WWW(uri, postData, headers);
				requestStartTime = startTime;
			}
		}
		private List<ITTNetworkRequest> _activeRequests = new List<ITTNetworkRequest>();
		private const int REQUEST_LIMIT = 200;
		private const int TIMEOUT_SECONDS = 20;
		public bool connectionError = false;
		private bool _attemptingReconnect = false;
#if UNITY_EDITOR
		private readonly string AIRPLANE_MODE = "(Could not contact DNS servers)";
		private readonly string TIMEOUT_RESPONSE = "(Could not contact DNS servers)";
#elif UNITY_ANDROID
		private readonly string AIRPLANE_MODE = "No address associated with hostname";
		private readonly string TIMEOUT_RESPONSE = "No address associated with hostname";
#elif UNITY_IPHONE
		private readonly string AIRPLANE_MODE = "The Internet connection appears to be offline.";
		private readonly string TIMEOUT_RESPONSE = "A server with the specified hostname could not be found.";
#endif

		private Texture2D _receivedTexture;
		public bool networkRecovered = false;
		void Update()
		{

			_activeRequests.FindAll(request => ((int)(DateTime.Now - request.requestStartTime).TotalSeconds >= TIMEOUT_SECONDS))
				.ForEach(expired => {
					if (null != expired.www)
					{
						expired.www.Dispose();
						expired.www = null;
					}
				});
			//	if there is a connection error test server until its valid then refresh the app and kill flag
			if (connectionError && !_attemptingReconnect)
			{	
				_activeRequests.ForEach(request => 
				{
					if (null != request.www)
					{
						request.www.Dispose();
						request.www = null;
					}
				});
				_activeRequests.Clear();
				_attemptingReconnect = true;
				StartCoroutine(checkInternetConnection("http://www.google.com"));
			}
		}

		IEnumerator checkInternetConnection(string url)
		{
			ITTNetworkRequest testRequest = new ITTNetworkRequest(url, null ,null, DateTime.Now);
			_activeRequests.Add(testRequest);

			#if UNITY_IPHONE
			while (null != testRequest.www && !testRequest.www.isDone)
				yield return null;
			#else
			yield return testRequest.www;
			#endif

			_attemptingReconnect = false;

			if (null != testRequest.www && string.IsNullOrEmpty(testRequest.www.error))
			{
				connectionError = false;
				networkRecovered = true;
				ITTDataCache.Instance.Data.ResetWaitOnServerForAllEntries();
				ITTDataCache.Instance.Data.RefreshAllData(true);
			}
		}
		#endregion

		private Dictionary<string, string> _postHeader;
		//	Data Cache will handle this postheader
		private Dictionary<string, string> PostHeader() {
			Dictionary<string, string> postDictionary = new
				Dictionary<string, string>();

			if (!string.IsNullOrEmpty(SessionManager.UserToken))
			{
				postDictionary.Add("X-Juju-Session-Key", SessionManager.UserToken);
			}

			return postDictionary;
		}

		#region Call
		public class NetworkCall<T>
		{
			public Action<T> SuccessHandler;
			public Action<string> FailureHandler;
			public string URL;
			public string RequestBody;

			public NetworkCall(Action<T> successHandler, Action<string> failureHandler, string url, string requestBody)
			{
				URL = url;
				RequestBody = requestBody;
				SuccessHandler = successHandler;
				FailureHandler = failureHandler;
			}
		}
		#endregion

		#region Login Flow
		protected class BufferedCall

		{
			public Action<string> SuccessHandler;
			public Action<string> FailureHandler;
			public string URL;
			public string RequestBody;

			public BufferedCall(string url, string requestBody, Action<string> successHandler, Action<string> failureHandler)
			{
				URL = url;
				RequestBody = requestBody;
				SuccessHandler = successHandler;
				FailureHandler = failureHandler;
			}
		}

		// TODO: this should be a collection of most recent calls eventually.
		protected BufferedCall bufferedCall;

		private void OnRefreshTokenSuccess(string json)
		{
			try
			{
				HelperMethods.RefreshTokenResponseObject tokenObject = ClientSDK.Utils.Json.GetObjectFromJson<HelperMethods.RefreshTokenResponseObject>(json);
				Debug.LogError("RefreshToken: normal");
				SessionManager.UpdateUserToken(tokenObject.token);

				if (null == bufferedCall)
					return;

				// Make original call again
				StartCoroutine(_PerformRequest<string>(bufferedCall.URL, bufferedCall.RequestBody, bufferedCall.SuccessHandler, bufferedCall.FailureHandler));
			}
			catch (JsonFx.Json.JsonDeserializationException d)
			{

				global::System.Text.RegularExpressions.Match match = global::System.Text.RegularExpressions.Regex.Match(json, "[A-Za-z0-9-_]{43}");
				Debug.LogError("RefreshToken: odd format: " + json + " " + d.Message);
				SessionManager.UpdateUserToken(match.ToString());

				if (null == bufferedCall)
					return;

				// Make original call again
				string url = bufferedCall.URL;
				string request = bufferedCall.RequestBody;
				Action<string> successHandler = bufferedCall.SuccessHandler;
				Action<string> failureHandler = bufferedCall.FailureHandler;
				bufferedCall = null;
				StartCoroutine(_PerformRequest<string>(url, request, successHandler, failureHandler, false));
			}
			catch (Exception e)
			{
				Debug.LogError("Token response parsing error: " + e.Message);
			}
		}
		private void OnRefreshTokenFailure(string error)
		{
			if (error.ToLower().Contains("not logged in"))
			{
				Debug.LogError("Token failure: prompting login");
				ITT.Scene.OnboardingViewController.Spawn(OnLoginSuccessRetryBufferedCall);
			}
			else
			{
				Debug.LogError("Error refreshing token: " + error);
				ModalPopupOK.Spawn("An error occurred."); // TODO: better message
			}
		}

		private void OnLoginSuccessRetryBufferedCall()
		{
			// Credentials should already be updated since the login was successful
			// try original call again
			Debug.LogError("Login success: attempting buffered call");
			string url = bufferedCall.URL;
			string request = bufferedCall.RequestBody;
			Action<string> successHandler = bufferedCall.SuccessHandler;
			Action<string> failureHandler = bufferedCall.FailureHandler;
			bufferedCall = null;

			StartCoroutine(_PerformRequest<string>(url, request, successHandler, failureHandler, false));
		}
		#endregion

		#region Anonymous Calls
		private string _GenerateURL(string displayID, double geoLat, double geoLong, ITTFilterRequest filterRequest, int limit = REQUEST_LIMIT) {
			return CHOOSE_HEALTHIER_URI + "db/" + displayID + "?sorting=event_date&query=" + filterRequest.RetrieveFilterURLString() + "&limit=" + limit;
		} 

		private void _WWWRequest<T>(WWW www, Action<T> callback, Action<string> errorCallback) where T : class {
			Type requestType = typeof(T);

			if (connectionError)
				return;

			if (null == www)
			{
				connectionError = true;
				if (null != errorCallback)
				{
					errorCallback(HelperMethods.Instance.Error_NetworkTimeOut);
					return;
				}
			}

			if (!string.IsNullOrEmpty(www.error)) {
				if (null != errorCallback) {
					string errorMessage;
					if (www.error.Contains(AIRPLANE_MODE))
					{
						errorMessage = HelperMethods.Instance.Error_NetworkRadioOff;
						connectionError = true;
					}
					else if (www.error.Contains(TIMEOUT_RESPONSE))
					{
						errorMessage = HelperMethods.Instance.Error_NetworkTimeOut;
						connectionError = true;
					}
					else
					{
						errorMessage = "Data Request error: " + www.error;
					}
					errorCallback(errorMessage);
				}
			} else {
				if (requestType == typeof(string)) {
					string data = www.text;
					if (string.IsNullOrEmpty(data))
					{
						if (null != errorCallback) {
							errorCallback("Data Request error: data returned null/empty string.");
						}
					}
					else
					{
						if (null != callback)
						{
							callback(data as T);
						}
					}
				}
				else if (requestType == typeof(Texture2D))
				{
					_receivedTexture = new Texture2D(560, 560, TextureFormat.RGB24, false);
					www.LoadImageIntoTexture(_receivedTexture);
					if (null == _receivedTexture && null != errorCallback)
					{
						errorCallback("DownloadImage error: failed to download image (null)"); 
					}
					else if (null != callback)
					{
						www.Dispose();
						www = null;
						callback(_receivedTexture as T);
					}
				} 
				else if (requestType == typeof(byte[])) {
					if (null == www.bytes || www.bytesDownloaded == 0)
					{
						if (null != errorCallback) {
							errorCallback("DownloadImage error: failed to download (size zero).");
						}
					}
					else
					{
						if (null != callback)
						{
							callback(www.bytes as T);
						}
					}
				}
			}

		}

		private IEnumerator _PerformRequest<T>(string url, string jsonData, Action<T> callback, Action<string> errorCallback, bool bufferCall = true) where T : class {

			if (connectionError)
			{
				if (null != errorCallback)
				{
					errorCallback(HelperMethods.Instance.Error_NetworkTimeOut);
					yield break;
				}
			}

			byte[] encodedJSON = null;

			if (!string.IsNullOrEmpty(jsonData)) {
				var encoding = new UTF8Encoding();
				encodedJSON = encoding.GetBytes(jsonData);
			}

			// Save this call in case it fails due to session credentials
			if (bufferCall)
			{
				bufferedCall = new BufferedCall(url, jsonData, callback as Action<string>, errorCallback);
			}

			ITTNetworkRequest networkRequest = new ITTNetworkRequest(url, encodedJSON, PostHeader(), DateTime.Now);
			_activeRequests.Add(networkRequest);

			#if UNITY_IPHONE
			while (null != networkRequest.www && !networkRequest.www.isDone)
				yield return null;
			#else
			yield return networkRequest.www;
			#endif

			_WWWRequest(networkRequest.www, callback, errorCallback);

		}

		public IEnumerator RequestDeals(double geoLat, double geoLong, ITTFilterRequest filterRequest, Action<string> callback, Action<string> errorCallback, int limit = REQUEST_LIMIT) {
			string url = _GenerateURL("deals/", geoLat, geoLong, filterRequest, limit);
			yield return StartCoroutine(_PerformRequest<string>(url, null, callback, errorCallback));
		}

		public IEnumerator RequestCombinedActivitiesDeals(double geoLat, double geoLong, ITTFilterRequest filterRequest, Action<string> callback, Action<string> errorCallback, int limit = REQUEST_LIMIT) {
			string url = _GenerateURL("Activity/", geoLat, geoLong, filterRequest, limit);
			yield return StartCoroutine(_PerformRequest<string>(url, null, callback, errorCallback));
		}

		public IEnumerator RequestSponsors(Action<string> callback, Action<string> errorCallback) {
			string url = CHOOSE_HEALTHIER_URI + "db/Sponsor/";
			yield return StartCoroutine(_PerformRequest<string>(url, null, callback, errorCallback));
		}

		public IEnumerator RequestHealthyTips(int tipOffset, int tipLimit, Action<string> callback, Action<string> errorCallback) {
			string url = CHOOSE_HEALTHIER_URI + "views/services.json?display_id=tips" + "&offset=" + tipOffset + "&limit=" + tipLimit;
			yield return StartCoroutine(_PerformRequest<string>(url, null, callback, errorCallback));
		}


		/// <summary>
		/// Retrieve the detail information from the id for any anonymous call
		/// </summary>
		/// <param name="objectId">Object identifier. This can be for any anonymous object detail id</param>
		/// <param name="callback">Raw JSON string retrieved from the call. This can be applied to a parser</param>
		public IEnumerator RetrieveDetailData(DetailType detailType, Int64 objectId, Action<string> callback, Action<string> errorCallback) {
			string parseDetailType = Enum.GetName(typeof(DetailType), detailType);
			string url = CHOOSE_HEALTHIER_URI + "db/" + parseDetailType + "/" + objectId;
			yield return StartCoroutine(_PerformRequest<string>(url, null, callback, errorCallback));
		}

		public IEnumerator DownloadImage(string imageURL, Action<Texture2D> callback, Action<string> errorCallback)
		{
			if (string.IsNullOrEmpty(imageURL))
			{
				if (null != errorCallback) {
					errorCallback("DownloadImage error: failed to download (size zero).");
				}
				yield break;
			}

			yield return StartCoroutine(_PerformRequest<Texture2D>(imageURL, null, callback, errorCallback));
		}

		public IEnumerator GetStaticMapImage(string location, Action<byte[]> callback, Action<string> errorCallback) {
			// If an address, replace space with +
			// If a lat/lon, remove space entirely
			location = location.Replace(" ", "+");
			location = location.Replace("\n", ",+");
			string url = "https://maps.googleapis.com/maps/api/staticmap?maptype=roadmap&size=256x119&scale=4&markers=size:mid%7Ccolor:red%7C" + location;
			WWW www = new WWW(url);
			yield return www;

			if (null != www.error)
				errorCallback(www.error);
			else
				callback(www.bytes);
		}

		public IEnumerator GetGeoLocationData(string addressString, Action<string> callback, Action<string> errorCallback) {
			string escapeEncodedString = addressString.Replace(" ", "%20").Replace(",", "%2C");
			string url = CHOOSE_HEALTHIER_URI + "user/geocode?query=" + escapeEncodedString;
			yield return StartCoroutine(_PerformRequest<string>(url, null, callback, errorCallback));
		}

		#endregion

		#region Anonymous call helpers
		public void StartDownloadImage(string imageURL, Action<Texture2D> callback, Action<string> errorCallback)
		{
			StartCoroutine(DownloadImage(imageURL, callback, errorCallback));
		}

		public static string SanitizeImageURL(string url)
		{
			if (string.IsNullOrEmpty(url))
			{
				Debug.LogError("Image URL improperly formatted: " + url);
				return null;
			}

			return url;
		}

		#endregion

		#region Authenticated Calls
		private void HandleLoginRequest(string jsonData) {

//			JObject o = JObject.Parse(jsonData);
//			SessionManager.UpdateSessionId(o["sessid"].ToString());
//			SessionManager.UpdateSessionName(o["session_name"].ToString());
//			SessionManager.UpdateUserToken(o["token"].ToString());
//			SessionManager.UpdateUserUid(o["user"]["uid"].ToString());
//			UserDataModel udm = new UserDataModel();
//			udm = JsonConvert.DeserializeObject<UserDataModel>(o["user"].ToString());
//			userDataModel = udm;
//
//			Debug.Log("WWW Object(User First Name):" + udm.Field_First_Name[UND_ARRAY][0].Value);
//			Debug.Log("WWW Object(User Age):" + udm.Field_Age[UND_ARRAY][0].Value);
//			Debug.Log("WWW Object(User Home Location):" + udm.Field_Home_Location[UND_ARRAY][0].Country);
//			Debug.Log("WWW Object(User Work Location):" + udm.Field_Work_Location[UND_ARRAY][0].Country);
//			Debug.Log("WWW Object(User Zip Code):" + udm.Field_Zipcode[UND_ARRAY][0].Postal);
//			Debug.Log("WWW Object(User Field Notification):" + udm.Field_Notifications[UND_ARRAY][0].Value);
//
//
//			Debug.Log ("WWW Object(Session ID): " + userSessionId);
//			Debug.Log ("WWW Object(Session Name): " + userSessionName);
//			Debug.Log ("WWW Object(Token): " + userToken);
			
		}

//		IEnumerator WaitForRegistrationRequest(WWW www) {
//			while (!www.isDone) {
//				yield return null;
//			}
//			if(string.IsNullOrEmpty(www.error)) {
//
//				JObject o = JObject.Parse(www.text);
//				SessionManager.UpdateUserUid(o["uid"].ToString());
//
//			} else {
//				Debug.Log ("WWW error:" + www.error);
//			}
//		}
//
//		IEnumerator WaitForUserLogoutRequest(WWW www) {
//
//			while (!www.isDone) {
//				yield return null;
//			}
//
//			if(string.IsNullOrEmpty(www.error)) {
//				JArray o = JArray.Parse(www.text);
//			} else {
//				Debug.Log ("WWW error:" + www.error);
//			}
//		}

//		IEnumerator WaitForUserRetrieveRequest(WWW www) {
//			
//			while (!www.isDone) {
//				yield return null;
//			}
//			
//			if(string.IsNullOrEmpty(www.error)) {
//				JObject o = JObject.Parse(www.text);
//			} else {
//				Debug.Log ("WWW error:" + www.error);
//			}
//		}

		public IEnumerator LoginUser(string username, string password, Action<string> callback, Action<string> errorCallback) {
			string uri = CHOOSE_HEALTHIER_URI + "user/generate_auth_cookie/";
			string jsonString = "username=" + username + "&password=" + password;
			jsonString = jsonString.Replace("@", "%40");
			yield return StartCoroutine(_PerformRequest<string>(uri, jsonString, callback, errorCallback, false));
		}

		public IEnumerator RegisterUser(RegistrationDataModel rdm, Action<string> callback, Action<string> errorCallback) {
			string uri = CHOOSE_HEALTHIER_URI + "user/register/";
			string formData = "username=" + rdm.username.Replace("@", "%40") + "&password=" + rdm.password + "&email=" + rdm.email.Replace("@", "%40");
			Debug.LogWarning (uri + " " + formData);
			yield return StartCoroutine(_PerformRequest<string>(uri, formData, callback, errorCallback));
		}

		public IEnumerator UpdateUser(string userData, Action<string> callback, Action<string> errorCallback) {
			string uri = CHOOSE_HEALTHIER_URI + "user/update/";
			yield return StartCoroutine(_PerformRequest<string>(uri, userData, callback, errorCallback));
		}

		public IEnumerator UpdateUserEmail(HelperMethods.ChangeEmailRequestObject emailRequest, Action<string> callback, Action<string> errorCallback) {
			string userDataModelJson = "{ \"email\": \"" + emailRequest.email + "\"}";
			yield return StartCoroutine(UpdateUser(userDataModelJson, callback, errorCallback));
		}

		public IEnumerator RequestNewPassword(string username, Action<string> callback, Action<string> errorCallback) {
			string uri = CHOOSE_HEALTHIER_URI + "user/forgot/";
			string passwordUserJson = "username=" + username;
			yield return StartCoroutine(_PerformRequest<string>(uri, passwordUserJson, callback, errorCallback));
		}

		public IEnumerator UpdateUserPassword(HelperMethods.ChangePasswordRequestObject passwordRequest, Action<string> callback, Action<string> errorCallback)
		{
			string userDataModelJson = "{ \"password\": \"" + passwordRequest.password + "\"}";
			yield return StartCoroutine(UpdateUser(userDataModelJson, callback, errorCallback));
		}

		public IEnumerator LogoutUser(Action<string> callback, Action<string> errorCallback) {
			string uri = CHOOSE_HEALTHIER_URI + "user/logout/";
			string postRequest = "{}";
			yield return StartCoroutine(_PerformRequest<string>(uri, postRequest, callback, errorCallback));
		}

		public IEnumerator RetrieveUserData(Action<string> callback, Action<string> errorCallback) {
			string uri = CHOOSE_HEALTHIER_URI + "users/" + SessionManager.UserUid + "/";
			yield return StartCoroutine(_PerformRequest<string>(uri, null, callback, errorCallback));
		}

		public IEnumerator RetrieveUserSavedItems(Action<string> callback, Action<string> errorCallback) {
			string uri = CHOOSE_HEALTHIER_URI + "db/ProfileActivity/?query=created_at%20%3E%202014-01-01&expand=1&limit=" + REQUEST_LIMIT;
			yield return StartCoroutine(_PerformRequest<string>(uri, null, callback, errorCallback));
		}

		public IEnumerator FlagSaved(Int64 nid, Action<string> callback, Action<string> errorCallback) {
			string uri = CHOOSE_HEALTHIER_URI + "db/ProfileActivity/Save/" + nid.ToString() + "/";
			yield return StartCoroutine(_PerformRequest<string>(uri, null, callback, errorCallback));
		}

		public IEnumerator RetrieveProfileActivityID(Int64 nid, Action<string> callback, Action<string> errorCallback)
		{
			string uri = CHOOSE_HEALTHIER_URI + "db/ProfileActivity/?query=activity%3AActivity%3A" + nid.ToString();
			yield return StartCoroutine(_PerformRequest<string>(uri, null, callback, errorCallback));
		}

		public IEnumerator FlagRecommended(Int64 nid, Action<string> callback, Action<string> errorCallback) {
			string uri = CHOOSE_HEALTHIER_URI + "db/ProfileActivity/Recommended/" + nid.ToString() + "/";
			yield return StartCoroutine(_PerformRequest<string>(uri, null, callback, errorCallback));
		}

		public IEnumerator IsFlaggedSaved(Int64 nid, Action<string> callback, Action<string> errorCallback) {
			string uri = CHOOSE_HEALTHIER_URI + "db/ProfileActivity/?query=activity%3AActivity%3A" + nid.ToString();
			yield return StartCoroutine(_PerformRequest<string>(uri, null, callback, errorCallback));
		}

		public IEnumerator IsFlaggedRecommended(Int64 nid, Action<string> callback, Action<string> errorCallback) {
			string uri = CHOOSE_HEALTHIER_URI + "db/ProfileActivity/?query=recommended%3Atrue%20activity%3AActivity%3A" + nid.ToString();
			yield return StartCoroutine(_PerformRequest<string>(uri, null, callback, errorCallback));
		}

		public IEnumerator GetRecommendedFlagCount(Int64 nid, Action<string> callback, Action<string> errorCallback) {
			string uri = CHOOSE_HEALTHIER_URI + "db/ProfileActivity/count/?query=recommended%3Atrue%20activity%3AActivity%3A" + nid.ToString();
			yield return StartCoroutine(_PerformRequest<string>(uri, null, callback, errorCallback));
		}

		public IEnumerator GetHealthyTipsList(int offset, Action<string> callback, Action<string> errorCallback)
		{
			string uri = CHOOSE_HEALTHIER_URI + "db/HealthyTip/" + "?limit=" + REQUEST_LIMIT;
			yield return StartCoroutine(_PerformRequest<string>(uri, null, callback, errorCallback));
		}

		public IEnumerator GetDetailHealthyTipData(Int64 tip, Action<string> callback, Action<string> errorCallback)
		{
			string uri = CHOOSE_HEALTHIER_URI + "db/HealthyTip/" + tip.ToString() + "/"; 
			yield return StartCoroutine(_PerformRequest<string>(uri, null, callback, errorCallback));
		}

		public IEnumerator GetDetailHealthyTipDataByNid(Int64 nid, Action<string> callback, Action<string> errorCallback)
		{
			string uri = CHOOSE_HEALTHIER_URI + "db/HealthyTip/" + nid.ToString() + "/";
			yield return StartCoroutine(_PerformRequest<string>(uri, null, callback, errorCallback));
		}
		#endregion
	}
}

