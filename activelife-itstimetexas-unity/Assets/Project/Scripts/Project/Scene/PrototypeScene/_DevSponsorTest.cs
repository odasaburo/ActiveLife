using UnityEngine;
using System.Collections;
using ITT.System;

public class _DevSponsorTest : MonoBehaviour {

	// Use this for initialization
	void Start () 
	{
		ITTDataCache.Instance.Initialize();
		//ITTDataCache.Instance.Data.GetDataEntry<SponsorList>(OnRetrievedSponsorList);
	}

	void OnRetrievedSponsorList(SponsorList sponsorList)
	{
		Debug.Log("Retrieved sponsors: " + sponsorList.Data.Length);
	}
}
