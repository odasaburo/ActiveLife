using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ITT.System {
	[global::System.Serializable]
	public class UserDataModel {
		public string Username { get; set; }
		public string Auth_Data { get; set; }
		public string Session_Token { get; set; }
		public Int64 Id { get; set; }
		public string Email { get; set; }

	}
	
	public static class UnixDateTimeHelper {
		private const string InvalidUnixEpochErrorMessage = "Unix epoch starts January 1st, 1970";

		public static DateTime FromUnixTime(this Int64 self) {
			var ret = new DateTime(1970, 1, 1);
			return ret.AddSeconds(self);
		}

		public static Int64 ToUnixTime(this DateTime self) {
			if(self == DateTime.MinValue) {
				return 0;
			}

			var epoch = new DateTime(1970, 1, 1);
			var delta = self - epoch;

			if(delta.TotalSeconds < 0) {
				throw new ArgumentOutOfRangeException(InvalidUnixEpochErrorMessage);
			}

			return (long) delta.TotalSeconds;
		}
	}

}

