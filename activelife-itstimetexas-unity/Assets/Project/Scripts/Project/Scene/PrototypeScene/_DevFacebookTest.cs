using UnityEngine;
using System.Collections;
using Facebook.Unity;

public class _DevFacebookTest : MonoBehaviour 
{
	public Texture2D tex;

	IEnumerator Start () 
	{
		Debug.Log("Initializing FB");
		FacebookWrapper.Initialize();

		while (!FB.IsInitialized)
			yield return null;

		Debug.Log("Starting login process");
		StartCoroutine(FBLoginProcess());
	}

	private IEnumerator FBLoginProcess()
	{
		Debug.Log("Prompt FB login");
		yield return StartCoroutine(FacebookWrapper.Login());

		if (!FacebookWrapper.IsLoggedIn)
		{
			Debug.LogError("Login failed/canceled!");
		}
		else
		{
			Debug.Log("FB connected! Testing permissions.");
			FB.API("/me/permissions", HttpMethod.GET, delegate (IGraphResult response) {
				if (!string.IsNullOrEmpty(response.Error))
				{
					Debug.LogError("Error retrieving permissions.");
				}
				else
				{
					Debug.Log ("Retrieved permissions: " + response.RawResult.ToString());
				}
			});



			WWWForm wwwForm = new WWWForm();
			wwwForm.AddField("message", "I just discovered the activity \"Monday Morning Walk\". Discover more activities at: https://choosehealthier.org/node/10374");
			wwwForm.AddBinaryData("image", tex.EncodeToPNG());
			//wwwForm.AddField("access_token", FB.AccessToken);

			//FB.API("/me/feed", Facebook.HttpMethod.POST, OnPostSuccess, wwwForm);
			FB.API("/me/photos", HttpMethod.POST, OnPostSuccess, wwwForm);
		}
	}

	private void OnPostSuccess(IGraphResult result)
	{
		if (!string.IsNullOrEmpty(result.Error)) 
		{
			Debug.LogError("Post error: " + result.Error);
		}
		else
		{
			Debug.Log("Post successful!");
		}
	}


}
