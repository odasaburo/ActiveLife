using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using ITT.System;


namespace ITT.Scene
{
	public class SettingsViewController : ITTStateMachine, IScreenViewBase 
	{
		public enum SettingsStates
		{
			Main = 0,
			ChangeEmail = 1,
			ChangePassword = 2
		}

		SettingsViewModel model;
		private UserProfile _userProfile;
		#region Unity methods
		void Awake()
		{
			if (null == model)
			{
				model = gameObject.GetComponent<SettingsViewModel>();
			}

			if (null == model)
			{
				throw new MissingComponentException();
			}
		}
		
		void Start()
		{
			AssignButtonDelegates();
		}
		
		public IEnumerator OnDisplay()
		{
			if (null == currentState)
			{
				base.OnAwake();
			}

			if (null == model)
			{
				Awake();
			}

			ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.USER_PROFILE, OnRetrievedUserProfile);
			gameObject.SetActive(true);
			yield return StartCoroutine(HelperMethods.Instance.AnimateIn(gameObject));
			currentState = SettingsStates.Main;
			yield break;
		}
		#endregion

		#region State methods

		override protected void Update() 
		{
			base.Update();
			if (Input.GetKeyDown(KeyCode.Escape))
			{

				if (model.changeEmailStateParent.activeSelf || model.changePasswordStateParent.activeSelf)
				{
					OnChangeEmailBackPressed();
				}
				else
				{
					OnMainBackPressed();
				}

			}
		}
		
		override protected void LateUpdate() 
		{
			base.LateUpdate();
		}
		
		IEnumerator Main_EnterState()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Main Settings Screen");
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Enter - Main Settings Screen")
			                                                     .SetEventLabel("User has entered the main settings screen"));
			model.mainStateParent.SetActive(true);
			yield break;
		}
		
		IEnumerator Main_ExitState()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Exit - Main Settings Screen")
			                                                     .SetEventLabel("User has exited the main settings screen"));
			model.mainStateParent.SetActive(false);
			yield break;
		}
		
		IEnumerator ChangeEmail_EnterState()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Change Email Settings Screen");
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Enter - Change Email Settings Screen")
			                                                     .SetEventLabel("User has entered the change email screen"));
			model.changeEmailStateParent.SetActive(true);
			yield break;
		}
		
		IEnumerator ChangeEmail_ExitState()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Exit - Change Email Settings Screen")
			                                                     .SetEventLabel("User has exited the change email screen"));
			model.changeEmailStateParent.SetActive(false);
			yield break;
		}

		IEnumerator ChangePassword_EnterState()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Change Password Settings Screen");
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Enter - Change Password Settings Screen")
			                                                     .SetEventLabel("User has entered the change password screen"));
			model.changePasswordStateParent.SetActive(true);
			yield break;
		}

		IEnumerator ChangePassword_ExitState()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Exit - Change Password Settings Screen")
			                                                     .SetEventLabel("User has exited the change password screen"));
			model.changePasswordStateParent.SetActive(false);
			yield break;
		}
		#endregion

		public IEnumerator OnHide() 
		{
			yield return StartCoroutine(HelperMethods.Instance.AnimateOut(gameObject));
		}

		public IEnumerator ForcedReverseIn()
		{
			gameObject.SetActive(true);
			yield return StartCoroutine(HelperMethods.Instance.ForcedReverseIn(gameObject));
		}
		
		public IEnumerator ForcedReverseOut()
		{
			yield return StartCoroutine(HelperMethods.Instance.ForcedReverseOut(gameObject));
			gameObject.SetActive(false);
		}
		
		public void AssignButtonDelegates()
		{
			model.mainStatebackButton.onClick.Add			(new EventDelegate(this, "OnMainBackPressed"				));
			model.signOutButton.onClick.Add					(new EventDelegate(this, "OnSignOutPressed"		 			));
			model.changeEmailButton.onClick.Add				(new EventDelegate(this, "OnChangeEmailPressed"		 		));
			model.changePasswordButton.onClick.Add			(new EventDelegate(this, "OnChangePasswordPressed"			));

			model.changeEmailBackButton.onClick.Add			(new EventDelegate(this, "OnChangeEmailBackPressed"			));
			model.changeEmailConfirmButton.onClick.Add		(new EventDelegate(this, "OnChangeEmailConfirmPressed"		));

			model.changePasswordBackButton.onClick.Add  	(new EventDelegate(this, "OnChangeEmailBackPressed"			));
			model.changePasswordConfirmButton.onClick.Add	(new EventDelegate(this, "OnChangePasswordConfirmPressed"	));
		}
		
		public void OnMainBackPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Click - Back Button Main Settings Screen")
			                                                     .SetEventLabel("User has clicked on the back button from the settings screen"));
			StartCoroutine(ITTMainSceneManager.Instance.AttemptAuthenticatedStateChange(ITTMainSceneManager.ITTStates.Profile));
		}
	
		public void OnSignOutPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Click - Sign Out Button Settings Screen")
			                                                     .SetEventLabel("User has clicked on the sign out button from the settings screen"));
			ITTDataCache.Instance.LogoutUser(OnLogoutSuccess, OnLogoutFailure);
		}

		public void OnChangeEmailPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Click - Change Email Button Settings Screen")
			                                                     .SetEventLabel("User has clicked on the change email button from the settings screen"));
			currentState = SettingsStates.ChangeEmail;
		}
		
		public void OnChangeEmailBackPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Click - Back Button")
			                                                     .SetEventLabel("User has clicked on the back button from the settings screen"));
			currentState = SettingsStates.Main;
		}
		
		public void OnChangeEmailConfirmPressed()
		{
			string newEmail = model.newEmailField.value;
			string confirmEmail = model.changeEmailPasswordField.value;
			if (model.newEmailField.defaultText == newEmail || string.IsNullOrEmpty(newEmail) || string.IsNullOrEmpty(Regex.Match(newEmail, ".+@.+[.].+").ToString()))
			{
				ModalPopupOK.Spawn("Please enter a valid email address.");
			}
			else if (model.changeEmailPasswordField.defaultText == confirmEmail || string.IsNullOrEmpty(confirmEmail) || string.IsNullOrEmpty(Regex.Match(confirmEmail, ".+@.+[.].+").ToString()))
			{
				ModalPopupOK.Spawn("Please enter a valid confirmation email address.");
			}
			else if (string.Compare(newEmail, confirmEmail) != 0)
			{
				ModalPopupOK.Spawn("Check that your email addresses match.");
			}
			else
			{
				HelperMethods.ChangeEmailRequestObject emailRequest = new HelperMethods.ChangeEmailRequestObject();
				emailRequest.email = newEmail;

				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Profile - Settings Screen")
				                                                     .SetEventAction("Click - Confirm Button Change Email Screen")
				                                                     .SetEventLabel("User has clicked on the confirm button from the change email screen. Email: " + model.newEmailField.value));
				ITTDataCache.Instance.UpdateUserEmail(emailRequest, OnChangeEmailSuccess, OnChangeEmailFailure);
			}
		}

		public void OnChangePasswordPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Click - Change Password Button Settings Screen")
			                                                     .SetEventLabel("User has clicked on the change password button from the settings screen"));
			currentState = SettingsStates.ChangePassword;
		}

		public void OnChangePasswordConfirmPressed()
		{
			//	TODO: Finish password change logic flow
			string newPassword = model.newPasswordField.value;
			string confirmPassword = model.confirmPasswordField.value;

			if (model.newPasswordField.defaultText == newPassword || string.IsNullOrEmpty(newPassword))
			{
				ModalPopupOK.Spawn("Please enter a new password.");
			}
			else if (model.confirmPasswordField.defaultText == confirmPassword || string.IsNullOrEmpty(newPassword))
			{
				ModalPopupOK.Spawn("Please confirm your password.");
			}
			else if (string.Compare(newPassword, confirmPassword) != 0)
			{
				ModalPopupOK.Spawn("Check that your passwords match.");
			}
			else
			{
				HelperMethods.ChangePasswordRequestObject passwordRequest = new HelperMethods.ChangePasswordRequestObject();
				passwordRequest.password = newPassword;

				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Profile - Settings Screen")
				                                                     .SetEventAction("Click - Confirm Button Change Password Screen")
				                                                     .SetEventLabel("User has clicked on the confirm button from the change password screen"));
				ITTDataCache.Instance.ChangePassword(passwordRequest, OnChangePasswordSuccess, OnChangePasswordFailure);
			}
		}

		#region Network/Data handlers
		public void OnRetrievedUserProfile(DataEntryBase data)
		{
			_userProfile = data as UserProfile;
			if (null == _userProfile)
				return;

			model.nameLabel.text = _userProfile.Data.Username;
		}

		public void OnLogoutSuccess(string json)
		{
			// Response should just be "[true]"
			ITTDataCache.Instance.ClearSessionData();
			model.nameLabel.text = "";
			ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Main;
			ModalPopupOK.Spawn("You are now logged out.");
		}

		public void OnLogoutFailure(string error)
		{
			Debug.LogError("OnLogoutFailure: " + error);
			if (error.Contains(HelperMethods.Instance.Error_NetworkRadioOff) || error.Contains(HelperMethods.Instance.Error_NetworkTimeOut))
			{
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.", () => {
					ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Main;
				});
			}
		}

		public void OnChangeEmailSuccess(string json)
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Change Email Success - Settings Screen")
			                                                     .SetEventLabel("User has successfully changed their email"));
			ModalPopupOK.Spawn("Your email has been updated.", () => {
				OnChangeEmailBackPressed();
			});

		}

		public void OnChangeEmailFailure(string error)
		{
			Debug.LogError("Change email error: " + error);
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Change Email Failure - Settings Screen")
			                                                     .SetEventLabel("User has encountered a change email error: " + error));
			if (error.Contains(HelperMethods.Instance.Error_NetworkRadioOff) || error.Contains(HelperMethods.Instance.Error_NetworkTimeOut))
			{
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.", () => {
					ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Main;
				});
			}
			ModalPopupOK.Spawn("An error occurred. Please try again later.");
		}

		public void OnChangePasswordSuccess(string json)
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Change Password Success - Settings Screen")
			                                                     .SetEventLabel("User has successfully changed their password"));
			ModalPopupOK.Spawn("You have successfully changed your password and will be logged out. Please login with your new credentials.", OnChangePasswordOkPressed);
		}

		public void OnChangePasswordOkPressed()
		{
			OnSignOutPressed();
		}

		public void OnChangePasswordFailure(string error)
		{
			Debug.LogError("Change password failed: " + error);
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Profile - Settings Screen")
			                                                     .SetEventAction("Change Password Failure - Settings Screen")
			                                                     .SetEventLabel("User has encountered a change password error: " + error));
			if (error.Contains(HelperMethods.Instance.Error_NetworkRadioOff) || error.Contains(HelperMethods.Instance.Error_NetworkTimeOut))
			{
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.", () => {
					ITTMainSceneManager.Instance.currentState = ITTMainSceneManager.ITTStates.Main;
				});
			}
		}
		#endregion
	}
}