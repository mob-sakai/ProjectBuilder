using Mobcast.Coffee.Build;
using UnityEditor;
using UnityEngine;


namespace Mobcast.Coffee.Build
{
	[System.Serializable]
	public class PlatformSettings_Android : IPlatformSettings
	{
		public BuildTarget platform{get{ return BuildTarget.Android;}}
		public Texture icon{get{ return EditorGUIUtility.FindTexture("BuildSettings.Android.Small");}}

		/// <summary>Keystore file path.</summary>
		[Tooltip("Keystore file path.")]
		public string keystoreFile = "";

		/// <summary>Keystore password.</summary>
		[Tooltip("Keystore password.")]
		public string keystorePassword = "";

		/// <summary>Keystore alias name.</summary>
		[Tooltip("Keystore alias name.")]
		public string keystoreAliasName = "";

		/// <summary>Keystore alias password.</summary>
		[Tooltip("Keystore alias password.")]
		public string keystoreAliasPassword = "";

		public void Reset()
		{
			keystoreFile = PlayerSettings.Android.keystoreName.Replace("\\", "/").Replace(Util.projectDir + "/", "");
			keystorePassword = PlayerSettings.Android.keystorePass;
			keystoreAliasName = PlayerSettings.Android.keyaliasName;
			keystoreAliasPassword = PlayerSettings.Android.keyaliasPass;
		}

		public void ApplySettings(ProjectBuilder builder)
		{
			PlayerSettings.Android.bundleVersionCode = builder.versionCode;
			PlayerSettings.Android.keystoreName = keystoreFile;
			PlayerSettings.Android.keystorePass = keystorePassword;
			PlayerSettings.Android.keyaliasName = keystoreAliasName;
			PlayerSettings.Android.keyaliasPass = keystoreAliasPassword;
		}

		/// <summary>
		/// Draws the ios settings.
		/// </summary>
		public void DrawSetting(SerializedObject serializedObject)
		{
			var settings = serializedObject.GetProperty("androidSettings");

			using (new EditorGUIEx.GroupScope("Android Settings"))
			{
				EditorGUILayout.LabelField("Keystore", EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				{
					EditorGUIEx.FilePathField(settings.GetProperty("keystoreFile"), "Select keystore file.", "", "");
					EditorGUIEx.PropertyField(settings.GetProperty("keystorePassword"));
					EditorGUIEx.PropertyField(settings.GetProperty("keystoreAliasName"), EditorGUIEx.GetContent("Alias"));
					EditorGUIEx.PropertyField(settings.GetProperty("keystoreAliasPassword"), EditorGUIEx.GetContent("Alias Password"));
				}
				EditorGUI.indentLevel--;
			}
		}
	}
}