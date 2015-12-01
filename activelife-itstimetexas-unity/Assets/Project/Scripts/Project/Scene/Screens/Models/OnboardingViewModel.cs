using UnityEngine;
using System.Collections;

public class OnboardingViewModel : MonoBehaviour 
{
	#region Common items
	#endregion

	#region Registration state items
	public GameObject registrationStateParent;
	public UIButton registrationCancelButton;
	public UIInput registrationEmail;
	public UIInput registrationPassword;
	public UIInput registrationConfirmPassword;
	public UIToggle registrationAcceptTerms;
	public UIButton registrationTermsLinkButton;
	public UIButton registrationSignUpButton;
	public UIButton registrationExistingAccountButton;
	public string termsAndConditionsLink;
		#endregion

	#region Sign In state items
	public GameObject signInStateParent;
	public UIButton signInBackButton;
	public UIInput signInUsername;
	public UIInput signInPassword;
	public UIButton signInForgotPassword;
	public UIButton signInSubmitButton;
	#endregion

}
