using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using ITT.System;
using JsonFx.Json;
using System.Globalization;

namespace ITT.Scene

{
	public class CardCarousel : MonoBehaviour 
	{
		#region Prefab Paths
		private string _cardPrefabPath = "Prefabs/Production/";
		private string _activityCardPrefabName = "ActivityDealCard";
		private string _activityCellPrefabName = "ActivityTableCell";
		#endregion

		#region Scroll members
		public bool _verticalMode = false;
		public UIScrollView ScrollView { get; set; }
		public UICenterOnChild CenterOnChild { get; set; }
		private Transform _prevCenterTarget; // for use only with vertical lists, so we can populate additional cards.
		private Vector3 dragStartPosition;
		private Vector3 dragEndPosition;
		private float dragDistanceThreshold = 0f;
		private float dragStartTime = 0;

		private GameObject _scrollObject;
		private GameObject _verticalEndPadding;
		
		private List<IActivityViewModelBase> _carouselCards;
		public List<IActivityViewModelBase> CarouselCards
		{
			get { return _carouselCards; }
		}
		private int _carouselLength = 5; // how many cards are in the carousel at any given time
		private int _carouselLengthMax; // we might alter the above number; this is what it was to start
		public int CurrentCardCount
		{
			get
			{
				if (null == _cardCache)
					return 0;

				int count = _cardCache.Count(x => null != x as ActivityDataModel);
				return count;
			}
		}

		public int DayIndex { get; set; }	// Index of this carousel's day: used as an offset from DateTime.Today
		public int VisibleCardIndexMin { get; set; } // Index into DataCache's activity list of the leftmost visible card.
		public int VisibleCardIndexMax { get; set; } // Index of the rightmost visible card.
		public int CenterCardIndex 
		{ 
			get; 
			set; 
		}	// can use this to index into _carouselCards
		#endregion

		#region Local Card Cache
		private List<BaseCardData> _cardCache;
		#endregion

		#region Misc members
		private bool _isConfigured = false;
		private float _cardPadding;
		private UIEventListener.VoidDelegate _onCardClickedDelegate; // Hang onto this so we can pass it along when cards are populated
		private bool _allowRecommending = false;
		private bool _clearOnRepopulate = true;
		private UIEventListener.VoidDelegate _onClickDelegate;
		public delegate void OnNoCardsForSelectedDayDelegate();
		public OnNoCardsForSelectedDayDelegate OnNoCardsForSelectedDay;
		#endregion
		private DateTime GetSelectedDay()
		{
			DateTime selectedDay = DateTime.MinValue;
			if (0 == DayIndex)
				selectedDay = DateTime.Now;
			else
				selectedDay = DateTime.Today.AddDays(DayIndex);
			
			return selectedDay;
		}

		void Start()
		{
			if (!_isConfigured)
				Configure();
		}

		private void Configure()
		{
			if (_isConfigured)
				return;

			_carouselLengthMax = _carouselLength;

			if (null == _scrollObject)
			{
				_scrollObject = NGUITools.AddChild(gameObject);
				_scrollObject.name = "ScrollView";
				ScrollView = _scrollObject.AddComponent<UIScrollView>();
			}
			else if (null == ScrollView)
			{
				ScrollView = _scrollObject.GetComponent<UIScrollView>();
				if (null == ScrollView)
					ScrollView = _scrollObject.AddComponent<UIScrollView>();
			}


			if (null == CenterOnChild)
				CenterOnChild = _scrollObject.GetComponent<UICenterOnChild> ();
			if (null == CenterOnChild)
				CenterOnChild = _scrollObject.AddComponent<UICenterOnChild> ();

			if (null == _cardCache)
				_cardCache = new List<BaseCardData>();

			CenterCardIndex = 0;
			_isConfigured = true;
		}

		public void Init(int dayIndex, int width, int height, float cardPadding, UIEventListener.VoidDelegate onClickDelegate, int carouselLength = -1, bool clearOnRepopulate = true, bool verticalMode = false, bool allowRecommending = false)
		{
			_verticalMode = verticalMode;
			_allowRecommending = allowRecommending;
			_clearOnRepopulate = clearOnRepopulate;
			_onClickDelegate = onClickDelegate;

			if (!_isConfigured)
				Configure();

			_carouselLength = carouselLength > 0 ? carouselLength : _carouselLength;
			_carouselLengthMax = _carouselLength;
			DayIndex = dayIndex;
			ScrollView.movement = _verticalMode ? UIScrollView.Movement.Vertical : UIScrollView.Movement.Horizontal;
			ScrollView.onDragStarted -= OnScrollViewDragStarted;
			ScrollView.onDragStarted += OnScrollViewDragStarted;
			ScrollView.onDragFinished -= OnScrollViewDragFinished;
			ScrollView.onDragFinished += OnScrollViewDragFinished;
			ScrollView.panel.SetRect(0, 0, width, height);
			ScrollView.panel.depth = 1;
			if (!_verticalMode)
			{
				// Fix issue where dragging isn't 1:1
				ScrollView.dragEffect = UIScrollView.DragEffect.None;
			}
			else
			{
				// Don't center for vertical lists
				CenterOnChild.enabled = false;
			}

			ScrollView.panel.clipping = _verticalMode ? UIDrawCall.Clipping.SoftClip : UIDrawCall.Clipping.None;

			_cardPadding = cardPadding;

			_onCardClickedDelegate = onClickDelegate;

			if (null == _carouselCards)
				_carouselCards = new List<IActivityViewModelBase>();
			
			if (_carouselCards.Count == 0)
			{
				//CreateCards(_carouselLengthMax);

				if (!_verticalMode)
				{
					CenterOnChild.Recenter();
					CenterOnChild.enabled = false; //We handle centering so don't do it yourself, CenterOnChild.
				}
				else
				{
					CreateCards(_carouselLengthMax);
					ScrollView.ResetPosition();
				}
			}
		}

		private void CreateCards(int desiredCount)
		{
			// Start where we left off. I.e. if we have 2 (indices 0 and 1), we start at 2 and go to 4, adding 3 (for a total of 5). :D
			float offset = ( _verticalMode ? (-160f - _cardPadding) : (540f + _cardPadding) ) * (_carouselCards.Count);
			int cap = Mathf.Min(desiredCount, _carouselLengthMax);
			int olderSiblings = _scrollObject.transform.childCount;

			// SUCH HACK: track the active status of the scroll object and restore it after we add cards. It needs to be active while adding.
			bool scrollActiveStatus = _scrollObject.active;
			_scrollObject.SetActive(true);

			GameObject cardPrefabToMake = Resources.Load(_cardPrefabPath + (!_verticalMode ? _activityCardPrefabName : _activityCellPrefabName) ) as GameObject;
			for (int i = 0; i < cap; i++)
			{
				GameObject newCard = NGUITools.AddChild(_scrollObject, cardPrefabToMake);
				newCard.name = "Card_" + (i + olderSiblings).ToString();

                if (0f == dragDistanceThreshold)
				{
				    BoxCollider col = newCard.GetComponent<BoxCollider>();
					dragDistanceThreshold = col.bounds.size.x * 0.33f;
				}

				UIDragScrollView dragScrollView = newCard.GetComponent<UIDragScrollView>();
				dragScrollView.scrollView = ScrollView;
				
				IActivityViewModelBase cardScript = newCard.GetComponent(typeof(IActivityViewModelBase)) as IActivityViewModelBase;
				if (null != cardScript)
				{
					_carouselCards.Add(cardScript);
				}

                Transform newCardTrans = newCard.transform;
				Vector3 localPos = newCardTrans.localPosition;
				if (!_verticalMode)
				{
					localPos.x = offset;
				}
				else
				{
					localPos.y = offset;
				}
				newCardTrans.localPosition = localPos;

				offset += (_verticalMode ? -160f - _cardPadding : 540f + _cardPadding);
				if (_carouselCards.Count == _carouselLengthMax)
					break;
			}

            if (_verticalMode)
			{
				RepositionPadding();
				_carouselLength = _carouselCards.Count;
				_carouselLengthMax = Mathf.Max(_carouselLength, _carouselLengthMax);
			}
			else
				_carouselLength = Mathf.Min(_carouselCards.Count, _carouselLengthMax);

			_scrollObject.SetActive(scrollActiveStatus);
		}

		public void Clear()
		{
			// Don't remove cards, but clear out their data
			for (int i = 0; i < _carouselCards.Count; i++) 
			{
				IActivityViewModelBase card = _carouselCards[i];

				card.Clear();
				if (_verticalMode)
				{
					MonoBehaviour cardBehavior = (card as MonoBehaviour);
					Vector3 position = cardBehavior.transform.localPosition;
					position.y = -i * ((cardBehavior.GetComponent<Collider>() as BoxCollider).size.y + _cardPadding);
					cardBehavior.transform.localPosition = position;
				}
			}

			// Old cached card data now too
			_cardCache.Clear();

			// Also clear out any date labels
			DateListItem[] dateLabels = transform.GetComponentsInChildren<DateListItem>();
			dateLabels.ToList().ForEach(x => Destroy(x.gameObject));
			ScrollView.ResetPosition();
		}

		private void ClampCarouselCards(int cap, bool clearExisting = false)
		{
			// Make sure we don't have fewer incoming items than carousel cards
			int clampedCap = Mathf.Clamp(cap, 0, _carouselLengthMax);
			int diff = Mathf.Clamp(clampedCap - _carouselCards.Count, -_carouselLengthMax, _carouselLengthMax);
			for (int i = diff; i < 0; i++)
			{
				int index = _carouselCards.Count + i;
				DestroyImmediate((_carouselCards[index] as MonoBehaviour).gameObject);
				_carouselCards.RemoveAt(index);
			}

			// For whatever's left, mark them unpopulated
			if (clearExisting)
			{
				_carouselCards.ForEach(x => 
                {
					x.Clear();
				});
			}
			// Reposition what's left
			RepositionCards();

			if (diff < 0)
				_carouselLength += diff;
			else if (diff > 0)
			{
				// We should add cards, but not more than the max.
				CreateCards(diff);
			}
		}

		public void RepositionCards()
		{
			float offset = 0;
			for (int i = 0; i < _carouselCards.Count; i++)
			{
				Vector3 localPosition = (_carouselCards[i] as MonoBehaviour).transform.localPosition;
				if (_verticalMode)
				{
					localPosition.y = offset;
				}
				else
				{
					localPosition.x = offset;
				}
				
				(_carouselCards[i] as MonoBehaviour).transform.localPosition = localPosition;
				offset += (_verticalMode ? -160f - _cardPadding : 540f + _cardPadding);
			}
			RepositionPadding();
		}

		public bool AnyCardsForSelectedDay(List<BaseCardData> cardList)
		{
			DateTime selectedDay = DateTime.Today.AddDays(DayIndex);
			return cardList.Any(x => x.dateTime.Date == selectedDay.Date);
		}

		public bool AnyCardsRemainingForSelectedDay(List<BaseCardData> cardList)
		{
			DateTime selectedDay = GetSelectedDay();

			// If it's on the same day and it's larger than the time
			return cardList.Any(x => x.dateTime >= selectedDay && x.dateTime.Date == selectedDay.Date);
		}

		public void Populate(List<BaseCardData> cardList)
		{
			bool hasAnyItems = false;
			bool hasItemsForToday = true;
			DateTime selectedDay = GetSelectedDay();

			hasAnyItems = cardList.Count > 0;

			if (!hasAnyItems)
			{
				if (null != OnNoCardsForSelectedDay && !_verticalMode)
					OnNoCardsForSelectedDay();
				return;
			}
			else if (DayIndex >= 0)
			{
				hasItemsForToday = cardList.Any(x => x.dateTime.Date == selectedDay.Date);
			}

			if (!hasItemsForToday)
			{
				ITTFilterRequest filter = new ITTFilterRequest();
				filter.AddFilterValue(new DistanceValue());
				filter.AddFilterValue(new MinDateValue(selectedDay));
				filter.AddFilterValue(new MaxDateValue(selectedDay));
				ITTDataCache.Instance.RequestCombinedActivities(OnRetrieveActivitiesForSelectedDaySuccess, OnRetrieveActivitiesForSelectedDayFailure, filter);
			}
			else
			{
				// Check for dupes and remove older cards for current day
				cardList.RemoveAll(x => _cardCache.Any(y => y.id == x.id));
				if (DayIndex == 0)
				{
					cardList.RemoveAll(x => x.dateTime < selectedDay);
				}
				_PopulateInternal(cardList);
			}
		}

		public void PopulateSponsors(SponsorList sponsorList)
		{
			List<BaseCardData> baseCardList = sponsorList.Data.ToList().Cast<BaseCardData>().ToList();
			_PopulateInternal(baseCardList);
		}

		private void OnRetrieveActivitiesForSelectedDaySuccess(string json)
		{
			List<BaseCardData> baseList = null;
			try
			{
				HelperMethods.ResultReponseObject result = JsonFx.Json.JsonReader.Deserialize<HelperMethods.ResultReponseObject>(json);
				if (result.total_count == 0)
				{
					Debug.Log("No items for selected day.");
					OnRetrieveActivitiesForSelectedDayFailure(HelperMethods.Instance.Error_EmptyResults);
					return;
				}

				MasterBaseCardData masterBaseCardData = JsonReader.Deserialize<MasterBaseCardData>(json);
				ActivityDataModel[] activityArray = masterBaseCardData.results;
				baseList = activityArray.Cast<BaseCardData>().ToList();
				ITTDataCache.Instance.Data.AppendDataEntry<ActivityDataModel[]>((int)DataCacheIndices.ACTIVITY_LIST, baseList.Cast<ActivityDataModel>().ToArray());
			}
			catch
			{
				Debug.LogError("CardCarousel error deserializing activity JSON");
			}
		}

		private void OnRetrieveActivitiesForSelectedDayFailure(string error)
		{
			Debug.LogError("CardCarousel error: " + error);
			if (error.Contains(HelperMethods.Instance.Error_NetworkRadioOff) || error.Contains(HelperMethods.Instance.Error_NetworkTimeOut))
			{
				ModalPopupOK.Spawn("Experiencing connection issues with the server. Please check your connection and try again.");
			}
			else if (error.Contains(HelperMethods.Instance.Error_EmptyResults) && null != OnNoCardsForSelectedDay && !_verticalMode)
			{
				OnNoCardsForSelectedDay();
			}
		}

		private void _DistanceSort(List<BaseCardData> list)
		{
			list.Sort((a, b) => 
			     {
				int dateDifference = a.dateTime.CompareTo(b.dateTime);
				if (dateDifference == 0)
				{
					return a.Proximity.CompareTo(b.Proximity);
				}
				else
					return dateDifference;
			});
		}

		private void _PopulateInternal(List<BaseCardData> cardList)
		{
            if (_clearOnRepopulate)
                Clear();

			// If this is a vertical list, show all items. Else parse out only today's items.
			if (_verticalMode)
			{
				_cardCache = cardList;
				ClampCarouselCards(_cardCache.Count, true);
			}
			else
			{
				bool receivingSponsors = cardList.Any(x => null != x as SponsorDataModel);
				if (receivingSponsors)
				{
					_cardCache.AddRange(cardList);
				}
				else
				{
                    List<BaseCardData> toAdd = cardList.FindAll( x =>
						{
							if (x.dateTime == DateTime.MinValue) 
								x.ParseDateString();

						    DateTime lowerBound = (0 == DayIndex ? DateTime.Now : DateTime.Today); 
							return x.dateTime >= lowerBound.AddDays(DayIndex) && x.dateTime < DateTime.Today.AddDays(DayIndex+1);
						}
					);

					_DistanceSort(toAdd);

					_cardCache.AddRange(toAdd);

					ClampCarouselCards(_cardCache.Count, true);
				}

                // Regardless, now sort such that sponsors appear once every five activities.
                // Grab all sponsors first if there are any
                List<BaseCardData> currentSponsors = _cardCache.FindAll(x => null != x as SponsorDataModel);
				foreach (BaseCardData bcd in currentSponsors)
				{
					_cardCache.Remove(bcd);
				}

				if (currentSponsors.Count != 0)
				{
					if (_cardCache.Count >= 5)
					{
						int s = 0;
						int a = 5;
						while (a < _cardCache.Count)
						{
							_cardCache.Insert(a, currentSponsors[s % currentSponsors.Count]);
							a+=6;
							s++;
						}
					}
					else if (_cardCache.Count > 0 && _cardCache.Count < 5)
					{
						_cardCache.Add(currentSponsors[UnityEngine.Random.Range(0, currentSponsors.Count)]);
					}
					else
						_cardCache.AddRange(currentSponsors);

					ClampCarouselCards(_cardCache.Count, true);
				}
			}

			DateTime lastDate = DateTime.MinValue;


			int i = 0;
			float dateLabelOffset = 0f;
			for ( ; i < _cardCache.Count && i < _carouselCards.Count; i++)
			{
				if (!_carouselCards[i].IsPopulated() || null != _cardCache[i] as ActivityDataModel)
				{
					_carouselCards[i].Populate(_cardCache[i], _onCardClickedDelegate, _allowRecommending);
					if (_verticalMode)
					{
						// Move this card further down if we inserted date labels earlier in the list
						MonoBehaviour cardBehavior = _carouselCards[i] as MonoBehaviour;
						Vector3 newCardPosition = cardBehavior.transform.localPosition;
						newCardPosition.y -= dateLabelOffset;
						cardBehavior.transform.localPosition = newCardPosition;

						// Insert date fillers every unique day
						if (_cardCache[i].dateTime.Day != lastDate.Day ||
						    _cardCache[i].dateTime.Month != lastDate.Month ||
						    _cardCache[i].dateTime.Year != lastDate.Year)
						{
							lastDate = _cardCache[i].dateTime;

							float additionalOffset = (cardBehavior.GetComponent<Collider>() as BoxCollider).size.y + _cardPadding;
							dateLabelOffset += additionalOffset;
							newCardPosition = cardBehavior.transform.localPosition;
							Vector3 dateLabelPosition = newCardPosition;
							newCardPosition.y -= additionalOffset;
							cardBehavior.transform.localPosition = newCardPosition;

							// Check if there's already a date label for this date.
							DateListItem[] dateLabels = ScrollView.transform.GetComponentsInChildren<DateListItem>();
							if (!dateLabels.ToList().Exists(x => x.dateTime == lastDate))
							{
								GameObject dateListItemPrefab = (GameObject) Resources.Load(_cardPrefabPath + "DateListItem");
								GameObject dateListItem = NGUITools.AddChild(ScrollView.gameObject, dateListItemPrefab);
								DateListItem dliScript = dateListItem.GetComponent<DateListItem>();
								UIDragScrollView dateDragScrollView = dateListItem.GetComponent<UIDragScrollView>();
								dateDragScrollView.scrollView = ScrollView;

								// Relative day (yesterday/today/tomorrow)
								DateTime now = DateTime.Now;
								DateTime yesterday = DateTime.Now.AddDays(-1);
								DateTime tomorrow = DateTime.Now.AddDays(1);
								
								if (lastDate.Date == now.Date)
								{
									dliScript.dayLabel.text = "TODAY";
								}
								else if (lastDate.Date == yesterday.Date)
								{
									dliScript.dayLabel.text = "YESTERDAY";
								}
								else if (lastDate.Date == tomorrow.Date)
								{
									dliScript.dayLabel.text = "TOMORROW";
								}
								else
								{
									dliScript.dayLabel.text = lastDate.DayOfWeek.ToString().ToUpper();
								}
								
								dliScript.dateLabel.text = lastDate.ToString("M");
								dliScript.dateTime = lastDate;

								dateListItem.transform.localPosition = dateLabelPosition;
							}
						}
					}
				}
			}

			RepositionPadding ();

			VisibleCardIndexMin = 0;
			VisibleCardIndexMax = i-1;
		}

		public void OnScrollViewDragStarted()
		{
			dragStartPosition = ScrollView.transform.position;
			dragStartTime = Time.time;
			if (_verticalMode)
			{
				_prevCenterTarget = CenterOnChild.DetermineCenterTarget();
				CenterCardIndex = _carouselCards.FindIndex( x => (x as MonoBehaviour).transform == _prevCenterTarget );
			}
		}

		// HACK
		void Update()
		{
			if (Vector3.zero != dragStartPosition)
				dragEndPosition = ScrollView.transform.position;
		}

		public void OnScrollViewDragFinished()
		{
			dragEndPosition = ScrollView.transform.position;

			Transform newCenterTarget = null;

			if (!_verticalMode)
			{
				Vector2 drag = dragEndPosition - dragStartPosition;
				dragStartPosition = dragEndPosition = Vector3.zero;

				float sqrDragDistance = Vector3.SqrMagnitude(drag);
				if (sqrDragDistance > dragDistanceThreshold * dragDistanceThreshold
				    || sqrDragDistance / ((Time.time - dragStartTime)/1.5) > dragDistanceThreshold * dragDistanceThreshold)
				{
					// We dragged far enough: try centering on the next card in this direction.
					if (null == CenterOnChild.centeredObject)
						newCenterTarget = CenterOnChild.DetermineCenterTarget();
					else
					{
						float dot = Vector3.Dot(drag, Vector3.right);
						int newIndex = (int)(CenterCardIndex - Mathf.Sign(dot));
						if (newIndex >= 0 && newIndex < _carouselCards.Count)
						{
							newCenterTarget = (_carouselCards[newIndex] as MonoBehaviour).transform;
						}
						else
						{
							newCenterTarget = CenterOnChild.centeredObject.transform;
						}
					}
				}
			}
			if (null == newCenterTarget)
			{
				// Try recentering on centered object, if it exists
				if (_verticalMode)
				{
					newCenterTarget = CenterOnChild.DetermineCenterTarget();
				}
				else 
				{
					if (null != CenterOnChild.centeredObject)
					{
						CenterOnChild.Recenter();
					}
					else
					{
						Debug.LogError("CardCarousel: centered on null");
					}
					return;
				}
			}

			DateListItem dli = newCenterTarget.GetComponent<DateListItem>();
			Transform nearestToDateLabel = null;
			int newTargetIndex = -1;
			if (null != dli)
			{
				// Make sure we still recycle the cards if we center on a date label
				// Find the nearest card in the direction we came from
				Vector3 deltaScroll = dli.transform.localPosition - _prevCenterTarget.transform.localPosition;
				RaycastHit[] hits = Physics.RaycastAll(dli.transform.position, -deltaScroll);
				if (null != hits && hits.Length > 0)
				{
					// Pull out only cards, so no date labels
					List<RaycastHit> hitList = hits.ToList().FindAll(x => null == x.collider.gameObject.GetComponent<DateListItem>());
					// Sort by closest
					hitList.Sort( (a, b) => a.distance.CompareTo(b.distance) );
					// Grab the first (closest)
					Transform closest = hitList[0].collider.transform;
					if (null == closest)
						return;
					Debug.LogWarning("Found " + closest.name);
					newTargetIndex = _carouselCards.FindIndex( x => (x as MonoBehaviour).transform == closest );
				}
			}
			else if (!_verticalMode)
				CenterOnChild.CenterOn(newCenterTarget);


			if (newTargetIndex < 0)
			{
				newTargetIndex = _carouselCards.FindIndex( x => (x as MonoBehaviour).transform == newCenterTarget );
			}
			if (0 > newTargetIndex) return;

			if (_verticalMode)
				_prevCenterTarget = newCenterTarget;

			int halfCarouselLength = _carouselLength / 2;
			int numCardsToMove = newTargetIndex - halfCarouselLength;

			CenterCardIndex = newTargetIndex;

			if (0 == numCardsToMove)
				return;

			// Slide the visible indices over an equal amount.
			// Clamp the number of cards moved if it would push us out of bounds.
			int oldMin = VisibleCardIndexMin;
			VisibleCardIndexMin = Mathf.Clamp(VisibleCardIndexMin + numCardsToMove, 0, _cardCache.Count - _carouselLength);
			int diff = VisibleCardIndexMin - oldMin;
			numCardsToMove = diff;
			VisibleCardIndexMax = Mathf.Clamp(VisibleCardIndexMax + numCardsToMove, _carouselLength-1, _cardCache.Count-1);

			int positiveEdge = numCardsToMove < 0 ? 0 : _carouselCards.Count;
			int negativeEdge = numCardsToMove < 0 ? _carouselCards.Count : 0;

			int negativeIndex = negativeEdge - (negativeEdge == _carouselLength ? 1 : 0);
			int positiveIndex = positiveEdge - (positiveEdge == _carouselLength ? 1 : 0);
			for (int i = 0; i < Mathf.Abs(numCardsToMove); i++)
			{
				IActivityViewModelBase movedCard = _carouselCards[negativeIndex];
				IActivityViewModelBase edgeCard  = _carouselCards[positiveIndex];

				DateTime lastDate = DateTime.MinValue;
				if (_verticalMode)
				{
                    lastDate = (edgeCard as ActivityTableCellViewModel).data.dateTime.Date;
                }

				int indexToPopulate = (numCardsToMove > 0 ? VisibleCardIndexMax - (numCardsToMove-1-i) : VisibleCardIndexMin + (Mathf.Abs(numCardsToMove)-1-i));

				movedCard.Clear();
				movedCard.Populate(_cardCache[indexToPopulate], _onCardClickedDelegate, _allowRecommending);

				DateTime newDate = _cardCache[indexToPopulate].dateTime.Date;

				Vector3 newPosition = (edgeCard as MonoBehaviour).transform.localPosition;
				if (_verticalMode)
				{
					float singleCardOffset = ((edgeCard as MonoBehaviour).GetComponent<Collider>() as BoxCollider).size.y + _cardPadding;

					newPosition.y -= (numCardsToMove > 0 ? 1 : -1) * singleCardOffset;
					if (lastDate != newDate)
					{
                        // If we're moving positive, it's the newDate. Else it's lastDate.
                        DateTime dateToShow = (numCardsToMove > 0 ? newDate : lastDate);

						Vector3 dateListItemPosition = newPosition;
						newPosition.y -= (numCardsToMove > 0 ? 1 : -1) * singleCardOffset;

						// Check if there's already a date label for this date.
						DateListItem[] dateLabels = ScrollView.transform.GetComponentsInChildren<DateListItem>();
                        var dateLabel = dateLabels.ToList().Find(x => x.dateTime.Date == dateToShow);
                        if (dateLabel == null)
						{

							// Now at the singleCardOffset position, spawn a date label.
							GameObject dateListItemPrefab = (GameObject) Resources.Load(_cardPrefabPath + "DateListItem");
							GameObject dateListItem = NGUITools.AddChild(ScrollView.gameObject, dateListItemPrefab);
							DateListItem dliScript = dateListItem.GetComponent<DateListItem>();
							UIDragScrollView dateDragScrollView = dateListItem.AddComponent<UIDragScrollView>();
							dateDragScrollView.scrollView = ScrollView;
							
							// Still respect relative dates
							DateTime now = DateTime.Now;
							DateTime yesterday = DateTime.Now.AddDays(-1);
							DateTime tomorrow = DateTime.Now.AddDays(1);

							if (dateToShow.Date == now.Date)
							{
								dliScript.dayLabel.text = "TODAY";
							}
							else if (dateToShow.Date == yesterday.Date)
							{
								dliScript.dayLabel.text = "YESTERDAY";
							}
							else if (dateToShow.Date == tomorrow.Date)
							{
								dliScript.dayLabel.text = "TOMORROW";
							}
							else
							{
								dliScript.dayLabel.text = dateToShow.DayOfWeek.ToString().ToUpper();
							}

                            dliScript.dateLabel.text = dateToShow.ToString("M");
							dliScript.dateTime = dateToShow.Date;
							dateListItem.gameObject.transform.localPosition = dateListItemPosition;
						}
					}

				}
				else
				{
					newPosition.x += (numCardsToMove > 0 ? 1 : -1) * ( ((edgeCard as MonoBehaviour).GetComponent<Collider>() as BoxCollider).size.x + _cardPadding );
				}

				(movedCard as MonoBehaviour).transform.localPosition = newPosition;
				_carouselCards.Insert(positiveEdge, movedCard);
				_carouselCards.RemoveAt(negativeEdge);

			}

			if (null != dli)
				return;

			CenterCardIndex = _carouselCards.FindIndex( x => (x as MonoBehaviour).transform == newCenterTarget );
		}

        public IEnumerator AnimateFade(bool toBackground, HelperMethods.TypeOfAnimation animType, bool clear, float duration = 0.25f)
        {
            gameObject.SetActive(true);
            ScrollView.panel.depth = toBackground ? 2 : 999;

            // fade
            HelperMethods.FadeObject(ScrollView.gameObject, animType, duration);

            // scale
            Vector3 scaleFrom = Vector3.one;
            Vector3 scaleTo = toBackground ? HelperMethods.CloseToZeroVec : HelperMethods.BigScreenSizeVec;
            if (animType == HelperMethods.TypeOfAnimation.AnimationIn)
            {
                var tmp = scaleFrom;
                scaleFrom = scaleTo;
                scaleTo = tmp;
            }
            yield return StartCoroutine(HelperMethods.InterpolateScale(gameObject, scaleFrom, scaleTo, duration));

            // finishing up
            if (clear)
                Clear();

            if (animType == HelperMethods.TypeOfAnimation.AnimationOut)
                Recenter();

            ScrollView.panel.depth = 2;
        }

		public void Recenter()
		{
			// Recenter my scroll list
			ScrollView.transform.localPosition = Vector3.zero;
			dragStartPosition = dragEndPosition = Vector3.zero;
			CenterCardIndex = 0;
		}

		void OnDrawGizmos()
		{
			Vector2 drag = dragEndPosition - dragStartPosition;
			Gizmos.color = Color.red;
			float sqrDragDistance = Vector3.SqrMagnitude(drag);
			if (sqrDragDistance > dragDistanceThreshold * dragDistanceThreshold)
			{
				Gizmos.color = Color.blue;
			}

			Gizmos.DrawLine(Vector3.zero, dragEndPosition - dragStartPosition);
		}

		public void SetScrollingEnabled(bool enabled)
		{
			foreach(IActivityViewModelBase a in _carouselCards)
			{
				(a as MonoBehaviour).GetComponent<Collider>().enabled = enabled;
			}
		}

		private void RepositionPadding()
		{
			if (!_verticalMode) {
				return;
			}

			float yPos = 0;
			for (int i = 0 ; i < _scrollObject.transform.childCount; ++i) {
				var childTransform = _scrollObject.transform.GetChild (i);
				if(childTransform.gameObject == _verticalEndPadding)
				{
					continue;
				}

				yPos = Mathf.Min(yPos, childTransform.localPosition.y);
			}

			if(_verticalEndPadding == null)
			{
				_verticalEndPadding = NGUITools.AddChild(_scrollObject);
				var col = _verticalEndPadding.AddComponent<BoxCollider>();
				col.size = new Vector3(100, 100);
				_verticalEndPadding.AddComponent<UISprite>();
			}
			Vector3 localPos = _verticalEndPadding.transform.localPosition;
			localPos.y = yPos - 100;
			_verticalEndPadding.transform.localPosition = localPos;
		}
	}
}
