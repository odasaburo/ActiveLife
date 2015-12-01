using UnityEngine;
using System.Collections;

public class UIColorChangeController : MonoBehaviour
{
	#region Editor Variables

	private UIColorChangeModel _uiColorChangeModel;

	[Space(10)]
	public bool useButtonAutoFill = false;
	public bool useCustomButtonHighlightColor = false;
	public Color customButtonHighlightColor = Color.blue;
	private Color _customButtonColor = Color.clear;
	
	[Space(10)]
	public bool useLabelAutoFill = false;
	public bool useCustomLabelHighlightColor = false;
	public Color customLabelHighlightColor = Color.red;
	private Color _customLabelColor = Color.clear;

	[Space(10)]
	public bool useSpriteAutoFill = false;
	public bool useCustomSpriteHighlightColor = false;
	public Color customSpriteHighlightColor = Color.green;
	private Color _customSpriteColor = Color.clear;
	[Range(0f, 1f)]public float spritesNewAlpha = 1f;

	#endregion

	#region Unity Methods

	private void Start()
	{
		Init ();
	}

	private void OnEnable()
	{
		Init();
		UpdateButtons(_customButtonColor, true);
	}

	void OnDestroy()
	{
		UnSubscribeEvents();
	}
	
	#endregion

	#region Helper Methods

	private void Init()
	{
		if(null == _uiColorChangeModel)
		{
			_uiColorChangeModel = GetComponent<UIColorChangeModel>();
			// be sure that register the events just once.
			SubscribeEvents();
		}
		
		if (null == _uiColorChangeModel)
		{
			throw new MissingComponentException("Testing change color");
		}

		UpdateColor();
	}

	private void SubscribeEvents()
	{
		ColorManagementSystemController.Instance.OnCurrentTimeStateChangeEvent += UpdateColor;
	}

	private void UnSubscribeEvents()
	{
		ColorManagementSystemController.Instance.OnCurrentTimeStateChangeEvent -= UpdateColor;
	}

	private void UpdateColor()
	{
		FindNewColors();
		UpdateButtons(_customButtonColor, false);
		UpdateLabels(_customLabelColor);
		UpdateSprites(_customSpriteColor);
	}

	private void FindNewColors()
	{
		Color newHighlightColor = ColorManagementSystemController.GetHighlightColorByTimeState(ColorManagementSystemController.Instance.CurrentTimeState);

		_customButtonColor = (useCustomButtonHighlightColor) ? customButtonHighlightColor : newHighlightColor;
		_customLabelColor  = (useCustomLabelHighlightColor)  ? customLabelHighlightColor  : newHighlightColor;
		_customSpriteColor = (useCustomSpriteHighlightColor) ? customSpriteHighlightColor : newHighlightColor;
	}

	private void UpdateButtons(Color updatedButtonColor, bool forceNormalState)
	{
		/* If there ar no buttons are added to the model script via inspector, 
		   look for UIButtons on the gameObject and give them an updated highlight color*/

		int uiButtonArrayLength = _uiColorChangeModel.someUIButton.Length;

		if(
			(uiButtonArrayLength == 0) && 
			(true == useButtonAutoFill)
			)
		{
			UIButtonColor[] thisObjectsButtons = gameObject.GetComponents<UIButtonColor>();
			if(null != thisObjectsButtons && thisObjectsButtons.Length != 0)
			{
				for(int i = 0; i < thisObjectsButtons.Length; i++)
				{
					if(null != thisObjectsButtons[i] && thisObjectsButtons[i].subjectToColorChange)
					{
						if(forceNormalState == true)
							thisObjectsButtons[i].SetState(UIButtonColor.State.Normal, true);
						else
							thisObjectsButtons[i].pressed = thisObjectsButtons[i].hover = updatedButtonColor;
					}
				}
			}
		}
		else // If there are buttons already assigned in the model, update only them with the newhighlight color
		{
			for(int x = 0; x < uiButtonArrayLength; x++)
			{
				if(null != _uiColorChangeModel.someUIButton[x])
				{
					UIButtonColor[] theseButtons = _uiColorChangeModel.someUIButton[x].gameObject.GetComponents<UIButtonColor>();
					if (null != theseButtons && theseButtons.Length > 0)
					{
						for(int i = 0; i < theseButtons.Length; i++)
						{
							if(null != theseButtons[i])
							{
								if(forceNormalState == true)
									theseButtons[i].SetState(UIButtonColor.State.Normal, true);
								else
									theseButtons[i].pressed = theseButtons[i].hover = updatedButtonColor;
							}
						}
					}

				}
			}
		}
	}

	private void UpdateLabels(Color updatedLabelColor)
	{
		/* If there ar no labels are added to the model script via inspector, 
		   look for UILabels on the gameObject and give them an updated highlight color*/
		
		int uiLabelArrayLength = _uiColorChangeModel.someUILabel.Length;
		
		if((uiLabelArrayLength == 0) &&
		   (true == useLabelAutoFill)
		   )
		{
			UILabel[] thisObjectsLabels = gameObject.GetComponents<UILabel>();
			if(null != thisObjectsLabels && thisObjectsLabels.Length != 0)
			{
				for(int i = 0; i < thisObjectsLabels.Length; i++)
				{
					if(null != thisObjectsLabels[i])
					{
						thisObjectsLabels[i].color = updatedLabelColor;
					}
				}
			}
		}
		else // If there are buttons already assigned in the model, update only them with the newhighlight color
		{
			for(int x = 0; x < uiLabelArrayLength; x++)
			{
				if(null != _uiColorChangeModel.someUILabel[x])
				{
					UILabel[] theseLabels = _uiColorChangeModel.someUILabel[x].gameObject.GetComponents<UILabel>();
					if (null != theseLabels && theseLabels.Length > 0)
					{
						for(int i = 0; i < theseLabels.Length; i++)
						{
							if(null != theseLabels[i])
							{
								theseLabels[i].color = updatedLabelColor;
							}
						}
					}

				}
			}
		}
	}

	private void UpdateSprites(Color updatedSpriteColor)
	{
		/* If there ar no sprites are added to the model script via inspector, 
		   look for UISprites on the gameObject and give them an updated highlight color*/
		
		int uiSpriteArrayLength = _uiColorChangeModel.someUISprite.Length;
		
		if((uiSpriteArrayLength == 0) &&
		   (true == useSpriteAutoFill)
		   )
		{
			UISprite[] thisObjectsSprite = gameObject.GetComponents<UISprite>();
			if(null != thisObjectsSprite && thisObjectsSprite.Length != 0)
			{
				for(int i = 0; i < thisObjectsSprite.Length; i++)
				{
					if(null != thisObjectsSprite[i])
					{
						updatedSpriteColor.a = spritesNewAlpha;
						thisObjectsSprite[i].color = updatedSpriteColor;
					}
				}
			}
		}
		else // If there are sprites already assigned in the model, update only them with the newhighlight color
		{
			for(int x = 0; x < uiSpriteArrayLength; x++)
			{
				if(null != _uiColorChangeModel.someUISprite[x])
				{
					UISprite[] theseSprites = _uiColorChangeModel.someUISprite[x].gameObject.GetComponents<UISprite>();
					if (null != theseSprites && theseSprites.Length > 0)
					{
						for(int i = 0; i < theseSprites.Length; i++)
						{
							if(null != theseSprites[i])
							{
								updatedSpriteColor.a = spritesNewAlpha;
								theseSprites[i].color = updatedSpriteColor;
							}
						}
					}

				}
			}
		}
	}

	#endregion
}
