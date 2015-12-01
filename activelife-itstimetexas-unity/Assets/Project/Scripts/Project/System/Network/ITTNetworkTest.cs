using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ITT.System {
	public class ITTNetworkTest : MonoBehaviour {

		public const string UND_ARRAY = "und";

		IEnumerator Start () {
			// Test the cycle
			yield return StartCoroutine(TestUserRegistration());
			yield return StartCoroutine(TestUserLogin());
			yield return StartCoroutine(TestUserUpdate());
			yield return StartCoroutine(TestRetrieveUserData());
			yield return StartCoroutine(TestUserLogout());
		}

		IEnumerator TestUserRegistration() {

			RegistrationDataModel rdm = new RegistrationDataModel();
			rdm.username = "chaoticmoonTest42";
			rdm.password = "chaoticchaos";
			rdm.email = "chaos42@chaoticmoon.com";
			rdm.legal_accept = "1";

			yield break;
		}

		IEnumerator TestUserLogin() {
			yield break;
		}

		IEnumerator TestUserUpdate() {

			yield break;
		}

		IEnumerator TestUserLogout() {

			yield break;
		}

		IEnumerator TestRetrieveUserData() {

			yield break;
		}
	}
}

