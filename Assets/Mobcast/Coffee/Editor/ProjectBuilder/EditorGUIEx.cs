using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Text;


namespace Mobcast.Coffee.Build
{
	internal static class EditorGUIEx
	{
		public static void FilePathField (SerializedProperty property, string title, string directory, string extension, params GUILayoutOption[] options)
		{
			FilePathField (property, new GUIContent (property.displayName), title, directory, extension, options);
		}

		public static void FilePathField (SerializedProperty property, GUIContent label, string title, string directory, string extension, params GUILayoutOption[] options)
		{
			var r = GUILayoutUtility.GetRect (label, EditorStyles.textField, options);
			label = EditorGUI.BeginProperty (r, label, property);

			// TextField (free edit).
			EditorGUI.BeginChangeCheck ();
			{
				r.width -= 14;
				string newValue = EditorGUI.TextField (r, label, property.stringValue);
				if (EditorGUI.EndChangeCheck ())
					property.stringValue = newValue;
			}

			// Select file button.
			var rButton = new Rect (r.x + r.width - 1, r.y, 20, 17);
			if (GUI.Button (rButton, EditorGUIUtility.FindTexture ("project"), EditorStyles.label)) {
				// If you select a file, convert to relative path.
				string path = EditorUtility.OpenFilePanel (title, directory, extension);
				if (!string.IsNullOrEmpty (path)) {
					property.stringValue = FileUtil.GetProjectRelativePath (path);
				}
			}
			EditorGUI.EndProperty ();
		}


		public static void DirectoryPathField (Rect position, SerializedProperty property, GUIContent label, string title, params GUILayoutOption[] options)
		{
			label = EditorGUI.BeginProperty (position, label, property);

			// TextField (free edit).
			EditorGUI.BeginChangeCheck ();
			{
				position.width -= 14;
				string newValue = EditorGUI.TextField (position, label, property.stringValue);
				if (EditorGUI.EndChangeCheck ())
					property.stringValue = newValue;
			}

			// Select file button.
			var rButton = new Rect (position.x + position.width - 1, position.y, 20, 17);
			if (GUI.Button (rButton, EditorGUIUtility.FindTexture ("project"), EditorStyles.label)) {
				string directory = 0 < property.stringValue.Length && Directory.Exists(property.stringValue) ? property.stringValue : "Assets/";
				string path = EditorUtility.OpenFolderPanel (title, directory, "");
				if (!string.IsNullOrEmpty (path)) {
					property.stringValue = FileUtil.GetProjectRelativePath (path);
				}
			}
			EditorGUI.EndProperty ();
		}

		public static void TextFieldWithTemplate (SerializedProperty property, string[] displayedOptions, bool maskable, params GUILayoutOption[] options)
		{
			TextFieldWithTemplate (property, new GUIContent (property.displayName), displayedOptions, maskable, options);
		}

		public static void TextFieldWithTemplate (SerializedProperty property, GUIContent label, string[] displayedOptions, bool maskable, params GUILayoutOption[] options)
		{
			TextFieldWithTemplate (GUILayoutUtility.GetRect (label, EditorStyles.textField, options), property, new GUIContent (property.displayName), displayedOptions, maskable, options);
		}

		public static void TextFieldWithTemplate (Rect r, SerializedProperty property, GUIContent label, string[] displayedOptions, bool maskable, params GUILayoutOption[] options)
		{
			var content = EditorGUI.BeginProperty (r, label, property);
			if (maskable)
				content.text += " (;)";

			// TextField (free edit).
			EditorGUI.BeginChangeCheck ();
			{
				r.width -= 14;
				string newValue = EditorGUI.TextField (r, content, property.stringValue);
				if (EditorGUI.EndChangeCheck ())
					property.stringValue = newValue;
			}

			// Template menu button.
			var rButton = new Rect (r.x + r.width + 2, r.y + 5, 14, 10);
			if (GUI.Button (rButton, EditorGUIUtility.FindTexture ("icon dropdown"), EditorStyles.label)) {
				// Create menu and add all items. When you clicked item, enable/disable itself.
				var menu = new GenericMenu ();
				foreach (var op in displayedOptions) {
					string item = op;
					bool active = maskable ? property.stringValue.Contains (item) : property.stringValue == item;
					menu.AddItem (new GUIContent (item), active, 
						() => {
							if (maskable) {
								property.stringValue = active ? property.stringValue.Replace (item, "") : property.stringValue + ";" + item;
								property.stringValue = property.stringValue.Replace (";;", ";").Trim (';');
							} else {
								property.stringValue = item;
							}
							property.serializedObject.ApplyModifiedProperties ();
						});
				}

				// Show template menu.
				GUIUtility.keyboardControl = 0;
				menu.DropDown (new Rect (r.x + EditorGUIUtility.labelWidth, r.y, r.width - EditorGUIUtility.labelWidth + 14, r.height));
			}
			EditorGUI.EndProperty ();
		}

		/// <summary>折りたたみスコープ(インスペクタ専用).</summary>
		internal class GroupScope : IDisposable
		{
			//---- ▼ GUIキャッシュ ▼ ----
			static GUIStyle styleHeader;
			static GUIStyle styleInner;

			static void CacheGUI ()
			{
				if (styleHeader != null)
					return;

				styleHeader = new GUIStyle ("RL Header");
				styleHeader.alignment = TextAnchor.MiddleLeft;
				styleHeader.richText = true;
				styleHeader.fontSize = 11;
				styleHeader.fontStyle = FontStyle.Bold;
				styleHeader.stretchWidth = true;
				styleHeader.margin = new RectOffset (4, 0, 2, 0);
				styleHeader.padding = new RectOffset (6, 4, 0, 0);
				styleHeader.stretchWidth = true;
				styleHeader.stretchHeight = false;
				styleHeader.normal.textColor = EditorStyles.label.normal.textColor;

				styleInner = new GUIStyle ("RL Background");
				styleInner.border = new RectOffset (10, 10, 1, 8);
				styleInner.margin = new RectOffset (4, 0, 0, 2);
				styleInner.padding = new RectOffset (4, 4, 3, 6);
				styleInner.clipping = TextClipping.Clip;
			}
			//---- ▲ GUIキャッシュ ▲ ----

			/// <summary>折りたたみスコープを設定.</summary>
			void SetScope (GUIContent content, params GUILayoutOption[] option)
			{
				CacheGUI ();

				//ヘッダー.
				Rect r = GUILayoutUtility.GetRect (18, 18, styleHeader);
				GUI.Label (r, content, styleHeader);

				//インナー表示.
				Color backgroundColor = GUI.backgroundColor;
				GUI.backgroundColor = Color.white;
				EditorGUILayout.BeginVertical (styleInner, option);
				GUI.backgroundColor = backgroundColor;
			}

			/// <summary>折りたたみスコープを開始します(usingの使用を推奨).</summary>
			public GroupScope (string text, params GUILayoutOption[] option)
			{
				SetScope (new GUIContent (text), option);
			}

			/// <summary>
			/// Releases all resource used by the <see cref="UnityEditorTools.GroupScope"/> object.
			/// </summary>
			public void Dispose ()
			{
				EditorGUILayout.EndVertical ();
			}
		}
	}
}
