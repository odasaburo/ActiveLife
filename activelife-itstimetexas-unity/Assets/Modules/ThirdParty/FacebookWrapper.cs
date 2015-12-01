using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Facebook.Unity;

public static class FacebookWrapper
{
	public static bool IsInitialized { get; set; }

	public static bool IsLoggedIn { get { return FB.IsLoggedIn; } }

	static FacebookWrapper()
	{
		IsInitialized = false;
	}
	
	public static void Initialize()
	{
		FB.Init(
			() =>
			{
				Debug.Log("FB.Init complete");
				FacebookWrapper.IsInitialized = true;
				/*FB.PublishInstall(result =>
					{
						Debug.Log("FB.PublishInstall: " + result.Text);
					}
				);*/
			},
			isUnityShown =>
			{
				Debug.Log("FB.Init Unity is shown: " + isUnityShown);
			}
		);
	}

	public static IEnumerator Login()
	{
		bool isDone = false;
		FB.LogInWithPublishPermissions(new List<string>() { "public_profile","user_friends","publish_actions" },
		         result => { Debug.Log("FB.Login: " + result.RawResult.ToString()); isDone = true; }
		);
		while (!isDone) yield return null;

	}

	public static void ReportScore(int score)
	{
		FB.API(
			query: "/me/scores",
			method: HttpMethod.POST,
			callback: response =>
			{
				if (!string.IsNullOrEmpty(response.Error))
				{
					Debug.Log("FB.ReportScore Error: " + response.Error);
				}
				else
				{
					Debug.Log("FB.ReportScore completed");
				}
			},
			formData: new Dictionary<string, string>() {{ "score", score.ToString()}}
		);
	}
}
