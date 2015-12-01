using System;
using UnityEngine;
using System.Collections;

public class ColorManagementSystemController : MonoBehaviour
{

	#region Constants

	public const float AnimationDuration = 1f;

	#endregion

	#region TimeState

	public enum TimeState
	{
		From6AmTo8Am = 0,
		From8AmTo10Am,
		From10AmTo12Pm,
		From12PmTo2Pm,
		From2PmTo4Pm,
		From4PmTo6Pm,
		From6PmTo8Pm,
		From8PmTo10Pm,
		From10PmTo12Am,
		From12AmTo6Am
	}

	private const int TimeStateCount = 10;

	private class ColorState
	{
		public ColorState(string name, Color color)
		{
			_spriteName = name;
			_color = color;
		}
		private string _spriteName;
		public string SpriteName { get {return _spriteName;} }
		private Color _color;
		public Color Color { get {return _color;} }
	}

	private ColorState[] _colorStates = new ColorState[TimeStateCount]
	{
		/*From6AmTo8Am*/	new ColorState("purple_blue_gradient",	new Color(0.5137f, 0.1843f, 0.7412f, 1f)), 
		/*From8AmTo10Am*/	new ColorState("blue_light_green",		new Color(0.2f, 0.4941f, 0.4705f)),
		/*From10AmTo12Pm*/	new ColorState("lightGreen_darkYellow",	new Color(0.1765f, 0.4863f, 0.0667f)),
		/*From12PmTo2Pm*/	new ColorState("darkYellow_orange", 	new Color(0.9412f, 0.5922f, 0.1412f)),
		/*From2PmTo4Pm*/	new ColorState("orange_darkRed", 		new Color(0.651f, 0.2353f, 0.051f)),
		/*From4PmTo6Pm*/	new ColorState("orange_darkYellow", 	new Color(0.9412f, 0.5922f, 0.1412f)),
		/*From6PmTo8Pm*/	new ColorState("darkYellow_blue",  		new Color(0.271f, 0.3725f, 0.5373f)),
		/*From8PmTo10Pm*/	new ColorState("blue_purple",  			new Color(0.5137f, 0.1843f, 0.7412f)),
		/*From10PmTo12Am*/	new ColorState("purple_darkBlue",  		new Color(0.0157f, 0f, 0.2784f)),
		/*From12AmTo6Am*/	new ColorState("darkBlue_black",  		new Color(0f, 0f, 0f))
	};

	private TimeState[] _colorStateIndex = new TimeState[24] 
	{
		/*00-02*/	TimeState.From12AmTo6Am,TimeState.From12AmTo6Am,
		/*02-04*/	TimeState.From12AmTo6Am,TimeState.From12AmTo6Am,
		/*04-06*/	TimeState.From12AmTo6Am,TimeState.From12AmTo6Am,
		/*06-08*/	TimeState.From6AmTo8Am,TimeState.From6AmTo8Am,
		/*08-10*/	TimeState.From8AmTo10Am,TimeState.From8AmTo10Am,
		/*10-12*/	TimeState.From10AmTo12Pm,TimeState.From10AmTo12Pm,
		/*12-14*/	TimeState.From12PmTo2Pm,TimeState.From12PmTo2Pm,
		/*14-16*/	TimeState.From2PmTo4Pm,TimeState.From2PmTo4Pm,
		/*16-18*/	TimeState.From4PmTo6Pm,TimeState.From4PmTo6Pm,
		/*18-20*/	TimeState.From6PmTo8Pm,TimeState.From6PmTo8Pm,
		/*20-22*/	TimeState.From8PmTo10Pm,TimeState.From8PmTo10Pm,
		/*22-00*/	TimeState.From10PmTo12Am,TimeState.From10PmTo12Am
	};
	#endregion

	#region Variables

	private static ColorManagementSystemController _instance;
		public static ColorManagementSystemController Instance
		{
			get 
			{
				if (null == _instance)
				{
					_instance = GameObject.FindObjectOfType(typeof(ColorManagementSystemController)) as ColorManagementSystemController;
				}

				return _instance;
			}
		}

	private ColorManagementSystemModel _colorManagementModel;

	private TimeState _currentTimeState;

	public TimeState CurrentTimeState
	{
		get { return _currentTimeState; }
		set
		{
			if (value == CurrentTimeState)
				return;

			_currentTimeState = value;

			_isIgnoreUpdateWhileAnimation = true;

			StartCoroutine(IgnoringUpdateUntilNextHour());

			print(_currentTimeState);

			if (OnCurrentTimeStateChangeEvent != null)
			{
				OnCurrentTimeStateChangeEvent();
			}

			SendMessage("ChangeColor", SendMessageOptions.DontRequireReceiver);
		}
	}

	public TimeState NextTimeState
	{
		get
		{
			int index = (int) _currentTimeState;
			index++;
			if (index >= TimeStateCount)
			{
				index = 0;
			}

			return (TimeState) index;
		}
	}

	public TimeState PreviousTimeState
	{
		get
		{
			int index = (int) _currentTimeState;
			index--;
			if (index < 0)
			{
				index = 0;
			}

			return (TimeState) index;
		}
	}

	public event Action OnCurrentTimeStateChangeEvent;

	private bool _isIgnoreUpdateWhileAnimation = false;

	private bool _isIgnoreUpdates = false;

	private IEnumerator IgnoringUpdateUntilNextHour()
	{
		_isIgnoreUpdates = true;
		global::System.DateTime currentTime = global::System.DateTime.Now;
		int addHours = 1;
		if (currentTime.Hour % 2 == 0) {
			addHours = 2;
		}
		int hour = currentTime.AddHours(addHours).Hour;
		DateTime newTime = Convert.ToDateTime(hour + ":00");
		TimeSpan timespan = newTime.Subtract(currentTime);

		yield return new WaitForSeconds((float)timespan.TotalSeconds);
		_isIgnoreUpdates = false;
	}

	public static AnimationCurve EasyInOutAnimCurve;

	#endregion

	#region MonoBehaviour actions

	// Use this for initialization
	private void Start()
	{
		_colorManagementModel = GetComponent<ColorManagementSystemModel>();

		if (null == _colorManagementModel)
		{
			throw new MissingComponentException(
				"ColorManagementSystemController.Start - can't get ColorManagementSystemModel component for _colorManagementModel");
		}
		SubscribeEvents();

		Init();

	}

	// Update is called once per frame
	private void Update()
	{
		UpdateColorManagement();
	}

	private void OnDestroy()
	{
		UnSubscribeEvents();
	}

	public void OnApplicationPause(bool paused) 
	{
		// to be sure that the color is updated when it back to background.
		if(!paused) {
			_isIgnoreUpdates = false;
			_isIgnoreUpdateWhileAnimation = false;
		}
	}
	#endregion

	#region Actions

	private void SubscribeEvents()
	{
		OnCurrentTimeStateChangeEvent += OnCurrentTimeStateChange;
	}

	private void UnSubscribeEvents()
	{
		OnCurrentTimeStateChangeEvent -= OnCurrentTimeStateChange;
	}

	private void Init()
	{		
		CurrentTimeState = GetTimeState();

		InitAnimationCurve();

		InitBackground();
	}

	private void InitBackground()
	{
		_colorManagementModel.BackgroundGradientMain.spriteName = GetSpriteNameByTimeState(PreviousTimeState);
		_colorManagementModel.BackgroundGradientSecondary.spriteName = GetSpriteNameByTimeState(_currentTimeState);
	}

	private void InitNextSpriteGradient()
	{
		_colorManagementModel.BackgroundGradientSecondary.spriteName = GetSpriteNameByTimeState(_currentTimeState);
	}

	public void InitAnimationCurve()
	{
		if (null != EasyInOutAnimCurve)
			return;

		TweenAlpha tween = _colorManagementModel.BackgroundGradientMain.gameObject.GetComponent<TweenAlpha>();

		if (null == tween)
		{
			throw new MissingComponentException();
		}

		EasyInOutAnimCurve = tween.animationCurve;

		Debug.Log("ColorManagementSystemController.InitAnimationCurve - OK");
	}

	private void SetTweenToSpriteAndStartAnimate()
	{
		SetTweenAlphaToSprite(_colorManagementModel.BackgroundGradientMain, false);
		SetTweenAlphaToSprite(_colorManagementModel.BackgroundGradientSecondary, true);
	}

	private void SetTweenAlphaToSprite(UISprite sprite, bool isFrom0To1)
	{
		TweenAlpha tween = sprite.gameObject.GetComponent<TweenAlpha>();

		AnimationCurve animationCurve = null;

		if (null == tween)
		{
			throw new MissingComponentException();
		}

		animationCurve = tween.animationCurve;

		if (null == EasyInOutAnimCurve)
			EasyInOutAnimCurve = animationCurve;

		Destroy(tween);

		tween = sprite.gameObject.AddComponent<TweenAlpha>();

		tween.from = isFrom0To1 ? 0f : 1f;
		tween.to = isFrom0To1 ? 1f : 0f;

		tween.duration = AnimationDuration;

		tween.delay = 1f;

		tween.animationCurve = animationCurve;

		if (isFrom0To1)
		{
			tween.SetOnFinished(OnBackgroundGradientChange);
		}
	}

	private void OnBackgroundGradientChange()
	{
		UISprite temp = _colorManagementModel.BackgroundGradientMain;
		_colorManagementModel.BackgroundGradientMain = _colorManagementModel.BackgroundGradientSecondary;
		_colorManagementModel.BackgroundGradientSecondary = temp;

		_isIgnoreUpdateWhileAnimation = false;

		Debug.Log("ColorManagementSystemController.OnBackgroundGradientChange - OK");
	}

	private void UpdateColorManagement()
	{
		if (_isIgnoreUpdateWhileAnimation || _isIgnoreUpdates)
			return;

		TimeState timeState = GetTimeState ();
		if (_currentTimeState != timeState) {
			CurrentTimeState = timeState;
		} else {
			StartCoroutine(IgnoringUpdateUntilNextHour());
		}
	}

	private void OnCurrentTimeStateChange()
	{
		//Debug.Log("ColorManagementSystemController.OnCurrentTimeStateChange - OK");
		InitNextSpriteGradient();
		SetTweenToSpriteAndStartAnimate();
	}

	#endregion

	#region Necessary Getters actions

	private TimeState GetTimeState()
	{
		global::System.DateTime day = global::System.DateTime.Now;
		return _colorStateIndex[day.Hour];
	}


	private string GetSpriteNameByTimeState(TimeState timeState)
	{
		return _colorStates[(int)timeState].SpriteName;
	}

	private Color GetColorByTimeState(TimeState timeState)
	{
		Color color = _colorStates[(int)timeState].Color;
		//print("ColorManagementSystemController.GetColorByTimeState: timeState -" + timeState + " color - " + color);
		return color;
	}

	#endregion

	#region Widget color action

	public static Color GetCurrentTextColor()
	{
		return Instance.GetColorByTimeState(Instance.CurrentTimeState);
	}

	public static Color GetHighlightColorByTimeState(TimeState state)
	{
		return Instance.GetColorByTimeState(state);
	}

	public static void UpdateColor(UIWidget widget)
	{
		TweenColor tweenColor = widget.gameObject.AddComponent<TweenColor>();

		if (null == tweenColor)
		{
			throw new MissingComponentException();
		}

		tweenColor.from = widget.color;//GetHighlightColorByTimeState(Instance.PreviousTimeState);
		tweenColor.to = GetCurrentTextColor();

		tweenColor.delay = 1f;
		tweenColor.duration = AnimationDuration;

		if (null == EasyInOutAnimCurve)
		{
			Instance.InitAnimationCurve();
		}

		tweenColor.animationCurve = EasyInOutAnimCurve;

		tweenColor.SetOnFinished(() => Destroy(tweenColor));

	}

	#endregion

	#region Test action region

	private void OnTestButton(GameObject sender)
	{
		Debug.Log("ColorManagementSystemController.OnTestButton");

		CurrentTimeState = NextTimeState;
	}

	#endregion
}
