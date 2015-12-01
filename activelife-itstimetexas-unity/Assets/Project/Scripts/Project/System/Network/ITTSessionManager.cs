using UnityEngine;
using System.Collections;
using System;

namespace ITT.System
{
	[global::System.Serializable]
	public class ITTSessionBlob
	{	
		public string UserToken { get; private set; }
		public string UserUid { get; private set; }

		public string FacebookId {get; private set;}
		public string FacebookEmail {get; private set;}

		public void UpdateUserToken(string userToken) {
			UserToken = userToken;
		}

		public void UpdateUserUid(string userUid) {
			UserUid = userUid;
		}

		public void UpdateFacebookId(string facebookId) {
			FacebookId = facebookId;
		}

		public void UpdateFacebookEmail(string facebookEmail) {
			FacebookEmail = facebookEmail;
		}

		public void Reset()
		{
			UserToken = null;
			UserUid = null;
			FacebookId = null;
			FacebookEmail = null;
		}
	}
}