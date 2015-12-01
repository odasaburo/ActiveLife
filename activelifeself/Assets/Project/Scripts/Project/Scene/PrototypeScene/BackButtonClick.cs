using UnityEngine;
using System.Collections;

namespace ITT.Prototype
{
	public class BackButtonClick : MonoBehaviour {

		public const int mainScene = 1;

		void OnClick ()
		{
			Application.LoadLevel(mainScene);
		}
	}
}

