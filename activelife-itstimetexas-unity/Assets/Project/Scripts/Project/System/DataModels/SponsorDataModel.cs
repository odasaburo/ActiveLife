using UnityEngine;
using System;
using System.Collections.Generic;


namespace ITT.System
{
	[global::System.Serializable]
	public class SponsorDataModel : BaseCardData
	{
		public string logo;
		public string web;

		public override string Type {
			get {
				return "sponsor";
			}
			set {
				base.Type = value;
			}
		}
	}
}