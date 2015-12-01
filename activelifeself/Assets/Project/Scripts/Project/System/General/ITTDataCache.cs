using UnityEngine;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using JsonFx.Json;
using System.IO;
using System.Web;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ITT.System
{
	
	#region Delegate Members
	public delegate void DataCallbackCommon(DataEntryBase data);
	#endregion

	#region Data Cache Index Enums
	public enum DataCacheIndices { ACTIVITY_LIST = 0, SPONSOR_LIST, USER_PROFILE, USER_FLAGS, LOCATION, TIPS, TIPTRACKER, RECOMMENDATIONS, RECOMMENDATIONTRACKER, MAX_INDEX };
	#endregion

	#region Data Entries
	[global::System.Serializable]
	public abstract class DataEntryBase 
	{
		public static readonly int RefreshTimeInMinutes = 30;
		
		protected DateTime Timestamp;
		protected bool isWaitingOnServer = false;
		[global::System.NonSerialized]
		public DataCallbackCommon ResponseSuccessCallback;
		[global::System.NonSerialized]
		public Action<string> ResponseFailureCallback;
		public bool IsTimeToRefresh()
		{
			return DateTime.UtcNow.Subtract(Timestamp).TotalMinutes >= RefreshTimeInMinutes && !isWaitingOnServer;
		}
		// Only to be called when the network manager re-establishes a connection
		public void ResetWaitingOnServer()
		{
			isWaitingOnServer = false;
		}
		public virtual void RequestData(bool force = false) { }
		public virtual void RefreshData() { }
		public virtual void OverwriteData(object data, bool invokeCallback = true) {}
		public virtual void AppendData<T>(T append) {}
		public virtual void Clear() {}
	}
	[global::System.Serializable]
	public class DataEntry<T> : DataEntryBase where T : class
	{
		protected virtual void PopulateData(string json) 
		{ 
			isWaitingOnServer = false; 
		}
		protected virtual void OnResponseFailure(string error)
		{ 
			Debug.LogError(this.GetType().Name + "OnResponseFailure: " + error);
			if (error.Contains(HelperMethods.Instance.Error_NetworkRadioOff) || error.Contains(HelperMethods.Instance.Error_NetworkTimeOut))
			{
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.");
			}
			isWaitingOnServer = false; 
			Timestamp = DateTime.MinValue;
			if (null != ResponseFailureCallback)
				ResponseFailureCallback(error);
		}

		protected T _data;
		public T Data
		{
			get { return _data; }
			protected set
			{
				_data = value;
				Timestamp = DateTime.UtcNow;
				if (null != _data && null != ResponseSuccessCallback)
				{
					ResponseSuccessCallback(this);
				}
				isWaitingOnServer = false;
			}
		}
		
		public override void OverwriteData (object data, bool invokeCallback = true)
		{
			if (null != data as T)
			{
				if (invokeCallback)
				{
					Data = data as T;
				}
				else
				{
					_data = data as T;
				}
			}
			else
			{
				Debug.LogError("OverwriteData error: parameter is not of type " + typeof(T).Name);
			}
		}
		
		public virtual bool IsValid()
		{
			return null != Data;
		}
		
		public DataEntry()
		{
			Timestamp = DateTime.MinValue;
		}	
		
		public override void Clear ()
		{
			Data = null;
		}
	}
	
	#region ActivityList DataEntry
	[global::System.Serializable]
	public class ActivityList : DataEntry<ActivityDataModel[]>
	{
		private int _dayCount = 0;
		private const int NUMBER_OF_ATTEMPTS = 5;

		protected override void PopulateData (string json)
		{
			try
			{
				MasterBaseCardData masterCardData = JsonReader.Deserialize<MasterBaseCardData>(json);
				Data = masterCardData.results;
			}
			catch
			{
				Debug.LogError("ActivityList: error populating object");
			}
			
			base.PopulateData(json);
		}
		
		public override bool IsValid ()
		{
			return null != Data && Data.Length != 0;
		}
		
		public override void RequestData(bool force = false)
		{
			if ((force || null == Data) && !isWaitingOnServer)
			{
				AttemptDataRetrieval();
				isWaitingOnServer = true;
			}
			else if (null != ResponseSuccessCallback)
			{
				ResponseSuccessCallback(this);
			}
		}
		
		public override void RefreshData ()
		{
			Timestamp = DateTime.UtcNow;
			RequestData(true);
		}
		
		private void RequestItemsForDay(DateTime date)
		{
			ITTFilterRequest dayFilter = new ITTFilterRequest();
			dayFilter.AddFilterValue(new DistanceValue());
			dayFilter.AddFilterValue(new MinDateValue(date));
			dayFilter.AddFilterValue(new MaxDateValue(date.Date));
			ITTDataCache.Instance.RequestCombinedActivities(this.ReceivedSingleDaysActivities, this.OnResponseFailure, dayFilter);
		}

		private void AttemptDataRetrieval()
		{
			if (_dayCount < NUMBER_OF_ATTEMPTS)
			{
				RequestItemsForDay(DateTime.Today.AddDays(_dayCount));
				_dayCount++;
			}
			else 
			{
				if (null != ResponseFailureCallback)
					ResponseFailureCallback(HelperMethods.Instance.Error_EmptyResults);
			}
				
		}

		private void ReceivedSingleDaysActivities(string json)
		{
			try
			{
				MasterBaseCardData masterCardData = JsonReader.Deserialize<MasterBaseCardData>(json);
				ActivityDataModel[] list = masterCardData.results;
				if (list.Any())
				{
					_dayCount = 0;
					AppendData<ActivityDataModel[]>(list);
				} 
				else
				{
					if (null != ResponseFailureCallback)
						ResponseFailureCallback(HelperMethods.Instance.Error_EmptyResults);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("Error parsing single day's activities: " + e.Message);
			}

			isWaitingOnServer = false;
		}
		
		public override void AppendData<T>(T append)
		{
			ActivityDataModel[] appendArray = append as ActivityDataModel[];
			if (null == appendArray)
				return;

			// If we have no activities yet, start the array with this one
			if (null == Data)
			{
				if (DateTime.MinValue == appendArray[0].dateTime)
					appendArray.ToList().ForEach(x => x.ParseDateString());

				Data = appendArray;
			}
			else
			{
				// Append, but don't dupe.
				List<ActivityDataModel> appendList = appendArray.ToList();
				appendList.ForEach(x => x.ParseDateString());
				List<ActivityDataModel> itemsNotInList = 
					appendList.FindAll(x => 
					                   {
						return !Data.Any(y => x.id == y.id);
					});
				
				if (itemsNotInList.Count != 0)
				{
					itemsNotInList.ForEach(x => { if (DateTime.MinValue == x.dateTime) x.ParseDateString(); } );
					List<ActivityDataModel> tempList = Data.ToList();
					
					tempList.AddRange(itemsNotInList); // This list doesn't need to be sorted: we do that in DynamicScrollView.
					Data = tempList.ToArray();
				}
			}
		}
	}
	#endregion
	
	#region SponsorList DataEntry
	[global::System.Serializable]
	public class SponsorList : DataEntry<SponsorDataModel[]>
	{
		protected override void PopulateData (string json)
		{
			try
			{
				MasterSponsorCardData masterSponsorData = JsonReader.Deserialize<MasterSponsorCardData>(json);
				Data = masterSponsorData.results;
			}
			catch
			{
				Debug.LogError("SponsorList: bad JSON");
				
			}
			base.PopulateData (json);
		}

		protected override void OnResponseFailure (string error)
		{
			base.OnResponseFailure (error);
		}

		public override void RequestData(bool force = false)
		{
			if ((force || null == Data) && !isWaitingOnServer)
			{
				ITTDataCache.Instance.RequestSponsorList(this.PopulateData, this.OnResponseFailure);
				
				isWaitingOnServer = true;
			}
			else if (null != ResponseSuccessCallback)
				ResponseSuccessCallback(this);
		}
		
		public override void RefreshData ()
		{
			RequestData(true);
		}
		
	}
	#endregion
	
	#region User Profile
	[global::System.Serializable]
	public class UserProfile : DataEntry<UserDataModel>
	{
		protected override void PopulateData(string json)
		{
			try
			{
				Data = JsonReader.Deserialize<UserDataModel>(json);
			}
			catch
			{
				Debug.LogError("UserData: invalid JSON");
			}
		}

		protected override void OnResponseFailure (string error)
		{
			base.OnResponseFailure (error);
		}

		public override void RequestData(bool force)
		{
			if (!ITTDataCache.Instance.HasSessionCredentials)
				return;
			
			if ((force || null == Data) && !isWaitingOnServer)
			{
				ITTDataCache.Instance.RetrieveUserData(this.PopulateData, this.OnResponseFailure);
				isWaitingOnServer = true;
			}
			else if (null != ResponseSuccessCallback)
				ResponseSuccessCallback(this);
		}	
	}
	#endregion
	
	#region UserFlags entry
	[Serializable]
	public class UserFlagsEntry : DataEntry<HelperMethods.UserFlags>
	{
		
		public override void RequestData(bool force)
		{
			if (null != ResponseSuccessCallback)
				ResponseSuccessCallback(this);
		}
	}
	
	#endregion
	
	#region LocationData entry
	[Serializable]
	public class LocationDataEntry : DataEntry<LocationDataModel>
	{		
		public override void RequestData(bool force)
		{
			if (null != ResponseSuccessCallback)
				ResponseSuccessCallback(this);
		}		
	}
	
	#endregion

	#region Tips/Notifications
	[global::System.Serializable]
	public class TipsEntry : DataEntry<ShortTips[]>
	{
		protected override void PopulateData(string json) 
		{
			try
			{
				MasterShortTips shortResults = JsonReader.Deserialize<MasterShortTips>(json);
				if(shortResults.results.Length > 0){
					Data = shortResults.results;
				}
				else
				{
					Debug.LogError("TipsEntry: results empty from server");
					return;
				}
			}
			catch
			{
				Debug.LogError("TipsEntry: error populating object");
			}
			
			base.PopulateData(json);
		}

		public override void RequestData(bool force = false)
		{
			if ((force || null == Data) && !isWaitingOnServer)
			{
				ITTDataCache.Instance.GetHealthyTipsList(0, this.PopulateData, this.OnResponseFailure);
				
				isWaitingOnServer = true;
			}
			else if (null != ResponseSuccessCallback)
				ResponseSuccessCallback(this);
		}
	}
	[global::System.Serializable]
	public class TipTrackerEntry : DataEntry<HelperMethods.TipTracker>
	{
		public override void RequestData(bool force)
		{
			if (null != ResponseSuccessCallback)
				ResponseSuccessCallback(this);
		}
	}

	[global::System.Serializable]
	public class RecommendationEntry : DataEntry<RecommendationNotification[]>
	{
		public override void RequestData (bool force)
		{
			Data = Data;
		}
	}
	[global::System.Serializable]
	public class RecommendationTrackerEntry : DataEntry<HelperMethods.RecommendationTracker>
	{
		public override void RequestData(bool force)
		{
			if (null != ResponseSuccessCallback)
				ResponseSuccessCallback(this);
		}
	}

	#endregion

	#endregion
	
	public class ITTDataCache : ITTSingleton<ITTDataCache> 
	{
		protected ITTDataCache() {}
		
		#region Static Variables
		// If it's been longer than this since our last pull on any data collection, pull a fresh copy.
		/* * * * * * * * * * * * * * * * * * * * * * * * *
		 * 					    NOTE!					 *
		 * 		  Timestamps will always be in UTC.		 *
		 * * * * * * * * * * * * * * * * * * * * * * * * */
		public readonly string DataCacheFilename = "cache.bin";
		public readonly string SessionFilename = "session.bin";
		public static bool RefreshOnResume = true;
		private bool _isInitialized = false;
		public bool IsInitialized 
		{ 
			get {
				return _isInitialized;
				}
		}
		#endregion
		
		public static string GetFilepath()
		{
			return Application.persistentDataPath + "/_datacache/";
		}

		void Awake()
		{
			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
			} 
		}

		#region DataBlob declaration
		[global::System.Serializable]
		public class DataBlob
		{
			
			#region Data Models
			protected List<DataEntryBase> _dataEntries;
			#endregion
			
			#region Constructor
			public DataBlob()
			{
				int size = (int)DataCacheIndices.MAX_INDEX;
				_dataEntries = new List<DataEntryBase>(size);
				
				// Add all data entry types here
				_dataEntries.Insert((int)DataCacheIndices.ACTIVITY_LIST, new ActivityList());
				_dataEntries.Insert((int)DataCacheIndices.SPONSOR_LIST, new SponsorList());
				_dataEntries.Insert((int)DataCacheIndices.USER_PROFILE, new UserProfile());
				_dataEntries.Insert((int)DataCacheIndices.USER_FLAGS, new UserFlagsEntry());
				_dataEntries.Insert((int)DataCacheIndices.LOCATION, new LocationDataEntry());
				_dataEntries.Insert((int)DataCacheIndices.TIPS, new TipsEntry());
				_dataEntries.Insert((int)DataCacheIndices.TIPTRACKER, new TipTrackerEntry());
				_dataEntries.Insert((int)DataCacheIndices.RECOMMENDATIONS, new RecommendationEntry());
				_dataEntries.Insert((int)DataCacheIndices.RECOMMENDATIONTRACKER, new RecommendationTrackerEntry());
			}
			#endregion
			
			#region Accessors
			public void GetDataEntry(int index, DataCallbackCommon callback, Action<string> errorCallback = null)
			{
				if (index < 0 || index >= _dataEntries.Count)
					return;

				_dataEntries[index].ResponseSuccessCallback -= callback;
				_dataEntries[index].ResponseSuccessCallback += callback;
				if (null != errorCallback)
				{
					_dataEntries[index].ResponseFailureCallback -= errorCallback;
					_dataEntries[index].ResponseFailureCallback += errorCallback;
				}
				_dataEntries[index].RequestData();

			}

			public void RemoveCallbackFromEntry(int index, DataCallbackCommon callback)
			{
				if (index < 0 || index >= _dataEntries.Count)
					return;
				_dataEntries[index].ResponseSuccessCallback -= callback;
			}

			public void UpdateDataEntry(int index, object item, bool invokeCallback = true)
			{
				if (index < 0 || index >= _dataEntries.Count)
					return;

				_dataEntries[index].OverwriteData(item, invokeCallback);
			}
			
			public void AppendDataEntry<T>(int index, T append)
			{
				if (index < 0 || index >= _dataEntries.Count)
					return;

				_dataEntries[index].AppendData<T>(append);
			}
			
			public void RefreshDataEntry(int index)
			{
				if (index < 0 || index >= _dataEntries.Count)
					return;

				_dataEntries[index].RequestData(true);
			}
			
			public void ClearDataEntry(int index)
			{
				if (index < 0 || index >= _dataEntries.Count)
					return;

				_dataEntries[index].Clear();
			}
			#endregion
			
			#region Methods
			public void RefreshAllData(bool force = false)
			{
				if (false == RefreshOnResume)
					return;
				
				foreach (DataEntryBase entry in _dataEntries)
				{
					if (entry.IsTimeToRefresh() || force)
					{
						entry.RefreshData();
					}
				}
			}

			public void ResetWaitOnServerForAllEntries()
			{
				foreach (DataEntryBase entry in _dataEntries)
				{
					entry.ResetWaitingOnServer();
				}
			}
			#endregion
		}
		#endregion
		
		#region Members
		private DataBlob _dataBlob;
		private ITTNetworkManager _networkManager;
		public delegate void NetworkRecovered();
		public event NetworkRecovered onNetworkRecovered;
		
		public bool HasSessionCredentials
		{
			get
			{
				return !string.IsNullOrEmpty(_networkManager.SessionManager.UserToken);
			}
		}
		
		public bool HasData
		{
			get
			{
				return null != Data;
			}
		}

		public bool HasNetworkConnection
		{
			get 
			{
				return !_networkManager.connectionError;
			}
		}
		#endregion
		
		#region Accessors 
		public DataBlob Data
		{
			get { return _dataBlob; }
			private set { _dataBlob = value; }
		}
		#endregion
		
		#region Methods
		void LateUpdate()
		{
			_dataBlob.RefreshAllData();

			if (_networkManager.networkRecovered)
			{
				_networkManager.networkRecovered = false;
				if (null != onNetworkRecovered)
				{
					onNetworkRecovered();
				}
			}
		}

		public void Initialize()
		{
			#if !UNITY_EDITOR
			RefreshOnResume = true;
			#endif
			_isInitialized = true;
			_networkManager = gameObject.AddComponent<ITTNetworkManager>();
			_networkManager.SessionManager = new ITTSessionBlob();
			
			// Try pulling data from the previous session first.
			DeserializeDataBlob();
			DeserializeSession();

		}

		public void FunnelCoroutine(IEnumerator co)
		{
			StartCoroutine(co);
		}

		void OnApplicationPause(bool pause)
		{
			if (pause)
			{
				SerializeDataBlob();
				SerializeSession();
			}
			else
			{
				if (_isInitialized) 
					_dataBlob.RefreshAllData(true);

			}
		}

		void OnApplicationQuit()
		{
			SerializeDataBlob();
			SerializeSession();
		}
		
		public void ClearSessionData()
		{
			_networkManager.SessionManager.Reset();
			if (Directory.Exists(GetFilepath()) && File.Exists(GetFilepath() + SessionFilename))
			{
				File.Delete(GetFilepath() + SessionFilename);
			}
		}
		
		private void InvalidateSession(string error)
		{
			ClearSessionData();
		}
		#endregion
		
		#region Network Calls
		private bool _NetworkOnline(Action<string> errorCallback)
		{
			if (_networkManager.connectionError)
			{
				if (null != errorCallback)
				{
					errorCallback(HelperMethods.Instance.Error_NetworkTimeOut);
					return false;
				}
			}
			return true;
		}
		public void RegisterUser(RegistrationDataModel rdm, Action<string> callback, Action<string> errorCallback) 
		{
			StartCoroutine(_networkManager.RegisterUser(rdm, callback, errorCallback));
		}
		
		public void LoginUser(string username, string password, Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.LoginUser(username, password, callback, errorCallback));
		}
		
		public void UpdateUserEmail(HelperMethods.ChangeEmailRequestObject emailRequest, Action<string> callback, Action<string> errorCallback) 
		{

			StartCoroutine(_networkManager.UpdateUserEmail(emailRequest, callback, errorCallback));
		}
		
		public void RequestNewPassword(string username, Action<string> callback, Action<string> errorCallback) 
		{
			StartCoroutine(_networkManager.RequestNewPassword(username, callback, errorCallback));
		}

		public void ChangePassword(HelperMethods.ChangePasswordRequestObject passwordRequest, Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.UpdateUserPassword(passwordRequest, callback, errorCallback));
		}

		public void RetrieveUserData(Action<string> callback, Action<string> errorCallback) 
		{
			StartCoroutine(_networkManager.RetrieveUserData(callback, errorCallback));
		}
		
		public void RequestCombinedActivities(Action<string> callback, Action<string> errorCallback, ITTFilterRequest filter = null)
		{
			LocationDataModel requestLocationData = RetrieveLocationData();
			if (requestLocationData == null) {
				requestLocationData = DefaultLocationData();
			}

			//	TODO: Add conversion to lat long call and store it here if lat long is missing
			StartCoroutine(_networkManager.RequestCombinedActivitiesDeals(requestLocationData.lastLatitude, requestLocationData.lastLongitude, filter ?? new ITTFilterRequest(), callback, errorCallback));	
		}
		
		public void RequestSponsorList(Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.RequestSponsors(callback, errorCallback));
		}
		
		public void StartDownloadImage(string imageUrl, Action<Texture2D> callback, Action<string> errorCallback)
		{
			_networkManager.StartDownloadImage(imageUrl, callback, errorCallback);
		}
		
		public void LogoutUser(Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.LogoutUser(callback, errorCallback));
		}
		
		public void RetrieveDetailData(DetailType detailType, Int64 id, Action<string> callback, Action<string> errorCallback)
		{
			if (_NetworkOnline(errorCallback))
				StartCoroutine(_networkManager.RetrieveDetailData(detailType, id, callback, errorCallback));
		}
		
		public void FlagSaved(Int64 id, Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.FlagSaved(id, callback, errorCallback));
		}
		
		public void FlagRecommended(Int64 id, Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.FlagRecommended(id, callback, errorCallback));
		}

		public void RetrieveProfileActivityID(Int64 id, Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.RetrieveProfileActivityID(id, callback, errorCallback));
		}

		public void IsFlaggedSaved(Int64 id, Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.IsFlaggedSaved(id, callback, errorCallback));
		}
		
		public void IsFlaggedRecommended(Int64 id, Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.IsFlaggedRecommended(id, callback, errorCallback));
		}
		
		public void GetRecommendedFlagCount(Int64 id, Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.GetRecommendedFlagCount(id, callback, errorCallback));
		}
		
		public void GetStaticMapImage(string location, Action<byte[]> callback, Action<string> errorCallback) 
		{
			StartCoroutine(_networkManager.GetStaticMapImage(location, callback, errorCallback));
		}
		
		public void RetrieveUserSavedItems(Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.RetrieveUserSavedItems(callback, errorCallback));
		}

		public void GetHealthyTipsList(int offset, Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.GetHealthyTipsList(offset, callback, errorCallback));
		}

		public void GetDetailHealthyTipData(Int64 tip, Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.GetDetailHealthyTipData(tip, callback, errorCallback));
		}

		public void GetDetailHealthyTipDataByNid(Int64 nid, Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.GetDetailHealthyTipDataByNid(nid, callback, errorCallback));
		}

		public void GetGeoLocationData(string addressString, Action<string> callback, Action<string> errorCallback)
		{
			StartCoroutine(_networkManager.GetGeoLocationData(addressString, callback, errorCallback));
		}
		#endregion
		
		
		#region Session accessors
		
		// Override for sign in
		public void UpdateSessionManager(UserDataModel loginResponse)
		{
			if (null != loginResponse)
			{
				_networkManager.SessionManager.UpdateUserToken(loginResponse.Session_Token);
				_networkManager.SessionManager.UpdateUserUid(loginResponse.Id.ToString());
				
				Data.UpdateDataEntry((int)DataCacheIndices.USER_PROFILE, loginResponse);
			}
			else
			{
				Debug.LogError("Error with login response (LoginResponseObject == null)");
			}
		}
		
		#endregion
		
		#region Serialization methods
		private void DeserializeDataBlob()
		{
			Stream stream = null;
			try
			{
				if (Directory.Exists(GetFilepath()) && File.Exists(GetFilepath() + DataCacheFilename))
				{
					stream = File.Open(GetFilepath() + DataCacheFilename, FileMode.Open);
					BinaryFormatter bformatter = new BinaryFormatter();
					bformatter.Binder = new VersionDeserializationBinder();
					_dataBlob = bformatter.Deserialize(stream) as DataBlob;
					stream.Close();
				}
				else
				{
					_dataBlob = new DataBlob();
				}
			}
			catch (Exception e)
			{
				Debug.LogError("DataCache: Error deserializing data blob: " + e.Message);
				if (File.Exists(GetFilepath() + DataCacheFilename))
				{
					File.Delete(GetFilepath() + DataCacheFilename);
				}
				
				_dataBlob = new DataBlob();
			}
			finally
			{
				if (null != stream)
					stream.Close();
			}
		}
		
		public void SerializeDataBlob()
		{
			Stream stream = null;
			try
			{
				if (!Directory.Exists(GetFilepath()))
				{
					Directory.CreateDirectory(GetFilepath());
				}
				stream = File.Open(GetFilepath() + DataCacheFilename, FileMode.Create);
				BinaryFormatter bformatter = new BinaryFormatter();
				bformatter.Binder = new VersionDeserializationBinder();
				bformatter.Serialize(stream, _dataBlob);
				stream.Close();
			}
			catch (Exception e)
			{
				string errormsg = "DataCache: Error serializing data blob: " + e.Message;
				Debug.LogError(errormsg);

				
				// Delete the file if it was written partially
				if (File.Exists(GetFilepath() + DataCacheFilename))
				{
					File.Delete(GetFilepath() + DataCacheFilename);
				}
			}
			finally
			{
				if (null != stream)
					stream.Close();
			}			
		}
		
		private void DeserializeSession()
		{
			Stream stream = null;
			try
			{
				if (Directory.Exists(GetFilepath()) && File.Exists(GetFilepath() + SessionFilename))
				{
					stream = File.Open(GetFilepath() + SessionFilename, FileMode.Open);
					BinaryFormatter bformatter = new BinaryFormatter();
					bformatter.Binder = new VersionDeserializationBinder();
					_networkManager.SessionManager = bformatter.Deserialize(stream) as ITTSessionBlob;
					stream.Close();
				}
			}
			catch (Exception e)
			{
				Debug.LogError("DataCache: Error deserializing session blob: " + e.Message);
				if (File.Exists(GetFilepath() + SessionFilename))
				{
					File.Delete(GetFilepath() + SessionFilename);
				}
			}
			finally
			{
				if (null != stream)
					stream.Close();
			}
		}
		
		public void SerializeSession()
		{
			Stream stream = null;
			if (!Directory.Exists(GetFilepath()))
			{
				Directory.CreateDirectory(GetFilepath());
			}
			try	
			{
				stream = File.Open(GetFilepath() + SessionFilename, FileMode.Create);
				BinaryFormatter bformatter = new BinaryFormatter();
				bformatter.Binder = new VersionDeserializationBinder();
				bformatter.Serialize(stream, _networkManager.SessionManager);
			}
			catch (Exception e)
			{
				Debug.LogError("DataCache: Error serializing session blob: " + e.Message);
				
				// Delete the file if it was written partially
				if (File.Exists(GetFilepath() + SessionFilename))
				{
					File.Delete(GetFilepath() + SessionFilename);
				}
			}
			finally
			{
				if (null != stream)
					stream.Close();
			}

			
		}
		#endregion
		
		#region Location Methods
        public IEnumerator UpdateLocation(Action callback, Action<string> errorCallback, UserFlagsEntry userFlags = null) 
		{
			if (!Input.location.isEnabledByUser)
			{
				errorCallback("Location service is not activated.  Please check settings and try again.");
				yield break;
			}

			Input.location.Start();

			int maxWait = 20;
			while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0) 
			{
				yield return new WaitForSeconds(1);
				maxWait--;
			}
			
			if (maxWait < 1) 
			{
				if (null != errorCallback)
				{
					errorCallback("Location service timed out. Please try again.");
					yield break;
				}
			}
			
			if (Input.location.status == LocationServiceStatus.Failed) 
			{
				if (null != errorCallback)
				{
					errorCallback("Location service is not activated. Please check settings and try again.");
					yield break;
				}
			} 
			else 
			{
                if (userFlags == null)
                    ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.USER_FLAGS, HelperMethods.Instance.FlagCurrentLocation);
                else
                    HelperMethods.Instance.FlagCurrentLocation(userFlags);

				Debug.Log("Retrieved Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + 
				          " " + Input.location.lastData.altitude + " " + Input.location.lastData.timestamp);
				
				LocationDataModel lastLocationData = new LocationDataModel();
				lastLocationData.lat = Input.location.lastData.latitude;
				lastLocationData.lon = Input.location.lastData.longitude;
				lastLocationData.lastAltitude = Input.location.lastData.altitude;
				lastLocationData.locationTimeStamp = Input.location.lastData.timestamp;
				
				_dataBlob.UpdateDataEntry((int)DataCacheIndices.LOCATION, lastLocationData);
				
				Input.location.Stop();

				if (null != callback)
				{
					callback();
				}
			}
		}
		
		public LocationDataModel RetrieveLocationData()
		{
			LocationDataModel latestLocation = null;
			
			_dataBlob.GetDataEntry((int)DataCacheIndices.LOCATION, data => {
				LocationDataEntry info = data as LocationDataEntry;
				if (null == info) return;
				if (info.Data != null) {
					latestLocation = new LocationDataModel();
					latestLocation.lat = info.Data.lastLatitude;
					latestLocation.lon = info.Data.lastLongitude;
					latestLocation.lastAltitude = info.Data.lastAltitude;
					latestLocation.locationTimeStamp = info.Data.locationTimeStamp;
					latestLocation.cityState = info.Data.cityState;
					latestLocation.zipCode = info.Data.zipCode;
				}
			});

			return latestLocation;
		}

		private static readonly float _latitud = 30.2500f;
		private static readonly float _longitud = -97.7500f;
		private static readonly string _cityState = "Austin,TX";
		public LocationDataModel DefaultLocationData()
		{

			LocationDataModel requestLocationData = new LocationDataModel();
			requestLocationData.lat = _latitud;
			requestLocationData.lon = _longitud;
			requestLocationData.cityState = _cityState;
			return requestLocationData;
		}
		#endregion
	}
}