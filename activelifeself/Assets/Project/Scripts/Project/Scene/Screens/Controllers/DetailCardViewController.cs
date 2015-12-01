using UnityEngine;
using System.Collections;

public class DetailCardViewController : MonoBehaviour 
{
	public DetailCardViewModel _detailCardViewModel;

	#region Global Variables
	private float _screenHeightValueForImage = 270f; //Position for image to appear at the top of the screen for the user
	public Vector3 NewImagePosition {get { return new Vector3( 0f, _screenHeightValueForImage, 0f);}}

	private float _screenHeightValueForDetails = -755f; //Position for the detail information to appear at the bottom of the image for the user
	public Vector3 NewCardDetailsPosition {get { return new Vector3( 0f, (_screenHeightValueForDetails), 0f);}}

	private Vector3 _scrollUpLerpPosition = new Vector3(0f, -575f, 0f);
	private float _scrollLerpTime = 0f;

	private bool _informationIsLoaded =  false;

	#endregion

	#region Unity Methods
	void Start()
	{
		if(_detailCardViewModel != null)
		{
			StartCoroutine(AnimateCard());
		}
	}
	#endregion

	#region Coroutines
	IEnumerator AnimateCard()
	{
		if( 
		    (_detailCardViewModel.mainImage != null) && 
		    (_detailCardViewModel.detailCardInformation != null)
		   )
		{
			_detailCardViewModel.mainImageContainer.GetComponent<Collider>().enabled = false;
			StartCoroutine(TweenObject(_detailCardViewModel.mainImage.gameObject));
			yield return StartCoroutine(FadeInObject(_detailCardViewModel.mainImageContainer.gameObject, 0.5f));
			StartCoroutine(PlayLoadingAnimation());
			yield return StartCoroutine(WaitToSeeIfInformationIsLoaded());
			StartCoroutine(StopLoadingAnimation());

			_scrollUpLerpPosition[1] -= _detailCardViewModel.detailLabel.GetComponent<UILabel>().height / 2f - 15f; // Used to offset animation for the resizing text height of the "Detail Label"
			_screenHeightValueForDetails -= _detailCardViewModel.detailLabel.GetComponent<UILabel>().height / 2f - 15f;

			AddInteractionBlocker();
			yield return StartCoroutine(AnimateTranform(_detailCardViewModel.mainImageContainer.transform, NewImagePosition, 0.5f));

			/*
			 *  Uncomment the line below if you wish the detail card to move below the image than back up to its bottom,
			 *  else leave the commented out for the card details just to slide down once the image is at the top of the screen
			 */
			//yield return StartCoroutine(AnimateTranform(_detailCardViewModel.detailCardInformation.transform ,NewCardDetailsPosition, 0.25f));

			yield return StartCoroutine(AnimateTranform(_detailCardViewModel.detailCardInformation.transform, _scrollUpLerpPosition, 0.25f));
			RemoveInteractionBlocker();
			_detailCardViewModel.mainImageContainer.GetComponent<Collider>().enabled = true;
			yield break;
		}
	}

	IEnumerator TweenObject(GameObject thisObject)
	{
		TweenScale.Begin (thisObject, 0.5f, Vector3.one);
		TweenPosition.Begin(thisObject, 0.5f, Vector3.zero);
		yield break;
	}

	IEnumerator FadeInObject(GameObject fadeGameObject, float fadeDuration)
	{
		TweenAlpha fadeObject = null;
		if(null == (fadeObject = fadeGameObject.GetComponent<TweenAlpha>()))
		{
			fadeObject = fadeGameObject.AddComponent<TweenAlpha>();
		}
		
		if(null != fadeObject)
		{
			fadeObject.from = 0f;
			fadeObject.to = 1f;
			fadeObject.duration = fadeDuration;
			fadeObject.PlayForward();
		}

		if(fadeObject.GetComponent<UIWidget>().alpha == 1)
		{
			yield break;
		}
	}

	IEnumerator FadeOutObject(GameObject fadeGameObject, float fadeDuration)
	{
		TweenAlpha fadeObject = null;
		if(null == (fadeObject = fadeGameObject.GetComponent<TweenAlpha>()))
		{
			fadeObject = fadeGameObject.AddComponent<TweenAlpha>();
		}
		
		if(null != fadeObject)
		{
			fadeObject.from = 0f;
			fadeObject.to = 1f;
			fadeObject.duration = fadeDuration;
			fadeObject.PlayReverse();
		}

		if(fadeObject.GetComponent<UIWidget>().alpha == 0)
		{
			yield break;
		}
	}

	IEnumerator PlayLoadingAnimation()
	{
		yield return StartCoroutine(FadeInObject(_detailCardViewModel.loadingIcon.gameObject, 0.25f));

		if(_informationIsLoaded == false)
		{
			while(_informationIsLoaded == false)
			{
				_detailCardViewModel.loadingIcon.transform.Rotate(Vector3.forward * 360 * Time.deltaTime);
				yield return null;
			}
		}

		if(_informationIsLoaded == true)
		{
			yield break;
		}
	}

	IEnumerator StopLoadingAnimation()
	{
		yield return StartCoroutine(FadeOutObject(_detailCardViewModel.loadingIcon.gameObject, 0.25f));
		yield break;
	}
		
	IEnumerator WaitToSeeIfInformationIsLoaded()
	{
		bool informationIsLoaded = false;
		while(informationIsLoaded == false)
		{

			if((_detailCardViewModel.titleLabel.text               != string.Empty) &&
			   (_detailCardViewModel.dateLabel.text                != string.Empty) &&
			   (_detailCardViewModel.timeLabel.text                != string.Empty) &&
			   (_detailCardViewModel.detailLabel.text              != string.Empty) &&
			   (_detailCardViewModel.adultPriceTextLabel.text      != string.Empty) &&
			   (_detailCardViewModel.childPriceTextLabel.text      != string.Empty) &&
			   (_detailCardViewModel.cardDiscountTextLabel.text    != string.Empty) &&
			   (_detailCardViewModel.locationNameTextLabel.text    != string.Empty) &&
			   (_detailCardViewModel.locationAddressTextLabel.text != string.Empty)
			   )
			{
				informationIsLoaded = true;
				_informationIsLoaded = true;
			}
			else
			{
				yield return null;
			}
		}

		yield break;
	}

	IEnumerator AnimateTranform(Transform thisTransform, Vector3 newPosition, float lerpDuration)
	{
		while(thisTransform.position != newPosition)
		{
			_scrollLerpTime += Time.deltaTime;
			thisTransform.localPosition = Vector3.Lerp(thisTransform.localPosition, newPosition, _scrollLerpTime / lerpDuration);
			
			if( ((_scrollLerpTime/lerpDuration) >= 1f) )
			{
				if(_scrollLerpTime != 0)
					_scrollLerpTime = 0f;

				yield break;
			}
			
			yield return null;
		}

		yield break;
	}

	private static void AddInteractionBlocker()
	{
		UICamera nGuiCamera = null;
		if(null != (nGuiCamera = FindObjectOfType<UICamera>()))
		{
			nGuiCamera.enabled = false;
		}
	}
	
	private static void RemoveInteractionBlocker()
	{
		UICamera nGuiCamera = null;
		if(null != (nGuiCamera = FindObjectOfType<UICamera>()))
		{
			nGuiCamera.enabled = true;
		}
	}
	#endregion
}
