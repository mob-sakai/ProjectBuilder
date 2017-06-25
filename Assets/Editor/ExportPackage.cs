using UnityEditor;

namespace Mobcast.Coffee.Build
{
	public static class ExportPackage
	{
		const string kPackageName = "ProjectBuilder.unitypackage";
		static readonly string[] kAssetPathes = {
			"Assets/Mobcast/Coffee/Editor/ProjectBuilder",
		};

		[MenuItem ("Coffee/Export Package/" + kPackageName)]
		[InitializeOnLoadMethod]
		static void Export ()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;
			
			AssetDatabase.ExportPackage (kAssetPathes, kPackageName, ExportPackageOptions.Recurse | ExportPackageOptions.Default);
			UnityEngine.Debug.Log ("Export successfully : " + kPackageName);
		}
	}
}