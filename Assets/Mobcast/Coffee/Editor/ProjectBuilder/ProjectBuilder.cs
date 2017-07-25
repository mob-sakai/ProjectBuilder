using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Mobcast.Coffee.Build
{
	/// <summary>
	/// プロジェクトのビルド設定を管理するクラスです.
	/// このクラスを継承すると、プロジェクトごとの独自処理を追加できます.
	/// </summary>
	public class ProjectBuilder : ScriptableObject
	{
		public const string kLogType = "#### [ProjectBuilder] ";

		//-------------------------------
		//	ビルド概要.
		//-------------------------------

		/// <summary>Buid AssetBundle.</summary>
		[Tooltip("Buid AssetBundle.")]
		public bool assetBundleBuild;

		/// <summary>AssetBundle options.</summary>
		[Tooltip("AssetBundle options.")]
		public BuildAssetBundleOptions bundleOptions;

		/// <summary>ビルドプラットフォームを指定します.</summary>
		[Tooltip("ビルドプラットフォームを指定します.")]
		public BuildTarget buildTarget;

		/// <summary>Build target group for this builder asset.</summary>
		public BuildTargetGroup buildTargetGroup { get { return BuildPipeline.GetBuildTargetGroup(buildTarget); } }

		/// <summary>BuildOptions.Development and BuildOptions.AllowDebugging.</summary>
		[Tooltip("BuildOptions.Development and BuildOptions.AllowDebugging.")]
		public bool developmentBuild = false;

		/// <summary>Define Script Symbols. If you have multiple definitions, separate with a semicolon(;)</summary>
		[Tooltip("Define Script Symbols.\nIf you have multiple definitions, separate with a semicolon(;)")]
		[TextArea(1, 5)]
		public string defineSymbols = "";

		/// <summary>端末に表示されるプロダクト名を指定します.</summary>
		[Tooltip("端末に表示されるプロダクト名を指定します.")]
		public string productName;

		/// <summary>会社名を指定します.</summary>
		[Tooltip("会社名を指定します.")]
		public string companyName;

		/// <summary>プロダクトのバンドル識別子を指定します.</summary>
		[Tooltip("プロダクトのバンドル識別子を指定します.")]
		public string applicationIdentifier;

		/// <summary>ビルド成果物(Xcode projectやAndroid project, apk, exe等)の出力パス.</summary>
		public string outputPath
		{
			get
			{
				if(assetBundleBuild)
					return "AssetBundles";
				else if (buildTarget == BuildTarget.Android && !EditorUserBuildSettings.exportAsGoogleAndroidProject)
					return "build.apk";
				else
					return "build";
			}
		}

		/// <summary>ビルド成果物出力先フルパス.</summary>
		public string outputFullPath { get { return Path.Combine(Util.projectDir, outputPath); } }


		//-------------------------------
		//	バージョン設定.
		//-------------------------------
		/// <summary>アプリのバージョンを指定します.</summary>
		[Tooltip("アプリのバージョンを指定します.")]
		public string version;

		/// <summary>バンドルコードを指定します.Androidの場合はVersionCode, iOSの場合はBuildNumberに相当します.この値は、リリース毎に更新する必要があります.</summary>
		[Tooltip("整数のバージョンコードを指定します.\nAndroidの場合はVersionCode, iOSの場合はBuildNumberに相当します.\nこの値は、リリース毎に更新する必要があります.")]
		public int versionCode = 0;

		public PlatformSettings_iOS iosSettings = new PlatformSettings_iOS();
		public PlatformSettings_Android androidSettings = new PlatformSettings_Android();


		[System.Serializable]
		public class SceneSetting
		{
			public bool enable = true;
			public string name;
		}

		public SceneSetting[] scenes = new SceneSetting[]{ };


		//-------------------------------
		//	継承関連.
		//-------------------------------
		/// <summary>設定適用後コールバック.</summary>
		protected virtual void OnApplySetting()
		{
		}

		//		/// <summary>ビルド後コールバック.</summary>
		//		protected virtual void OnPostBuildPlayer()
		//		{
		//		}


		//-------------------------------
		//	Unityコールバック.
		//-------------------------------
		public virtual void Reset()
		{
			buildTarget = EditorUserBuildSettings.activeBuildTarget;
			productName = PlayerSettings.productName;
			companyName = PlayerSettings.companyName;
#if UNITY_5_6_OR_NEWER
			applicationIdentifier = PlayerSettings.GetApplicationIdentifier(buildTargetGroup);
#else
			applicationIdentifier = PlayerSettings.bundleIdentifier;
#endif
			version = PlayerSettings.bundleVersion;
			defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

			//Platform settings.
			androidSettings.Reset();
			iosSettings.Reset();
		}


		//-------------------------------
		//	アクション.
		//-------------------------------
		/// <summary>
		/// Define script symbol.
		/// </summary>
		public bool DefineSymbol()
		{
			if (assetBundleBuild)
				return false;
			
			var oldDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
			List<string> symbolList = new List<string>(defineSymbols.Split(',', ';', '\n', '\r'));

			// Symbols specified in command line arguments.
			if (Util.executeArguments.ContainsKey(Util.OPT_APPEND_SYMBOL))
			{
				var argSymbols = Util.executeArguments[Util.OPT_APPEND_SYMBOL].Split(',', ';', '\n', '\r');

				// Include symbols.
				foreach (var s in argSymbols.Where(x=>x.IndexOf("!") != 0))
					symbolList.Add(s);

				// Exclude symbols start with '!'.
				foreach (var s in argSymbols.Where(x=>x.IndexOf("!") == 0 && symbolList.Contains(x.Substring(1))))
					symbolList.Remove(s.Substring(1));
			}

			// Update define script symbol.
			string symbols = symbolList.Count == 0 ? "" : symbolList.Aggregate((a, b) => a + ";" + b);
			PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, symbols);
			UnityEngine.Debug.LogFormat("{0}DefineSymbol is updated : {1} -> {2}", kLogType, oldDefineSymbols, symbols);

			// Any symbol has been chenged?
			return oldDefineSymbols != symbols;
		}

		//-------------------------------
		//	アクション.
		//-------------------------------
		/// <summary>
		/// PlayerSettingにビルド設定を反映します.
		/// </summary>
		public void ApplySettings()
		{
			if (assetBundleBuild)
				return;
			
			//ビルド情報を設定します.
#if UNITY_5_6_OR_NEWER
			PlayerSettings.SetApplicationIdentifier(buildTargetGroup, applicationIdentifier);
#else
			PlayerSettings.bundleIdentifier = applicationIdentifier;
#endif
			PlayerSettings.productName = productName;
			PlayerSettings.companyName = companyName;

			EditorUserBuildSettings.development = developmentBuild;
			EditorUserBuildSettings.allowDebugging = developmentBuild;

			//アプリバージョン.
			//実行引数に開発ビルド番号定義がある場合、ビルド番号を再定義します.
			PlayerSettings.bundleVersion = version;
			string buildNumber;
			if (developmentBuild && Util.executeArguments.TryGetValue(Util.OPT_DEV_BUILD_NUM, out buildNumber) && !string.IsNullOrEmpty(buildNumber))
				PlayerSettings.bundleVersion += "." + buildNumber;
			File.WriteAllText(Path.Combine(Util.projectDir, "BUILD_VERSION"), PlayerSettings.bundleVersion);

			// Scene Settings.
			EditorBuildSettings.scenes = EditorBuildSettings.scenes
				.Where(x => x.enabled || scenes.Any(y=>y.enable && y.name == Path.GetFileName(x.path)))
				.Where(x=>!scenes.Any(y=>!y.enable && y.name == Path.GetFileName(x.path)))
				.ToArray();

			//Platform settings.
			iosSettings.ApplySettings(this);
			androidSettings.ApplySettings(this);

			OnApplySetting();
			AssetDatabase.SaveAssets();
		}

		/// <summary>
		/// BuildPipelineによるビルドを実行します.
		/// </summary>
		/// <returns>ビルドに成功していればtrueを、それ以外はfalseを返す.</returns>
		/// <param name="autoRunPlayer">Build & Runモードでビルドします.</param>
		public bool BuildPlayer(bool autoRunPlayer)
		{
			if (assetBundleBuild)
			{
				if (!Directory.Exists(outputPath))
					Directory.CreateDirectory(outputPath);
				BuildPipeline.BuildAssetBundles(outputPath, bundleOptions, buildTarget);
				return true;
			}

			// Build options.
			BuildOptions opt = developmentBuild ? (BuildOptions.Development & BuildOptions.AllowDebugging) : BuildOptions.None
			                   | (autoRunPlayer ? BuildOptions.AutoRunPlayer : BuildOptions.None);

			// Scenes to build.
			string[] scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();

			// Start build.
			UnityEngine.Debug.Log(kLogType + "BuildPlayer is started. Defined symbols : " + PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup));
			string errorMsg = BuildPipeline.BuildPlayer(scenes, outputFullPath, buildTarget, opt);

			if (string.IsNullOrEmpty(errorMsg))
			{
				UnityEngine.Debug.Log(kLogType + "BuildPlayer is finished successfuly.");
				Util.RevealOutputInFinder(outputFullPath);
				return true;
			}
			else
			{
				UnityEngine.Debug.LogError(kLogType + "BuildPlayer is failed : " + errorMsg);
				return false;
			}
		}

		/// <summary>
		/// Build method for CUI(-executeMethod option).
		/// </summary>
		static void Build()
		{
			Util.StartBuild(Util.GetBuilderFromExecuteArgument(), false);
		}

#if UNITY_CLOUD_BUILD
		/// <summary>
		/// Pre-export method for Unity Cloud Build.
		/// </summary>
		static void PreExport(UnityEngine.CloudBuild.BuildManifestObject manifest)
		{
			Util.executeArguments[Util.OPT_DEV_BUILD_NUM] = manifest.GetValue("buildNumber", "unknown");
			Util.GetBuilderFromExecuteArgument().ApplySettings();
		}
#endif
	}
}