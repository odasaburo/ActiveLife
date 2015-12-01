using UnityEngine;

public class NotificationViewController : MonoBehaviour {

	public bool showNotification;
	public UIWidget scrollViewWidget;

	private NotificationViewModel _model;
	private bool _detailPresent;
	private Vector3 _boxColliderSize;
	private BoxCollider[] _boxColliderList;
	private int _scrollViewHeight; 
	private int _bottomAnchorOffset;

	private const int _TARGET_LEFT = 0;
	private const int _TARGET_BOTTOM = 0;
	private const int _TARGET_RIGHT = 1;
	private const int _FONT_SIZE = 36;


	void Awake()
	{
		_model = gameObject.GetComponent<NotificationViewModel>();
		if (_model == null)
		{
			throw new MissingComponentException();
		}

		_detailPresent = false;
		_scrollViewHeight = scrollViewWidget.height * -1;
	}


	void Start ()
	{
		if( showNotification )
		{
			scrollViewWidget.SetAnchor(_model.topBar);
			scrollViewWidget.leftAnchor.Set(_TARGET_LEFT, -6);
			scrollViewWidget.rightAnchor.Set(_TARGET_RIGHT, 7);
			scrollViewWidget.bottomAnchor.Set(_TARGET_BOTTOM, _scrollViewHeight);
			scrollViewWidget.topAnchor.Set(_TARGET_BOTTOM, 0);

			_boxColliderList = scrollViewWidget.GetComponentsInChildren<BoxCollider>();
			_boxColliderSize = _boxColliderList[0].size;
			_boxColliderList[0].size = Vector3.zero;
			_boxColliderList[1].size = Vector3.zero;

			setNotificationData();
			_model.topBar.SetActive(true);

			UIEventListener.Get(_model.topBar).onClick += PresentNotificationDetail;
			UIEventListener.Get(_model.tapAway).onClick += closeAndDismiss;
		}
	}
	
	
	private void setNotificationData()
	{
		_model.healthyTipLabel.text = "East Side Bikram Yoga";
		_model.descriptionLabel.text = "Winter break is coming up! Use the extra time to play outside with your kids and get in more steps. Try going for a hike or walk in the neighborhood.";
		_model.sponsorLabel.text = "Powered By: H-E-B";
		_model.websiteLabel.text = "www.healthytip.com";
		
		int descriptionLabelHeight = _FONT_SIZE * ( _FONT_SIZE * _model.descriptionLabel.text.Length / _model.bottomDetail.width );
		_bottomAnchorOffset = - ( descriptionLabelHeight + (_model.bottomDetail.height  - _FONT_SIZE) );
	}


	public void PresentNotificationDetail(GameObject go)
	{
		if( !_detailPresent )
		{
			_model.bottomDetail.bottomAnchor.Set(_TARGET_BOTTOM, _bottomAnchorOffset );
			scrollViewWidget.SetAnchor(_model.bottomDetail.gameObject);
			scrollViewWidget.bottomAnchor.Set(_TARGET_BOTTOM, _scrollViewHeight);
			scrollViewWidget.topAnchor.Set(_TARGET_BOTTOM, 0);

			_model.bottomDetail.gameObject.SetActive(true);
			_detailPresent = true;
		}
	}


	public void closeAndDismiss(GameObject go)
	{
		showNotification = false;
		gameObject.SetActive(false);

		scrollViewWidget.SetAnchor((GameObject)null);
		scrollViewWidget.transform.position = Vector3.zero;

		_boxColliderList[0].size = _boxColliderSize;
		_boxColliderList[1].size = _boxColliderSize;
	}
}