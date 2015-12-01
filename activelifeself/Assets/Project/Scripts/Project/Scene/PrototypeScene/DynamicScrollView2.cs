using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ITT.System;
using ITT.Scene;

public class DynamicScrollView2 : MonoBehaviour 
{
    public GameObject loadingIcon;

	#region Scroll views
	private bool _verticalMode = false;
	private float _cardPadding;
	#endregion

	#region Carousel
	private CardCarousel _mainCarousel;
	private CardCarousel _secondaryCarousel; // For the main view. Filters and profile will only use the main carousel.
	#endregion

	#region Selected Day
	public DayOfWeekController dayOfWeekController;
	#endregion

	#region Detail View stuff
	private UIScrollView _detailCardScrollView;
	private UISprite _releaseToCloseSprite;
	private UIRoot _uIRoot;
	private GameObject _cardToPresent;
	private bool _didDragPastReleaseThreshold;
	private GameObject _detailCard;
	
	public float closeDetailCardThreshold = -850;
	public float closeDetailCardThresholdHorizontal = 150;

	private SwipeScripts _swipeScripts;
	#endregion

	#region Misc
	private const string _prefabPath = "Prefabs/Production/";
	public delegate void ActivitiesPopulatedDelegate();
	public ActivitiesPopulatedDelegate OnActivitiesPopulated;
	public delegate void BoolDelegate(bool b);
	private BoolDelegate HideNoResultsCardDelegate;
	private BoolDelegate SetEndOfDayNoResultsCardTextDelegate;
	public NotificationManager _notificationManager;

    private bool isMainCarouselPopulated;
    private bool isMainCarouselPopulateFailed;
	#endregion

	#region Methods

	void Awake()
	{
		UIRoot[] roots = NGUITools.FindActive<UIRoot>();
		if (null != roots[0]) {
			_uIRoot = roots[0];
		}
		_cardToPresent = null;
		_didDragPastReleaseThreshold = false;
		HideCarousel(true);
	}

	void Start()
	{
		if (null != dayOfWeekController)
			dayOfWeekController.OnSelectedDayChangeEvent += OnSelectedDayChanged;
		ITTDataCache.Instance.onNetworkRecovered += OnNetworkRecovered;
	}

	void Update()
	{
		ITTMainSceneManager.Instance.DetailCardOpen = null != _detailCard;
		if (null != _detailCard)
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				ReleaseDetailCardForce(true);
			}
		}
		if (null != _notificationManager) {
			bool enableScrolling = !_notificationManager.IsNotificationOpen ();
			if (null != _mainCarousel ) {
				_mainCarousel.SetScrollingEnabled (enableScrolling);
			}
			if (null != _secondaryCarousel ) {
				_secondaryCarousel.SetScrollingEnabled (enableScrolling);
			}
		}
	}

	public void Init(int width, int height, float cardPadding, BoolDelegate onCarouselSwap, BoolDelegate onShowEndOfDayCard, int carouselLength = -1, bool clearOnRepopulate = true, bool verticalMode = false, bool allowRecommending = false)
	{
		_verticalMode = verticalMode;
		_cardPadding = cardPadding;
		HideNoResultsCardDelegate = onCarouselSwap;
		SetEndOfDayNoResultsCardTextDelegate = onShowEndOfDayCard;

		// Initialize carousels and populate with blank cards
		if (null == _mainCarousel)
		{
			GameObject mainCarouselObject = NGUITools.AddChild(gameObject);
			mainCarouselObject.name = "MainCarousel";
			_mainCarousel = mainCarouselObject.AddComponent<CardCarousel>();
		}
		int dayIndex = (!_verticalMode ? dayOfWeekController.SelectedDay.IndexOfDay : -1);
		_mainCarousel.Init(dayIndex, width, height, cardPadding, PresentDetailCardHelper, carouselLength, clearOnRepopulate, _verticalMode, allowRecommending);
		_mainCarousel.OnNoCardsForSelectedDay = InvokeShowNoResultsCard;
		if (null == _secondaryCarousel)
		{
			GameObject secondaryCarouselObject = NGUITools.AddChild(gameObject);
			secondaryCarouselObject.name = "SecondaryCarousel";
			_secondaryCarousel = secondaryCarouselObject.AddComponent<CardCarousel>();
		}
		if (!_verticalMode)
		{
			// If the next day index is out of bounds, init with prev just so he's initialized with something.
			int curIndex = dayOfWeekController.SelectedDay.IndexOfDay;
			int secondaryDayIndex =  curIndex == dayOfWeekController.NumberOfDays-1 ? curIndex - 1 : curIndex + 1;
			_secondaryCarousel.Init(secondaryDayIndex, width, height, cardPadding, PresentDetailCardHelper, carouselLength, clearOnRepopulate, _verticalMode, allowRecommending);
			_secondaryCarousel.OnNoCardsForSelectedDay = InvokeShowNoResultsCard;
		}
	}

	public int GetCurrentCardCount()
	{
		int ret = 0;
		if (null != _mainCarousel)
			ret = _mainCarousel.CurrentCardCount;

		return ret;
	}

	public void HideCarousel(bool hide)
	{
		if (null != _mainCarousel && null != _mainCarousel.ScrollView)
		{
			_mainCarousel.ScrollView.gameObject.SetActive(!hide);
		}
	}

	public void ClearCarousel()
	{
		if (null != _mainCarousel)
			_mainCarousel.Clear();
	}

	public void RepositionCarouselCards()
	{
		if (null != _mainCarousel)
			_mainCarousel.RepositionCards();
	}

	void OnApplicationPause(bool pause)
	{
		if (!pause && !_verticalMode && null != _mainCarousel)
		{
			_mainCarousel.Recenter();
		}

	}

    public void Populate(DataEntryBase data)
    {
        ActivityList activityList = data as ActivityList;
        if (null == activityList)
            return;

        // Keep hidden until we have ALL our data.
        HideCarousel(true);
        // However don't show "No Cards" as we don't know if there are any yet.
        HideNoResultsCard(true);

        if (null == activityList.Data)
        {
            //We don't need to do anything until update the data from the server
            return;
        }

        // Always operate on the main carousel
        List<BaseCardData> cardList = activityList.Data.Cast<BaseCardData>().ToList();
        bool anyToday = _mainCarousel.AnyCardsForSelectedDay(cardList);
        bool anyMoreToday = _mainCarousel.AnyCardsRemainingForSelectedDay(cardList);
        _mainCarousel.Populate(cardList);

        // If the Populate gave me something, recenter and call our handler.
        bool hasCards = _mainCarousel.CarouselCards.Any(x =>
        {
            var cardModel = x as ActivityDealCardViewModel;
            return cardModel != null && cardModel.IsActivity;
        });

		if (hasCards)
		{
			CancelInvoke("InvokeShowNoResultsCard");
            if (null != OnActivitiesPopulated)
                OnActivitiesPopulated();

            _mainCarousel.Recenter();
			if(null != _mainCarousel.CenterOnChild)
				_mainCarousel.CenterOnChild.Recenter();

			// Now grab sponsors.
			ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.SPONSOR_LIST, PopulateSponsors, OnPopulateSponsorsFailure);
		}
		else
		{
			// Otherwise if Populate did nothing (for example, if the day had activities but all in the past), show the No Results card.
			if (anyToday && !anyMoreToday)
			{
				ShowEndOfDayCard();
			}
			else
			{
				// However delay it since this could be our initial empty cache pull and we haven't hit the server yet.
				Invoke ("InvokeShowNoResultsCard", 5f);
			}
		}

        isMainCarouselPopulated = true;
	}

	public void Populate(List<BaseCardData> cardList)
	{
		_mainCarousel.Populate(cardList);

		if (_mainCarousel.CurrentCardCount > 0)
		{
			HideCarousel(false);
			HideNoResultsCard(true);
			if (null != OnActivitiesPopulated)
				OnActivitiesPopulated();
		}
		else
		{
			HideCarousel(true);
			HideNoResultsCard(false);
		}
	}

	public void OnPopulateFailure(string error)
	{
		if (error.Contains(HelperMethods.Instance.Error_NetworkRadioOff) || error.Contains(HelperMethods.Instance.Error_NetworkTimeOut))
		{
			HideCarousel(true);
			if (null != OnActivitiesPopulated)
				OnActivitiesPopulated();
			ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.");
		}
		else if (HelperMethods.Instance.Error_EmptyResults == error)
		{
			HideCarousel(true);
			if (null != OnActivitiesPopulated)
				OnActivitiesPopulated();
			HideNoResultsCard(false);
		}

        isMainCarouselPopulateFailed = true;
	}

	public void PopulateSponsors(DataEntryBase data)
	{
		// If there are no activities, don't show this!
		bool noActivities =_mainCarousel.CarouselCards.Count(x => x as ActivityDealCardViewModel != null && (x as ActivityDealCardViewModel).IsActivity) == 0;
		if (noActivities)
		{
			HideCarousel(true);
			HideNoResultsCard(false);
			return;
		}

		// Inject these every so often
		SponsorList sponsorList = data as SponsorList;
		if (null == sponsorList)
			return;
		else if (null == sponsorList.Data || sponsorList.Data.Length == 0)
			return;

		HideCarousel(false);
		HideNoResultsCard(true);

		_mainCarousel.PopulateSponsors(sponsorList);
	}

	public void OnPopulateSponsorsFailure(string error)
	{
		Debug.LogError("OnPopulateSponsorsFailure: " + error);
		OnPopulateFailure(error);
	}

	void HideNoResultsCard(bool hide)
	{
		if (null != HideNoResultsCardDelegate)
		{
			HideNoResultsCardDelegate(hide);
		}
	}

	void InvokeShowNoResultsCard()
	{
		HideNoResultsCard(false);
	}

	void ShowEndOfDayCard()
	{
		if (null != SetEndOfDayNoResultsCardTextDelegate)
		{
			SetEndOfDayNoResultsCardTextDelegate(true);
		}
	}

	void PresentDetailCardHelper(GameObject go)
	{
		// If user is not logged in, prompt
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Detail - Detail Card")
		                                                     .SetEventAction("Click - Summary Card")
		                                                     .SetEventLabel("User has tapped on a summary card and is attempting to load its details."));
		_cardToPresent = go;
		StartCoroutine(ITTMainSceneManager.Instance.AttemptDetailView(PresentDetailCard));	
	}
	
	void PresentDetailCard()
	{
		if (null != _notificationManager && _notificationManager.IsNotificationPresent())
		{
			_notificationManager.CloseNotification();
			_cardToPresent = null;
			return;
		}
		if (!ITTDataCache.Instance.HasSessionCredentials)
		{
			_cardToPresent = null;
			return;
		}
		
		if (null != _cardToPresent && null == _detailCard)
		{
			_detailCard = NGUITools.AddChild(_uIRoot.transform.gameObject, (GameObject)Resources.Load(_prefabPath + "DetailCardLayout"));
			_detailCard.AddComponent<UIDragDropItem>();

			DetailCardViewModel detailCardModel = _detailCard.GetComponent<DetailCardViewModel>();
			ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
			                                                     .SetEventCategory("Detail - Detail Card")
			                                                     .SetEventAction("Loading - Detail Card")
			                                                     .SetEventLabel("Detail Card is loading Nid: " + detailCardModel.Nid + " Name: " + detailCardModel.name));
			ActivityDealCardViewModel activityDealCardModel = _cardToPresent.GetComponent<ActivityDealCardViewModel>();
			ActivityTableCellViewModel atcv = _cardToPresent.GetComponent<ActivityTableCellViewModel>();
			if (null != activityDealCardModel)
			{
				detailCardModel.titleLabel.text = activityDealCardModel.titleLabel.text;
				_releaseToCloseSprite = detailCardModel.releaseToCloseSprite;
				NGUITools.SetActive(_releaseToCloseSprite.transform.gameObject,false);
				_detailCardScrollView = detailCardModel.scrollView;
				
				SetSwipeScript();
				
				_detailCardScrollView.onDragFinished = ReleaseDetailCard;
				detailCardModel.onNetworkFailed = OnNetworkFailure;
				detailCardModel.Nid = activityDealCardModel.nid;
				detailCardModel.SetTexture(activityDealCardModel.primaryImage);
				detailCardModel.SetBaseCardData(activityDealCardModel.data);
			}
			else if (null != atcv)
			{
				detailCardModel.titleLabel.text = atcv.titleLabel.text;
				_releaseToCloseSprite = detailCardModel.releaseToCloseSprite;
				NGUITools.SetActive(_releaseToCloseSprite.transform.gameObject,false);
				_detailCardScrollView = detailCardModel.scrollView;
				
				SetSwipeScript();
				
				_detailCardScrollView.onDragFinished = ReleaseDetailCard;
				detailCardModel.onNetworkFailed = OnNetworkFailure;
				detailCardModel.Nid = atcv.nid;
				detailCardModel.SetTexture(atcv.mainImage);
				detailCardModel.SetBaseCardData(atcv.data);
			}
		}
	}

	void ReleaseDetailCardForce(bool force = false)
	{
		if (true == force)
		{
			TweenScale tweenScale = _swipeScripts.ScrollViewContainer.GetComponent<TweenScale>();
			
			if (null == tweenScale)
			{
				throw new MissingComponentException();
			}
			
			tweenScale.to = _swipeScripts.ScrollViewContainer.transform.localScale;
			
			tweenScale.duration = 0.5f;
			
			tweenScale.SetOnFinished(() =>
			                         {
				tweenScale.to = Vector3.one;
				NGUITools.Destroy(_detailCard);
				_cardToPresent = null;
				NGUITools.SetActive(_mainCarousel.ScrollView.transform.gameObject, true);
				_detailCardScrollView.enabled = false;
				_detailCardScrollView.transform.localScale = Vector3.one;
			});
			
			tweenScale.enabled = true;
			tweenScale.PlayReverse();
			
			if (null != _releaseToCloseSprite)
			{
				NGUITools.SetActive(_releaseToCloseSprite.transform.gameObject,false);
			}
		}
	}

	private bool _isCloseAnimated = false;

	void ReleaseDetailCard()
	{
		if (_isCloseAnimated)
			return;

		// updownmovement
		if (closeDetailCardThresholdHorizontal <= Mathf.Abs(_swipeScripts.CurrentDistance))
		{
			_isCloseAnimated = true;
			_detailCardScrollView.panel.clipping = UIDrawCall.Clipping.SoftClip;

			TweenScale tweenScale = _swipeScripts.gameObject.AddComponent<TweenScale>();
			
			if (null == tweenScale)
			{
				throw new MissingComponentException();
			}
			
			tweenScale.from = _swipeScripts.ScrollViewContainer.transform.localScale;

			tweenScale.to = Vector3.zero;
			
			tweenScale.duration = 0.5f;

			tweenScale.SetOnFinished(() =>
				{
					_isCloseAnimated = false;
					tweenScale.to = Vector3.one;
					NGUITools.Destroy(_detailCard);
					_cardToPresent = null;
					NGUITools.SetActive(_mainCarousel.ScrollView.transform.gameObject, true);
					_detailCardScrollView.enabled = false;
					_detailCardScrollView.transform.localScale = Vector3.one;
				});
			
			tweenScale.enabled = true;
			tweenScale.PlayForward();
			
			if (null != _releaseToCloseSprite)
			{
				NGUITools.SetActive(_releaseToCloseSprite.transform.gameObject,false);
			}
		}
	}

	private void SetSwipeScript()
	{
		_swipeScripts = _detailCard.GetComponent<SwipeScripts>();
		
		if (null == _swipeScripts)
		{
			throw  new MissingComponentException();
		}
	}

	private void OnSelectedDayChanged(int indexOfDay)
	{
		if (null == _mainCarousel || _mainCarousel.DayIndex == indexOfDay)
			return;

		StartCoroutine(AnimateAndPopulateNewCarousel(indexOfDay));
	}

	private void OnNetworkRecovered()
	{
		if (null == _mainCarousel)
			return;
		if (null == dayOfWeekController)
			return;

		StartCoroutine(AnimateAndPopulateNewCarousel(0));
		dayOfWeekController.ResetToFirstDay();
	}

    public IEnumerator RefreshCarousel()
    {
        yield return StartCoroutine(AnimateAndPopulateNewCarousel(dayOfWeekController.SelectedDay.IndexOfDay));
    }

	private IEnumerator AnimateAndPopulateNewCarousel(int indexOfDay)
	{
        // Disable No Cards label regardless every time
        HideNoResultsCard(true);

		int oldDayIndex = _mainCarousel.DayIndex;
		// Main carousel should now be this, and secondary should be what main was.
		CardCarousel temp = _secondaryCarousel;
		_secondaryCarousel = _mainCarousel;
		_mainCarousel = temp;

		Resources.UnloadUnusedAssets();
		_mainCarousel.DayIndex = indexOfDay;
		

		// Start animation, based on direction
		int direction = indexOfDay - oldDayIndex;
		// - backwards, + forwards
		bool movingForward = direction > 0;

        loadingIcon.SetActive(true);

        if (_secondaryCarousel.gameObject.activeInHierarchy)
            yield return StartCoroutine(_secondaryCarousel.AnimateFade(movingForward, HelperMethods.TypeOfAnimation.AnimationOut, true));

        isMainCarouselPopulated = false;
        isMainCarouselPopulateFailed = false;
		ITTDataCache.Instance.Data.GetDataEntry((int)DataCacheIndices.ACTIVITY_LIST, Populate, OnPopulateFailure);

        while (!isMainCarouselPopulated)
        {
            yield return null;

            if (isMainCarouselPopulateFailed)
            {
                loadingIcon.SetActive(false);
                yield break;
            }
        }

        yield return StartCoroutine(_mainCarousel.AnimateFade(!movingForward, HelperMethods.TypeOfAnimation.AnimationIn, false));
        loadingIcon.SetActive(false);
	}


	#endregion

	#region Close detail card

	public bool IsDetailedCardActive
	{
		get { return null != _detailCard; }
	}

	public void CloseDetailCard()
	{
		ITTGoogleAnalytics.Instance.googleAnalytics.LogEvent(new EventHitBuilder()
		                                                     .SetEventCategory("Detail - Detail Card")
		                                                     .SetEventAction("Close - Detail Card")
		                                                     .SetEventLabel("User has closed down the detail card."));
		ReleaseDetailCardForce(true);
	}

	public void OnNetworkFailure()
	{
		ReleaseDetailCardForce(true);
	}
	#endregion
}
