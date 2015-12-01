using UnityEngine;
using System.Collections;

public class LocationViewModel : MonoBehaviour 
{
	public UIInput inputField;
	public UIButton cancelButton;
	public UIButton useCurrentButton;
	// Yes, I know, so much hack. :(
	public MainAppViewModel mainAppViewModel;
	public UIInput inputBox;
	public UILabel recentSearchLabel;
	public GameObject clearButton;
}
