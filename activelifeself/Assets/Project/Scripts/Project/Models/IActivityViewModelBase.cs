using UnityEngine;
using System.Collections;
using ITT.System;

namespace ITT.Scene
{
	public interface IActivityViewModelBase 
	{
		bool IsPopulated();
		void Clear();
		void Populate(BaseCardData data, UIEventListener.VoidDelegate onClickedDelegate = null,  bool allowRecommending = false);
	}
}