using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mobcast.Coffee.Build;
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Mobcast.Coffee.Build
{
	[System.Serializable]
	public class BuildTargetSettings_iOS : IBuildTargetSettings
	{
		public BuildTarget buildTarget{get{ return BuildTarget.iOS;}}

		public Texture icon{get{ return EditorGUIUtility.FindTexture("BuildSettings.iPhone.Small");}}

		/// <summary>Enable automatically sign.</summary>
		[Tooltip("Enable automatically sign.")]
		public bool automaticallySign = false;

		/// <summary>Developer Team Id.</summary>
		[Tooltip("Developer Team Id.")]
		public string developerTeamId = "";

		/// <summary>Code Sign Identifier.</summary>
		[Tooltip("Code Sign Identifier.")]
		public string codeSignIdentity = "";


		/// <summary>Provisioning Profile Id. For example: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx</summary>
		[Tooltip("Provisioning Profile Id.\nFor example: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx")]
		public string profileId = "";


		/// <summary>Provisioning Profile Specifier. For example: com campany app_name</summary>
		[Tooltip("Provisioning Profile Specifier.\nFor example: com campany app_name")]
		public string profileSpecifier = "";



		/// <summary>Support languages. If you have multiple definitions, separate with a semicolon(;)</summary>
		[Tooltip("Support languages.\nIf you have multiple definitions, separate with a semicolon(;)")]
		public string languages = "jp;en";


		/// <summary>Generate exportOptions.plist automatically for xcodebuild (XCode7 and later).</summary>
		[Tooltip("Generate exportOptions.plist under build path for xcodebuild (XCode7 and later).")]
		public bool generateExportOptionPlist = false;

		/// <summary>The method of distribution, which can be set as any of the following: app-store, ad-hoc, package, enterprise, development, developer-id.</summary>
		[Tooltip("The method of distribution, which can be set as any of the following:\napp-store, ad-hoc, package, enterprise, development, developer-id.")]
		public string exportMethod = "development";

		/// <summary>Option to include Bitcode.</summary>
		[Tooltip("Option to include Bitcode.")]
		public bool uploadBitcode = false;

		/// <summary>Option to include symbols in the generated ipa file.</summary>
		[Tooltip("Option to include symbols in the generated ipa file.")]
		public bool uploadSymbols = false;

		/// <summary>Entitlements file(*.entitlement).</summary>
		[Tooltip("Entitlements file(*.entitlements).")]
		public string entitlementsFile = "";

		/// <summary>Apple services. If you have multiple definitions, separate with a semicolon(;)</summary>
		[Tooltip("Apple services.\nIf you have multiple definitions, separate with a semicolon(;)")]
		public string services = "";

		/// <summary>Additional frameworks. If you have multiple definitions, separate with a semicolon(;)</summary>
		[Tooltip("Additional frameworks.\nIf you have multiple definitions, separate with a semicolon(;)")]
		public string frameworks = "";





		static readonly string[] s_AvailableExportMethods =
		{
			"app-store",
			"ad-hoc",
			"package",
			"enterprise",
			"development",
			"developer-id",
		};

		static readonly string[] s_AvailableLanguages =
		{
			"jp",
			"en",
		};


		static readonly string[] s_AvailableFrameworks =
		{
			"iAd.framework",
		};

		static readonly string[] s_AvailableServices =
		{
			"com.apple.ApplePay",
			"com.apple.ApplicationGroups.iOS",
			"com.apple.BackgroundModes",
			"com.apple.DataProtection",
			"com.apple.GameCenter",
			"com.apple.GameControllers.appletvos",
			"com.apple.HealthKit",
			"com.apple.HomeKit",
			"com.apple.InAppPurchase",
			"com.apple.InterAppAudio",
			"com.apple.Keychain",
			"com.apple.Maps.iOS",
			"com.apple.NetworkExtensions",
			"com.apple.Push",
			"com.apple.SafariKeychain",
			"com.apple.Siri",
			"com.apple.VPNLite",
			"com.apple.WAC",
			"com.apple.Wallet",
			"com.apple.iCloud",
		};



		public void Reset()
		{
#if UNITY_5_4_OR_NEWER
			developerTeamId = PlayerSettings.iOS.appleDeveloperTeamID;
#endif
#if UNITY_5_5_OR_NEWER
			automaticallySign = PlayerSettings.iOS.appleEnableAutomaticSigning;
			profileId = PlayerSettings.iOS.iOSManualProvisioningProfileID;
#endif
		}

		public void ApplySettings(ProjectBuilder builder)
		{
			PlayerSettings.iOS.buildNumber = builder.versionCode.ToString();
#if UNITY_5_4_OR_NEWER
			PlayerSettings.iOS.appleDeveloperTeamID = developerTeamId;
#endif
#if UNITY_5_5_OR_NEWER
			PlayerSettings.iOS.appleEnableAutomaticSigning = automaticallySign;
			if(!automaticallySign)
			{
				PlayerSettings.iOS.iOSManualProvisioningProfileID = profileId;
			}
#endif
		}


		/// <summary>
		/// Draws the ios settings.
		/// </summary>
		public void DrawSetting(SerializedObject serializedObject)
		{
			var settings = serializedObject.FindProperty("iosSettings");

			using (new EditorGUIEx.GroupScope("iOS Settings"))
			{
				// XCode Project.
				EditorGUILayout.LabelField("XCode Project", EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				{
					EditorGUIEx.TextFieldWithTemplate(settings.FindPropertyRelative("languages"), s_AvailableLanguages, true);
					EditorGUIEx.TextFieldWithTemplate(settings.FindPropertyRelative("frameworks"), s_AvailableFrameworks, true);
					EditorGUIEx.TextFieldWithTemplate(settings.FindPropertyRelative("services"), s_AvailableServices, true);
					EditorGUIEx.FilePathField(settings.FindPropertyRelative("entitlementsFile"), "Select entitlement file.", "", "entitlements");
				}
				EditorGUI.indentLevel--;

				// Signing.
				EditorGUILayout.LabelField("Signing", EditorStyles.boldLabel);
				var spAutomaticallySign = settings.FindPropertyRelative("automaticallySign");
				EditorGUI.indentLevel++;
				{
					EditorGUILayout.PropertyField(spAutomaticallySign);
					EditorGUILayout.PropertyField(settings.FindPropertyRelative("developerTeamId"));
					if (!spAutomaticallySign.boolValue)
					{
						EditorGUILayout.PropertyField(settings.FindPropertyRelative("codeSignIdentity"));
						EditorGUILayout.PropertyField(settings.FindPropertyRelative("profileId"));
						EditorGUILayout.PropertyField(settings.FindPropertyRelative("profileSpecifier"));
					}
				}
				EditorGUI.indentLevel--;


				// exportOptions.plist.
				EditorGUILayout.LabelField("exportOptions.plist Setting", EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				{
					var spGenerate = settings.FindPropertyRelative("generateExportOptionPlist");
					EditorGUILayout.PropertyField(spGenerate, new GUIContent("Generate Automatically"));
					if (spGenerate.boolValue)
					{
						EditorGUIEx.TextFieldWithTemplate(settings.FindPropertyRelative("exportMethod"), s_AvailableExportMethods, false);
						EditorGUILayout.PropertyField(settings.FindPropertyRelative("uploadBitcode"));
						EditorGUILayout.PropertyField(settings.FindPropertyRelative("uploadSymbols"));
					}
				}
				EditorGUI.indentLevel--;
			}
		}

		/// <summary>
		/// Raises the postprocess build event.
		/// </summary>
		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
		{
#if UNITY_IOS
			PlatformSettings_iOS current = Util.currentBuilder.iosSettings;
			if (buildTarget != BuildTarget.iOS || current == null)
				return;

			// Generate exportOptions.plist automatically.
			if (current.generateExportOptionPlist)
			{
				var plist = new PlistDocument();
				plist.root.SetString("teamID", current.developerTeamId);
				plist.root.SetString("method", current.exportMethod);
				plist.root.SetBoolean("uploadBitcode", current.uploadBitcode);
				plist.root.SetBoolean("uploadSymbols", current.uploadSymbols);

				// Generate exportOptions.plist into build path.
				plist.WriteToFile(Path.Combine(path, "exportOptions.plist"));
			}

			// Support languages.
			string[] languages = current.languages.Split(';');
			if (0 < languages.Length)
			{
				// Load Info.plist
				string infoPlistPath = Path.Combine(path, "Info.plist");
				var plist = new PlistDocument();
				plist.ReadFromFile(infoPlistPath);

				// Set default language.
				plist.root.SetString("CFBundleDevelopmentRegion", languages[0]);

				PlistElementArray bundleLocalizations = 
					plist.root.values.ContainsKey("CFBundleLocalizations") ? plist.root.values["CFBundleLocalizations"].AsArray()
				: plist.root.CreateArray("CFBundleLocalizations");

				// Add support language.
				foreach (var lang in current.languages.Split(';'))
				{
					if (bundleLocalizations.values.All(x => x.AsString() != lang))
						bundleLocalizations.AddString(lang);
				}

				// Save Info.plist
				plist.WriteToFile(infoPlistPath);
			}

			// Modify XCode project.
			string projPath = PBXProject.GetPBXProjectPath(path);
			PBXProject proj = new PBXProject();
			proj.ReadFromFile(projPath);
			string targetGuid = proj.TargetGuidByName(PBXProject.GetUnityTargetName());

			// Modify build properties.
			proj.SetBuildProperty(targetGuid, "ENABLE_BITCODE", current.uploadBitcode ? "YES" : "NO");
			if (!string.IsNullOrEmpty(current.developerTeamId))
				proj.SetBuildProperty(targetGuid, "DEVELOPMENT_TEAM", current.developerTeamId);
			
			if (!current.automaticallySign && !string.IsNullOrEmpty(current.profileId))
				proj.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE", current.profileId);
			if (!current.automaticallySign && !string.IsNullOrEmpty(current.profileSpecifier))
				proj.SetBuildProperty(targetGuid, "PROVISIONING_PROFILE_SPECIFIER", current.profileSpecifier);
			if (!current.automaticallySign && !string.IsNullOrEmpty(current.codeSignIdentity))
				proj.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY", current.codeSignIdentity);

			// Set entitlement file.
			if (!string.IsNullOrEmpty(current.entitlementsFile))
			{
				string filename = Path.GetFileName(current.entitlementsFile);
				if (!proj.ContainsFileByProjectPath(filename))
					proj.AddFile("../" + current.entitlementsFile, filename);
				proj.SetBuildProperty(targetGuid, "CODE_SIGN_ENTITLEMENTS", filename);
			}

			// Add frameworks.
			if (!string.IsNullOrEmpty(current.frameworks))
			{
				foreach (var fw in current.frameworks.Split(';'))
					proj.AddFrameworkToProject(targetGuid, fw, false);
			}

			// Activate services.
			if (!string.IsNullOrEmpty(current.services) && !string.IsNullOrEmpty(current.developerTeamId))
			{
				Regex reg = new Regex("(\\t*SystemCapabilities = {\\n)((.*{\\n.*\\n.*};\\n)+)");
				string replaceText = 
					string.Format("\nDevelopmentTeam = {0};\n$0{1}\n"
						, current.developerTeamId
						, current.services.Split(';').Select(x => x + " = {enabled = 1;};").Aggregate((a, b) => a + b)
					);
				proj.ReadFromString(reg.Replace(proj.WriteToString(), replaceText));
			}

			// Save XCode project.
			proj.WriteToFile(projPath);
#endif
		}
	}
}