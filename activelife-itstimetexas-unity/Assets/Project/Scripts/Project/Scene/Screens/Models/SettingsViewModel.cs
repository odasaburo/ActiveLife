using UnityEngine;
using System.Collections;

public class SettingsViewModel : MonoBehaviour 
{
	#region Main
	public GameObject mainStateParent;
	public UIButton mainStatebackButton;
	public UIButton signOutButton;
	public UIButton changeEmailButton;
	public UIButton changePasswordButton;
	public UILabel nameLabel;
	#endregion

	#region Change Email
	public GameObject changeEmailStateParent;
	public UIInput newEmailField;
	public UIInput changeEmailPasswordField;
	public UIButton changeEmailConfirmButton;
	public UIButton changeEmailBackButton;
	#endregion

	#region Change Password
	public GameObject changePasswordStateParent;
	public UIInput newPasswordField;
	public UIInput confirmPasswordField;
	public UIButton changePasswordConfirmButton;
	public UIButton changePasswordBackButton;
	#endregion
}
