using UnityEngine;
using System.Collections;
using System;

namespace ITT.Prototype
{
	public enum ExampleScene
	{
		ScrollingCardsExample = 2,
		StackedCardsExample
	}
	
	public class ExampleButtonHandlerScript : MonoBehaviour {
		
		void Start()
		{
//			Newtonsoft.Json.JsonConvert.SerializeObject(new object());
			JsonFx.Json.JsonWriter.Serialize(new object());
		}
		
		public void LoadExampleScene(UILabel exampleScene)
		{
			ExampleScene parsedExampleScene = (ExampleScene)Enum.Parse(typeof(ExampleScene), exampleScene.text.Replace(" ", string.Empty)) ;

			switch(parsedExampleScene)
			{
			case ExampleScene.ScrollingCardsExample:
				Application.LoadLevel((int)ExampleScene.ScrollingCardsExample);
				break;
			case ExampleScene.StackedCardsExample:
				Application.LoadLevel((int)ExampleScene.StackedCardsExample);
				break;
			default:
				break;

			}
		}
	}
}

