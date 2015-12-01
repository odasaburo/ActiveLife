using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.IO;
using System.Collections;

public static class ITTPostProcessiOS {

	public const string BUILD_LOCATION = "Choose_Healthier_builds/";
	[PostProcessBuild]
	public static void OnPostProcessBuild(BuildTarget target, string path)
	{
		#if UNITY_IPHONE
		{	
			string baseProjectpath = Application.dataPath;
			baseProjectpath = baseProjectpath.Substring(0, baseProjectpath.Length - 6);

			string device = "iOSandIpad";
			if (PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPadOnly)
				device = "iPad";
			else if (PlayerSettings.iOS.targetDevice == iOSTargetDevice.iPhoneOnly)
				device = "iPhone";

			Debug.Log("OnPostProcessBuild - START on: "+device);
		}
		#else
		// We do nothing if not iPhone
		Debug.Log("OnPostProcessBuild - Warning: This is not an iOS build");
		#endif     
		Debug.Log("OnPostProcessBuild - STOP");
	}

	public static void updateiPhoneFile(string filePath, string lineToFind, string[] linesToAdd, bool postFix) {
		string[] lines = System.IO.File.ReadAllLines(filePath);
		FileStream filestr = new FileStream(filePath, FileMode.Create);
		filestr.Close() ;
		StreamWriter fUpdatedFile = new StreamWriter(filePath);

		foreach (string line in lines) {
			if(postFix) {
				fUpdatedFile.WriteLine(line);
				if (line.StartsWith(lineToFind)) {
					foreach (string addLine in linesToAdd) {
						fUpdatedFile.WriteLine(addLine);
					}
				}
			} else {
				if (line.StartsWith(lineToFind)) {
					foreach (string addLine in linesToAdd) {
						fUpdatedFile.WriteLine(addLine);
					}
				}
				fUpdatedFile.WriteLine(line);
			}
		}
		fUpdatedFile.Close();
	}

}
