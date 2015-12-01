using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ITT.System
{
	[global::System.Serializable]
	public class ActivityDataModel : BaseCardData
	{
		public override string Type
		{
			get
			{
				return "activity";
			}
			set
			{
				base.Type = value;
			}
		}
	}
}