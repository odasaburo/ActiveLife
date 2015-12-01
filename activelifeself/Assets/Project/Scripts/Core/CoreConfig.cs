using UnityEngine;

namespace ChaoticMoon.Core
{
	public enum LogChannels
	{
		DEBUG,
		WARN,
		ERROR
		// Extend this as needed.
	}
	
	public static class ConfigFormatterOptions
	{
		public static System.Runtime.Serialization.IFormatter GetFormatter()
		{
			//return new ChaoticMoon.JsonFx.JSONFormatter(); 
			return new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
		}
		
		public static string GetFormatterExtension()
		{
			//return ".txt"; 
			return ".bytes";
		}
		
		public static string GetRelativeRuntimeConfigOverridePath()
		{
			return "";
		}
	}
	
	public static class ConfigPostBuild
	{
		public const int PostBuildPriority = 0;
	}
}