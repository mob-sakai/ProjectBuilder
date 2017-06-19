using Mobcast.Coffee.Build;
using UnityEditor;
using UnityEngine;


namespace Mobcast.Coffee.Build
{
	public interface IPlatformSettings
	{
		BuildTarget platform { get;}
		Texture icon { get;}

		void Reset();

		void ApplySettings(ProjectBuilder builder);

		void DrawSetting(SerializedObject serializedObject);
	}
}