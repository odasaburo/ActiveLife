using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using ITT.System;

namespace ITT.Scene {
	public class ITTMainSceneManager : ITTStateMachine {
		
		public enum ITTStates {
			Tooltip = 0,
			Main = 1,
			Location = 2,
			Filter = 3, 
			FilterResults = 4,
			Profile = 5,
			Settings = 6,
			Onboarding = 7
		}

		#region Extra state stuff
		[HideInInspector]
		public Enum bufferedState;

		public bool DetailCardOpen;
		public bool OverlayIsOpen
		{
			get
			{
				return OnboardingView.IsOpen || DetailCardOpen;
			}
		}

		public IEnumerator AttemptAuthenticatedStateChange(Enum desiredState)
		{
			if (ITTDataCache.Instance.HasSessionCredentials)
			{
				currentState = desiredState;
			}
			else if(!ITTDataCache.Instance.HasSessionCredentials)
			{
				bufferedState = desiredState;
				
				if (null != CurrentScreen)
				{
					IScreenViewBase screen = CurrentScreen as IScreenViewBase;
					if (null != screen)
						StartCoroutine(screen.OnHide());
				}

				OnboardingViewController.Spawn(EnterBufferedState);
				StartCoroutine(OnboardingView.OnDisplay());
			}

			yield break;
		}
		
		public IEnumerator AttemptDetailView(Action successHandler)
		{

			if (ITTDataCache.Instance.HasSessionCredentials)
			{
				if (null != successHandler)
				{
					successHandler();
				}
			}
			else if(!ITTDataCache.Instance.HasSessionCredentials)
			{
				if (null != CurrentScreen)
				{
					IScreenViewBase screen = CurrentScreen as IScreenViewBase;
					if (null != screen)
						StartCoroutine(screen.OnHide());
				}
				
				OnboardingViewController.Spawn(successHandler);
				StartCoroutine(OnboardingView.OnDisplay());
			}

			yield break;
		}
		
		public void EnterBufferedState()
		{
			if (null != bufferedState)
			{
				currentState = bufferedState;
				bufferedState = null;
			}
		}

		
		#endregion
		
		#region Scene Objects 
		
		public MainAppViewController MainView;
		public LocationViewController LocationView;
		public ProfileViewController ProfileView;
		public FilterViewController FilterView;
		public FilterResultsViewController FilterResultsView;
		public SettingsViewController SettingsView;
		public ToolTipViewController TooltipView;
		public OnboardingViewController OnboardingView;
			
		public MonoBehaviour CurrentScreen { get; protected set;}				
		#endregion
		
		#region Static Access
		private static ITTMainSceneManager _instance;
		public static ITTMainSceneManager Instance
		{
			get 
			{
				if (null == _instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(ITTMainSceneManager)) as ITTMainSceneManager;
				}
				
				return _instance;
			}
		}
		
		#endregion

		#region Google Analytics Exception Handling
		private void HandleException(string condition, string stackTrace, LogType type)
		{
			if (type == LogType.Exception)
			{
				StackTrace trace = new StackTrace();
				ITTGoogleAnalytics.Instance.googleAnalytics.LogException(stackTrace + " " + trace.ToString(), true);
			}
		}

		#endregion
		#region General App Behavior
		
		void Awake()
		{
			OnAwake();
		}
		
		override protected void OnAwake()
		{
			base.OnAwake();
			
			
			ITTDataCache.Instance.Initialize();
			FacebookWrapper.Initialize();

			Application.RegisterLogCallback(HandleException);
		}
		
		IEnumerator Start() {
			
			if (!ITTDataCache.Instance.HasData)
				yield return null;
			
			ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.USER_FLAGS, OnRetrievedUserFlags);
		}

		void OnRetrievedUserFlags(DataEntryBase data)
		{
			UserFlagsEntry flags = data as UserFlagsEntry;

			if (null == flags || null == flags.Data)
			{

				StartCoroutine(PlayVideo());
			}
			else
			{
				if(flags.Data.hasSeenTooltip == false)
				{
					StartCoroutine(PlayVideo());
				}
				else
				{
					if (flags.Data.hasToggledLocation == true)
					{
						StartCoroutine(ITTDataCache.Instance.UpdateLocation(null, null, userFlags: flags));
					}
					currentState = ITTMainSceneManager.ITTStates.Main;
				}
			}

			ITTDataCache.Instance.Data.RemoveCallbackFromEntry((int)DataCacheIndices.USER_FLAGS, OnRetrievedUserFlags);

			#if !UNITY_EDITOR
			TouchScreenKeyboard.hideInput = true;
			#endif
		}

		public IEnumerator PlayVideo()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("FTUE - Launch Video")
			                                                     .SetEventAction("Play")
			                                                     .SetEventLabel("First time launch intro video played"));
			Color clearColor = ColorManagementSystemController.GetCurrentTextColor();
			Handheld.PlayFullScreenMovie("ActiveLife_Anim_v3_b.mp4", clearColor, FullScreenMovieControlMode.Hidden, FullScreenMovieScalingMode.Fill);
			currentState = ITTMainSceneManager.ITTStates.Tooltip;
			yield break;
		}

		override protected void Update() {
			base.Update();
		}
		
		override protected void LateUpdate() {
			base.LateUpdate();
		}
		
		#endregion
		
		#region Main App State
		IEnumerator Main_EnterState() {
			CurrentScreen = MainView;
			yield return StartCoroutine(MainView.OnDisplay());

			ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Main Screen");
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Main - Main Screen")
			                                                     .SetEventAction("Enter - Main Screen")
			                                                     .SetEventLabel("User has entered the main screen"));
		}
		
		void Main_Update() {
			
		}
		
		IEnumerator Main_ExitState() {
			yield return StartCoroutine(MainView.OnHide());
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Main - Main Screen")
			                                                     .SetEventAction("Exit - Main Screen")
			                                                     .SetEventLabel("User has exited the main screen"));
			MainView.gameObject.SetActive(false);
		}
		#endregion
		
		#region Tool Tip App State
		IEnumerator Tooltip_EnterState() {
			CurrentScreen = TooltipView;
			yield return StartCoroutine(TooltipView.OnDisplay());
		}
		
		void Tooltip_Update() {
			
		}
		
		IEnumerator Tooltip_ExitState() {
			yield return StartCoroutine(TooltipView.OnHide());
			TooltipView.gameObject.SetActive(false);
		}		
		#endregion
		
		#region Location App State
		IEnumerator Location_EnterState() {
			yield return StartCoroutine(LocationView.OnDisplay());
			ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Location Screen");
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Location - Location Screen")
			                                                     .SetEventAction("Enter - Location Screen")
			                                                     .SetEventLabel("User has entered the location screen"));
			CurrentScreen = LocationView;
		}
		
		void Location_Update() {
			
		}
		
		IEnumerator Location_ExitState() {
			yield return StartCoroutine(LocationView.OnHide());
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Location - Location Screen")
			                                                     .SetEventAction("Exit - Location Screen")
			                                                     .SetEventLabel("User has exited the location screen"));
			LocationView.gameObject.SetActive(false);
		}		
		#endregion
		
		#region Filter App State
		IEnumerator Filter_EnterState() {
			CurrentScreen = FilterView;
			yield return StartCoroutine(FilterView.OnDisplay());
			ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Filter Screen");
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Filter - Filter Screen")
			                                                     .SetEventAction("Enter - Filter Screen")
			                                                     .SetEventLabel("User has entered the filter screen"));
		}
		
		void Filter_Update() {
			
		}
		
		IEnumerator Filter_ExitState() {
			yield return StartCoroutine(FilterView.OnHide());
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Filter - Filter Screen")
			                                                     .SetEventAction("Exit - Filter Screen")
			                                                     .SetEventLabel("User has exited the filter screen"));
			FilterView.gameObject.SetActive(false);
		}		
		#endregion
		
		#region Filter Results App State
		IEnumerator FilterResults_EnterState() {
			CurrentScreen = FilterResultsView;
			yield return StartCoroutine(FilterResultsView.OnDisplay());
			ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Filter Results Screen");
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Filter - Filter Results Screen")
			                                                     .SetEventAction("Enter - Filter Results Screen")
			                                                     .SetEventLabel("User has entered the filter results screen"));
		}
		
		void FilterResults_Update() {
			
		}
		
		IEnumerator FilterResults_ExitState() {
			yield return StartCoroutine(FilterResultsView.OnHide());
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Filter - Filter Results Screen")
			                                                     .SetEventAction("Exit - Filter Results Screen")
			                                                     .SetEventLabel("User has exited the filter results screen"));
			FilterResultsView.gameObject.SetActive(false);
		}		
		#endregion
		
		#region Profile App State
		IEnumerator Profile_EnterState() {
			CurrentScreen = ProfileView;
			yield return StartCoroutine(ProfileView.OnDisplay());
			ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Profile Screen");
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Profile Screen")
			                                                     .SetEventAction("Enter - Profile Screen")
			                                                     .SetEventLabel("User has entered the profile screen"));
		}
		
		void Profile_Update() {
			
		}
		
		IEnumerator Profile_ExitState() {
			yield return StartCoroutine(ProfileView.OnHide());
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Profile Screen")
			                                                     .SetEventAction("Exit - Profile Screen")
			                                                     .SetEventLabel("User has exited the profile screen"));
			ProfileView.gameObject.SetActive(false);
		}
		
		#endregion
		
		#region Settings App State
		IEnumerator Settings_EnterState() {
			CurrentScreen = SettingsView;
			yield return StartCoroutine(SettingsView.OnDisplay());
			ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Profile Settings Screen");
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Profile Settings Screen")
			                                                     .SetEventAction("Enter - Profile Settings Screen")
			                                                     .SetEventLabel("User has entered the profile settings screen"));
		}
		
		void Settings_Update() {
			
		}
		
		IEnumerator Settings_ExitState() {
			yield return StartCoroutine(SettingsView.OnHide());
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Profile Settings Screen")
			                                                     .SetEventAction("Exit - Profile Settings Screen")
			                                                     .SetEventLabel("User has exited the profile settings screen"));
			SettingsView.gameObject.SetActive(false);
		}
		#endregion
	}
}

