using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using ITT.System;

class ActivityListComparer : System.Collections.Generic.IEqualityComparer<ActivityList> 
{
	public bool Equals(ActivityList x, ActivityList y)
	{
		return x.Equals(y);
	}

	public int GetHashCode(ActivityList obj)
	{
		return obj.GetHashCode();
	}
}

class SponsorListComparer : System.Collections.Generic.IEqualityComparer<SponsorList> 
{
	public bool Equals(SponsorList x, SponsorList y)
	{
		return x.Equals(y);
	}
	
	public int GetHashCode(SponsorList obj)
	{
		return obj.GetHashCode();
	}
}

class UserProfileComparer : System.Collections.Generic.IEqualityComparer<UserProfile> 
{
	public bool Equals(UserProfile x, UserProfile y)
	{
		return x.Equals(y);
	}
	
	public int GetHashCode(UserProfile obj)
	{
		return obj.GetHashCode();
	}
}

class UserFlagsEntryComparer : System.Collections.Generic.IEqualityComparer<UserFlagsEntry> 
{
	public bool Equals(UserFlagsEntry x, UserFlagsEntry y)
	{
		return x.Equals(y);
	}
	
	public int GetHashCode(UserFlagsEntry obj)
	{
		return obj.GetHashCode();
	}
}

class LocationDataEntryComparer : System.Collections.Generic.IEqualityComparer<LocationDataEntry> 
{
	public bool Equals(LocationDataEntry x, LocationDataEntry y)
	{
		return x.Equals(y);
	}
	
	public int GetHashCode(LocationDataEntry obj)
	{
		return obj.GetHashCode();
	}
}

[Serializable]
class TypeComparer : System.Collections.Generic.IEqualityComparer<Type> 
{
	public bool Equals(Type x, Type y)
	{
		return x.Equals(y);
	}
	
	public int GetHashCode(Type obj)
	{
		return obj.GetHashCode();
	}
}