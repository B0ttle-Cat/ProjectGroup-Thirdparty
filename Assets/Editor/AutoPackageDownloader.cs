#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEditor.PackageManager;

using UnityEngine;

[InitializeOnLoad]
public static class AutoPackageDownloader
{
	static AutoPackageDownloader()
	{
		List<string> gitUrlList = new List<string>()
		{
			"https://github.com/marijnz/unity-toolbar-extender.git", // https://github.com/marijnz/unity-toolbar-extender
			"https://github.com/antonysze/unity-custom-play-button.git", // https://github.com/antonysze/unity-custom-play-button/tree/main
			//"com.unity.toonshader"
		};

		var request =  Client.List();
		while(!request.IsCompleted) { }

		List<string> ready = new List<string>();
		foreach(var package in request.Result)
		{
			try
			{
				string packageId = package.packageId;
				int atIndex = packageId.IndexOf('@');
				if(atIndex >= 0)
				{
					packageId = packageId.Substring(atIndex + 1);
					bool startsWithHttpOrHttps = Regex.IsMatch(packageId, "^(http:|https:)");
					if(startsWithHttpOrHttps)
					{
						ready.Add(packageId);
					}
				}
			}
			catch(Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		for(int i = 0 ; i < gitUrlList.Count ; i++)
		{
			if(ready.Contains(gitUrlList[i]))
			{
				gitUrlList.RemoveAt(i--);
			}
		}

		if(gitUrlList.Count > 0)
		{
			string gitUrls = string.Join("\n - ", gitUrlList);
			bool shouldInstall = EditorUtility.DisplayDialog("패키지 설치 확인", $"Git 패키지를 자동으로 설치하시겠습니까?\n - {gitUrls}", "설치", "취소");
			if(shouldInstall)
			{
				for(int i = 0 ; i < gitUrlList.Count ; i++)
				{
					Client.Add(gitUrlList[i]);
				}
			}
		}
	}
}
#endif
