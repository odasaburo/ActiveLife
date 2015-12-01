using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using ITT.System;


namespace ITT.Scene
{
	public class OnboardingViewController : System.ITTStateMachine, IScreenViewBase
	{
		private OnboardingViewModel model;
		private RegistrationDataModel tempRegistrationData;

		public static Action OnSignInSuccessHandler;

		/// <summary>
		/// True if we are waiting on a registration call.
		/// </summary>
		private bool _registering = false;

		/// <summary>
		/// True if and only if the onboarding view is open and not in a transitioning state.
		/// </summary>
		private bool _open = false;
		public bool IsOpen { get { return _open; } }

		public enum OnboardingStates
		{
			Registration = 0,
			SignIn = 1
		}

		public static void Spawn(Action successHandler)
		{
			// Only allow one
			if (null != GameObject.FindObjectOfType<OnboardingViewController>())
				return;

			OnSignInSuccessHandler = successHandler;
		}

		void Awake()
		{
			model = GetComponent<OnboardingViewModel>();
			if (null == model)
			{
				throw new MissingComponentException();
			}

			if(null != model)
			{
				base.OnAwake();
			}
		}
		
		void Start()
		{
			AssignButtonDelegates();
		}

		#region State machine methods
		override protected void Update() 
		{
			base.Update();
			if (Input.GetKeyDown(KeyCode.Escape) && _open)
			{
				if(model.registrationStateParent.GetComponent<UIWidget>().alpha == 1) // checks to see if the main state or history state for the profile view is seen 
				{
					OnExitScreen();
				}
				else
				{
					OnEnterRegistrationScreen();
				}
			}
		}
		
		override protected void LateUpdate() 
		{
			base.LateUpdate();
		}
		
		IEnumerator Registration_EnterState()
		{
			model.registrationStateParent.SetActive(true);
			yield break;
		}
		
		IEnumerator Registration_ExitState()
		{
			yield return StartCoroutine(OnHide ());
			model.registrationStateParent.SetActive(false);
			yield break;
		}
		
		IEnumerator SignIn_EnterState()
		{
			model.signInStateParent.SetActive(true);
			yield break;
		}
		
		IEnumerator SignIn_ExitState()
		{
			yield return StartCoroutine(OnHide ());
			model.signInStateParent.SetActive(false);
			yield break;
		}
		
		#endregion

		public IEnumerator OnDisplay()
		{
			if (null == currentState)
			{
				base.OnAwake();
			}


			gameObject.SetActive(true);
			yield return StartCoroutine(HelperMethods.Instance.AnimateIn(gameObject));

			_open = true;

			DetermineSignInButtonStatus();
			DetermineSignUpButtonStatus();

			ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Registration Screen");
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Registration - Registration Screen")
			                                                     .SetEventAction("Enter - Registration Screen")
			                                                     .SetEventLabel("User has begun the registration flow"));
		}
		
		public IEnumerator OnHide() 
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Registration - Registration Screen")
			                                                     .SetEventAction("Exit - Registration Screen")
			                                                     .SetEventLabel("User has exited the registration flow"));
			IScreenViewBase screen = ITTMainSceneManager.Instance.CurrentScreen as IScreenViewBase;
		
			if (null != screen)
			{
				StartCoroutine(screen.ForcedReverseIn());
			}
			yield return StartCoroutine(ForcedReverseOut());
		}

		public IEnumerator ForcedReverseIn()
		{
			gameObject.SetActive(true);
			yield return StartCoroutine(HelperMethods.Instance.ForcedReverseIn(gameObject));
		}
		
		public IEnumerator ForcedReverseOut()
		{
			yield return StartCoroutine(HelperMethods.Instance.ForcedReverseOut(gameObject));
			_open = false;
		}
		
		public void AssignButtonDelegates()
		{
			model.registrationCancelButton.onClick.Add			( new EventDelegate( this, "OnBackPressed"				));
			model.registrationSignUpButton.onClick.Add			( new EventDelegate( this, "OnSignUpPressed"			));
			model.registrationTermsLinkButton.onClick.Add		( new EventDelegate( this, "OnTermsLinkPressed"			));
			model.registrationExistingAccountButton.onClick.Add	( new EventDelegate( this, "OnEnterSignInScreen"		));

			model.registrationEmail.onChange.Add				( new EventDelegate ( this, "DetermineSignUpButtonStatus" ));
			model.registrationPassword.onChange.Add				( new EventDelegate ( this, "DetermineSignUpButtonStatus" ));
			model.registrationConfirmPassword.onChange.Add		( new EventDelegate ( this, "DetermineSignUpButtonStatus" ));
			model.registrationAcceptTerms.onChange.Add			( new EventDelegate ( this, "DetermineSignUpButtonStatus" ));

			model.signInBackButton.onClick.Add					( new EventDelegate( this, "OnEnterRegistrationScreen"	));
			model.signInSubmitButton.onClick.Add				( new EventDelegate( this, "OnSignInPressed"			));

			model.signInUsername.onChange.Add					( new EventDelegate( this, "DetermineSignInButtonStatus" ));
			model.signInPassword.onChange.Add					( new EventDelegate( this, "DetermineSignInButtonStatus" ));
			model.signInForgotPassword.onClick.Add				( new EventDelegate( this, "OnForgotPasswordPressed" ));
		}

		public void DetermineSignInButtonStatus()
		{
			if (model.signInUsername.value != model.signInUsername.defaultText && !string.IsNullOrEmpty(model.signInUsername.value) &&
			    model.signInPassword.value != model.signInPassword.defaultText && !string.IsNullOrEmpty(model.signInPassword.value))
			{
				model.signInSubmitButton.isEnabled = true;
			}
			else
			{
				model.signInSubmitButton.isEnabled = false;
			}
		}

		public void DetermineSignUpButtonStatus()
		{
			if (model.registrationEmail.value != model.registrationEmail.defaultText && !string.IsNullOrEmpty(model.registrationEmail.value) &&
			    model.registrationPassword.value != model.registrationPassword.defaultText && !string.IsNullOrEmpty(model.registrationPassword.value) &&
			    model.registrationConfirmPassword.value != model.registrationConfirmPassword.defaultText && !string.IsNullOrEmpty(model.registrationConfirmPassword.value) &&
			    model.registrationAcceptTerms.value && !_registering)
			{
				model.registrationSignUpButton.isEnabled = true;
			}
			else
			{
				model.registrationSignUpButton.isEnabled = false;
			}
		}

		public void OnBackPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Registration - Registration Screen")
			                                                     .SetEventAction("Click - Registration Back Button")
			                                                     .SetEventLabel("User has clicked on the back button"));
			OnExitScreen();
		}

		public void OnTermsLinkPressed()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Registration - Registration Screen")
			                                                     .SetEventAction("Click - Registration ToS Button")
			                                                     .SetEventLabel("User has clicked on the link to view the terms of service"));
			Application.OpenURL(model.termsAndConditionsLink);
		}

		public void OnExitScreen()
		{
			StartCoroutine(OnHide());
		}

		public void OnEnterRegistrationScreen()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Registration - Registration Screen")
			                                                     .SetEventAction("Enter - Registration Screen")
			                                                     .SetEventLabel("User has begun the registration flow"));
			FadeInOutGameObjects(model.registrationStateParent, model.signInStateParent);
		}

		public void OnEnterSignInScreen()
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogScreen("Sign In Screen");
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Registration - Sign In Screen")
			                                                     .SetEventAction("Enter - Sign In Screen")
			                                                     .SetEventLabel("User has entered the sign in screen during registration"));
			FadeInOutGameObjects(model.signInStateParent,	model.registrationStateParent);
		}

		public void OnForgotPasswordPressed()
		{
			// Ensure valid username entered first
			if (model.signInUsername.value != model.signInUsername.defaultText && !string.IsNullOrEmpty(model.signInUsername.value))
			{
				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Registration - Sign In Screen")
				                                                     .SetEventAction("Click - Sign In Forgot Password Button")
				                                                     .SetEventLabel("User has clicked on the forgot password button"));
				ITTDataCache.Instance.RequestNewPassword(model.signInUsername.value, OnForgotPasswordSuccess, OnForgotPasswordFailure);
			}
			else
			{
				ModalPopupOK.Spawn("Please enter a valid username.");
			}
		}

		public void FadeInOutGameObjects(GameObject fadeInThisObject, GameObject fadeOutThisObject)
		{
			TweenAlpha fadeInComponenet = null;
			if(null == (fadeInComponenet = fadeInThisObject.GetComponent<TweenAlpha>()))
			{
				fadeInComponenet = fadeInThisObject.AddComponent<TweenAlpha>();
			}

			fadeInComponenet.from = 0f;
			fadeInComponenet.to = 1f;
			fadeInComponenet.duration = 0.25f;
			fadeInComponenet.PlayForward();

			TweenAlpha fadeOutComponenet = null;
			if(null == (fadeOutComponenet = fadeOutThisObject.GetComponent<TweenAlpha>()))
			{
				fadeOutComponenet = fadeOutThisObject.AddComponent<TweenAlpha>();
			}

			fadeOutComponenet.from = 0f;
			fadeOutComponenet.to = 1f;
			fadeOutComponenet.duration = 0.25f;
			fadeOutComponenet.PlayReverse();
		}

		#region Button Handlers
		public void OnSignUpPressed()
		{
			if (string.IsNullOrEmpty(model.registrationEmail.value) || string.IsNullOrEmpty(model.registrationPassword.value) || string.IsNullOrEmpty(model.registrationConfirmPassword.value))
			{
				ModalPopupOK.Spawn("Please ensure all fields are filled out.");
			}
			else if (model.registrationPassword.value != model.registrationConfirmPassword.value)
			{
				ModalPopupOK.Spawn("Passwords do not match.");
			}
			else if (!model.registrationAcceptTerms.value)
			{
				ModalPopupOK.Spawn("You must accept the terms and conditions to register a new account.");
			}
			else
			{
				tempRegistrationData = new RegistrationDataModel();
				tempRegistrationData.email = model.registrationEmail.value;
				int atIndex = model.registrationEmail.value.IndexOf('@');
				if (0 > atIndex)
				{
					ModalPopupOK.Spawn("The email address has been entered incorrectly.");
					return;
				}

				tempRegistrationData.username = model.registrationEmail.value;
				tempRegistrationData.password = model.registrationPassword.value;
				tempRegistrationData.legal_accept = (model.registrationAcceptTerms.value ? "1" : "0");

				// TODO: actual call
				_registering = true;
				DetermineSignUpButtonStatus();
				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Registration - Registration Screen")
				                                                     .SetEventAction("Click - Sign Up Button")
				                                                     .SetEventLabel("User has clicked the sign up button. Username: " + model.registrationEmail.value + " Legal: " + tempRegistrationData.legal_accept.ToString()));
				ITTDataCache.Instance.RegisterUser(tempRegistrationData, OnRegistrationSuccess, OnRegistrationFailure);
			}
		}

		public void OnSignInPressed()
		{
			if (!string.IsNullOrEmpty(model.signInUsername.value) && !string.IsNullOrEmpty(model.signInPassword.value))
			{
				string username = model.signInUsername.value;

				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Registration - Sign In Screen")
				                                                     .SetEventAction("Click - Sign In Button")
				                                                     .SetEventLabel("User has clicked on the sign in button. Username: " + model.signInUsername.value));
				ITTDataCache.Instance.LoginUser(username, model.signInPassword.value, OnSignInSuccess, OnSignInFailure);
			}
			else
			{
				ModalPopupOK.Spawn("Please ensure all fields are filled out.");
			}
		}
		#endregion

		#region Network handlers

		public void OnRegistrationSuccess(string json)
		{
			_registering = false;
			// Automatically log them in...? We don't actually care about the JSON passed in.
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Registration - Registration Screen")
			                                                     .SetEventAction("Registration Success - Registration Screen")
			                                                     .SetEventLabel("User has successfully registered and is attempting to login."));
			ITTDataCache.Instance.LoginUser(tempRegistrationData.username, tempRegistrationData.password, OnSignInSuccess, OnSignInFailure);
		}

		public void OnRegistrationFailure(string error)
		{
			_registering = false;
			DetermineSignUpButtonStatus();
			tempRegistrationData = null;

			Debug.LogError("Registration error: " + error);
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Registration - Registration Screen")
			                                                     .SetEventAction("Registration Failure - Registration Screen")
			                                                     .SetEventLabel("User had a registration error: " + error));
			if (error.Contains("406"))
			{
				// Get the 406 message
				string[] parts = error.Split(':');
				string msg = parts[parts.Length-1];
				ModalPopupOK.Spawn(msg);
			}
			else if (error.Contains("409") || error.ToLower().Contains("already taken"))
			{
				// Technically this error means the user name is in use, but since we don't have a username field... :/
				ModalPopupOK.Spawn("This email address is already in use.");
			}
			else if (error.Contains(HelperMethods.Instance.Error_NetworkRadioOff) || error.Contains(HelperMethods.Instance.Error_NetworkTimeOut))
			{
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection.", () => {
					StartCoroutine(OnHide());
				});
			}
			else if (!error.Contains("200"))
			{
				ModalPopupOK.Spawn("An error occurred. Error: " + error);
			}
			else
			{
				ModalPopupOK.Spawn("An error occurred. Please try again later.");
			}
		}

		public void OnSignInSuccess(string json)
		{
			try
			{

				JsonFx.Json.JsonReaderSettings jsonSettings = new JsonFx.Json.JsonReaderSettings();
				jsonSettings.TypeHintName = "__type";
				JsonFx.Json.JsonReader jsonReader = new JsonFx.Json.JsonReader(json, jsonSettings);
				UserDataModel loginResponse = jsonReader.Deserialize<UserDataModel>();

				ITTDataCache.Instance.UpdateSessionManager(loginResponse);

				ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
				                                                     .SetEventCategory("Registration - Sign In Screen")
				                                                     .SetEventAction("Sign In Success - Sign In Screen")
				                                                     .SetEventLabel("User has successfully logged into their account."));
				if (null != OnSignInSuccessHandler)
				{
					OnSignInSuccessHandler();
					OnSignInSuccessHandler = null;
				}

				StartCoroutine(OnHide());
			}
			catch(Exception ex)
			{
				Debug.LogError("OnSignInSuccess error. " + ex.Message);
			}
		}

		public void OnSignInFailure(string error)
		{
			Debug.LogError("Sign in error: " + error);
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Registration - Sign In Screen")
			                                                     .SetEventAction("Sign In Failure - Sign In Screen")
			                                                     .SetEventLabel("User had a sign in error: " + error));
			if (error.Contains("401"))
			{
				ModalPopupOK.Spawn("The email address and/or password you entered is incorrect.");
			}
			else if (error.Contains(HelperMethods.Instance.Error_NetworkRadioOff) || error.Contains(HelperMethods.Instance.Error_NetworkTimeOut))
			{
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection.", () => {
					StartCoroutine(OnHide());
				});
			}
			else
			{
				ModalPopupOK.Spawn("Can't Login. Verify that your email and/or password is correct and try again.");
			}
		}

		public void OnForgotPasswordSuccess(string json)
		{
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Registration - Sign In Screen")
			                                                     .SetEventAction("Forgot Password Success - Sign In Screen")
			                                                     .SetEventLabel("User has successfully requested a password reset"));
			ModalPopupOK.Spawn("An email has been sent with a temporary password.");
		}

		public void OnForgotPasswordFailure(string error)
		{
			string message = "";
			if (error.Contains("404"))
			{
				message = "Username not found.";
			}
			else if (error.Contains(HelperMethods.Instance.Error_NetworkRadioOff) || error.Contains(HelperMethods.Instance.Error_NetworkTimeOut))
			{
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.", () => {
					StartCoroutine(OnHide());
				});
			}
			else
			{
				message = error;
			}

			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Registration - Sign In Screen")
			                                                     .SetEventAction("Forgot Password failure - Sign In Screen")
			                                                     .SetEventLabel("User had a forgot password error: " + message));
			ModalPopupOK.Spawn(message);
		}

		#endregion
	}
}