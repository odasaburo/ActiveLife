using ITT.Scene;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(UIWidget))]
public class PreviousLocationViewController : MonoBehaviour
{
	public UILabel _previousLocationLabel;
	public LocationViewController _locationView;

	void Start()
	{
		UIWidget widget = GetComponent<UIWidget>();
		// Set Anchors
		widget.leftAnchor.target = _locationView.transform;
		widget.leftAnchor.absolute = 0;
		
		widget.rightAnchor.target = _locationView.transform;
		widget.rightAnchor.absolute = 0;

		_previousLocationLabel.leftAnchor.target = _locationView.transform;
		_previousLocationLabel.leftAnchor.absolute = 27;

		_previousLocationLabel.ResetAndUpdateAnchors ();
		widget.ResetAndUpdateAnchors ();

	}

	void OnEnable()
	{
		_previousLocationLabel.ResetAndUpdateAnchors ();
	}

	public void OnSearch()
	{
		string location = _previousLocationLabel.text;
		_locationView.OnSearch (location);
	}
}
