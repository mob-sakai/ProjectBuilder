using Mobcast.Coffee.Build;
using UnityEditor;
using UnityEngine;


namespace Mobcast.Coffee.Build
{
	/// <summary>
	/// Build target settings interface.
	/// </summary>
	public interface IBuildTargetSettings
	{
		/// <summary>
		/// Build target.
		/// </summary>
		BuildTarget buildTarget { get;}

		/// <summary>
		/// Icon for build target.
		/// </summary>
		Texture icon { get;}

		/// <summary>
		/// </summary>
		void Reset();

		/// <summary>
		/// On Applies the settings.
		/// </summary>
		void ApplySettings(ProjectBuilder builder);

		/// <summary>
		/// Draws the setting.
		/// </summary>
		void DrawSetting(SerializedObject serializedObject);
	}
}