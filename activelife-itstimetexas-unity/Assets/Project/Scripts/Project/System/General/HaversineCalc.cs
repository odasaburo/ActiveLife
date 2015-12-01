using UnityEngine;
using System.Collections;
using System;

namespace ITT.System
{
	public enum DistanceType
	{
		Miles,
		Kilometers
	};

	public struct Position
	{
		public float Latitude;
		public float Longitude;
	}

	public static class HaversineCalc 
	{
		public static float Distance(float latA, float lonA, float latB, float lonB, DistanceType type)
		{
			float R = (type == DistanceType.Miles) ? 3960 : 6371;

			float fLat = toRadian(latB - latA);
			float fLon = toRadian(lonB - lonA);

			float a = Mathf.Sin(fLat / 2) * Mathf.Sin(fLat / 2) + Mathf.Cos(toRadian(latA)) * Mathf.Cos(toRadian(latB)) * Mathf.Sin(fLon / 2) * Mathf.Sin(fLon / 2);
			float c = 2 * Mathf.Asin(Mathf.Min(1, Mathf.Sqrt(a)));
			float d = R * c;

			return d;
		}

		private static float toRadian(float val)
		{
			return (Mathf.PI / 180.0f) * val;
		}
	}
}

