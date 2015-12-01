using UnityEngine;
using System.Collections;

[RequireComponent(typeof(UIColorChangeController))]
public class UIColorChangeModel : MonoBehaviour
{
	[Space(10)] public UIButtonColor[] someUIButton;
	[Space(5)] public UILabel[] someUILabel;
	[Space(5)] public UISprite[] someUISprite;
}
