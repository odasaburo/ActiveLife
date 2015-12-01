using UnityEngine;
using System.Collections;

namespace ITT.Scene
{
	public interface IScreenViewBase
	{
		IEnumerator OnDisplay();

		IEnumerator OnHide();

		IEnumerator ForcedReverseIn();

		IEnumerator ForcedReverseOut();

		void AssignButtonDelegates();

	}
}