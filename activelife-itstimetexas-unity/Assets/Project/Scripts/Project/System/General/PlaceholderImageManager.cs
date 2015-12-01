using UnityEngine;
using System.Collections;

namespace ITT.System
{
	public class PlaceholderImageManager : ITTSingleton<PlaceholderImageManager>
	{
		public Texture[] Images;

		protected PlaceholderImageManager() {}

		void Start()
		{
			if (null == Images || Images.Length == 0)
			{
				Debug.LogError("PlaceholderImageManager: no images!");
				enabled = false;
			}
		}

		/// <summary>
		/// Returns a random image
		/// </summary>
		/// <returns>
		/// A random image.
		/// </returns>
		/// <param name="seed">
		/// If provided, the seed to get the random image with.
		/// </param>
		public Texture GetRandomImage(int? seed = null)
		{
			if (null == Images || Images.Length == 0)
				return null;

			if (null != seed) {
				Random.seed = (int)seed;
			}
			return Images[Random.Range(0,Images.Length)];
		}
	}
}