using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Mobcast.Coffee.Build;
using System;
using System.Reflection;
using UnityEditor.Callbacks;
using System.Text;


namespace Mobcast.Coffee.Build
{
	internal static class EditorGUIEx
	{
		/// <summary>コンテンツディクショナリ.</summary>
		static Dictionary<string, GUIContent> s_ContentDictionary = new Dictionary<string, GUIContent>();

		/// <summary>プロパティディクショナリ.</summary>
		static Dictionary<object, Dictionary<string, SerializedProperty>> s_PropertyDictionaries = new Dictionary<object, Dictionary<string, SerializedProperty>>();

		/// <summary>プロパティディクショナリのリフレッシュカウント.</summary>
		static int s_RefleshCount = 0;

		/// <summary>
		/// 相対パスからSerializedPropertyを取得します.
		/// このメソッドで取得したSerializedPropertyはキャッシュされます.
		/// SerializedProperty.FindPropertyRelativeは都度インスタンス作成されるため、GCコストがかかりますが、
		/// こちらのメソッドでは高速かつ省メモリでSerializedPropertyを取得できます.
		/// </summary>
		/// <param name="property">SerializedProperty.</param>
		/// <param name="relativePath">相対パス.</param>
		public static SerializedProperty GetProperty(this object self, string path)
		{
			//プロパティディクショナリのリフレッシュカウントをすすめます.
			//1000000アクセス毎に、プロパティディクショナリをリセットします.
			if (1000000 < ++s_RefleshCount)
			{
				s_RefleshCount = 0;
				s_ContentDictionary = new Dictionary<string, GUIContent>();
				s_PropertyDictionaries = new Dictionary<object, Dictionary<string, SerializedProperty>>();
			}

			//プロパティディクショナリを取得または生成します.
			Dictionary<string, SerializedProperty> map;
			if (!s_PropertyDictionaries.TryGetValue(self, out map) || map == null)
			{
				map = new Dictionary<string, SerializedProperty>();
				s_PropertyDictionaries[self] = map;
			}

			SerializedProperty property;
			if (!map.TryGetValue(path, out property) || property == null)
			{
				property = self is SerializedObject ? (self as SerializedObject).FindProperty(path)
					: self is SerializedProperty ? (self as SerializedProperty).FindPropertyRelative(path)
					: null;
				map[path] = property;
			}
			return property;
		}

		/// <summary>
		/// オブジェクトからGUIContentを取得します.
		/// このメソッドで取得したGUIContentはキャッシュされます.
		/// </summary>
		/// <param name="label">コンテンツラベル.</param>
		public static GUIContent GetContent(string label)
		{
			return GetContent(label, null);
		}

		/// <summary>
		/// オブジェクトからGUIContentを取得します.
		/// このメソッドで取得したGUIContentはキャッシュされます.
		/// </summary>
		/// <param name="label">コンテンツラベル.</param>
		public static GUIContent GetContent(string label, Texture icon, string tooltip = "")
		{
			GUIContent c;
			if (!s_ContentDictionary.TryGetValue(label, out c) || c == null)
			{
				c = new GUIContent(label, icon, tooltip);
				s_ContentDictionary[label] = c;
			}
			return c;
		}

		public static void PropertyField(SerializedProperty property, params GUILayoutOption[] options)
		{
			EditorGUILayout.PropertyField(property, EditorGUIEx.GetContent(property.displayName), options);
		}

		public static void PropertyField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
		{
			EditorGUILayout.PropertyField(property, label, options);
		}

		static StringBuilder s_StringBuilder = new StringBuilder();

		public static void PopupField(SerializedProperty property, Dictionary<int,string> displayedOptions, params GUILayoutOption[] options)
		{
			PopupField(property, EditorGUIEx.GetContent(property.displayName), displayedOptions, options);
		}

		public static void PopupField(SerializedProperty property, GUIContent label, Dictionary<int,string> displayedOptions, params GUILayoutOption[] options)
		{
			var popupLabel = displayedOptions.ContainsKey(property.intValue) ? EditorGUIEx.GetContent(displayedOptions[property.intValue]) : GUIContent.none;
			_PopupField(property, label, displayedOptions, popupLabel, false, options);
		}

		public static void MaskField(SerializedProperty property, Dictionary<int,string> displayedOptions, params GUILayoutOption[] options)
		{
			MaskField(property, EditorGUIEx.GetContent(property.displayName), displayedOptions, options);
		}

		public static void MaskField(SerializedProperty property, GUIContent label, Dictionary<int,string> displayedOptions, params GUILayoutOption[] options)
		{
			s_StringBuilder.Length = 0;
			displayedOptions
				.Where(op => (op.Key == property.intValue) || 0 != (op.Key & property.intValue))
				.Select(op => op.Value)
				.Aggregate(s_StringBuilder, (a, b) => a.AppendFormat("{0}, ", b));

			if (2 < s_StringBuilder.Length)
				s_StringBuilder.Length -= 2;

			_PopupField(property, label, displayedOptions, EditorGUIEx.GetContent(s_StringBuilder.ToString()), true, options);
		}


		public static void _PopupField(SerializedProperty property, GUIContent label, Dictionary<int,string> displayedOptions, GUIContent popupLabel, bool maskable, params GUILayoutOption[] options)
		{
			var r = GUILayoutUtility.GetRect(label, EditorStyles.popup, options);
			label = EditorGUI.BeginProperty(r, label, property);

			// Prefix label.
			r = EditorGUI.PrefixLabel(r, label);

			// Popup button.
			if (GUI.Button(r, popupLabel, EditorStyles.popup))
			{
				// Create menu and add all items. When you clicked item, apply itself.
				var menu = new GenericMenu();
				int current = property.intValue;

				foreach (var op in displayedOptions)
				{
					var item = op;

					bool active = maskable ? (item.Key == current || 0 != (current & item.Key)) : (current == item.Key);
					menu.AddItem(EditorGUIEx.GetContent(item.Value), active, 
						() =>
						{
							if(!active && maskable && item.Key == 0)
								property.intValue = 0;
							else if(maskable)
								property.intValue = active ? (current & ~item.Key) : (current | item.Key);
							else
								property.intValue = item.Key;
							property.serializedObject.ApplyModifiedProperties();
						});
				}
				menu.DropDown(r);
			}
			EditorGUI.EndProperty();
		}


		public static void PopupField(SerializedProperty property, string[] displayedOptions, params GUILayoutOption[] options)
		{
			PopupField(property, EditorGUIEx.GetContent(property.displayName), displayedOptions, options);
		}

		public static void PopupField(SerializedProperty property, GUIContent label, string[] displayedOptions, params GUILayoutOption[] options)
		{
			var r = GUILayoutUtility.GetRect(label, EditorStyles.popup, options);
			label = EditorGUI.BeginProperty(r, label, property);

			// Prefix label.
			r = EditorGUI.PrefixLabel(r, label);

			// Popup button.
			if (GUI.Button(r, EditorGUIEx.GetContent(property.stringValue), EditorStyles.popup))
			{
				// Create menu and add all items. When you clicked item, apply itself.
				var menu = new GenericMenu();
				foreach (var op in displayedOptions)
				{
					string item = op;
					menu.AddItem(EditorGUIEx.GetContent(item), property.stringValue == item, 
						() =>
						{
							property.stringValue = item;
							property.serializedObject.ApplyModifiedProperties();
						});
				}
				menu.DropDown(r);
			}
			EditorGUI.EndProperty();
		}

		public static void FilePathField(SerializedProperty property, string title, string directory, string extension, params GUILayoutOption[] options)
		{
			FilePathField(property, EditorGUIEx.GetContent(property.displayName), title, directory, extension, options);
		}

		public static void FilePathField(SerializedProperty property, GUIContent label, string title, string directory, string extension, params GUILayoutOption[] options)
		{
			var r = GUILayoutUtility.GetRect(label, EditorStyles.textField, options);
			label = EditorGUI.BeginProperty(r, label, property);

			// TextField (free edit).
			EditorGUI.BeginChangeCheck();
			{
				r.width -= 14;
				string newValue = EditorGUI.TextField(r, label, property.stringValue);
				if (EditorGUI.EndChangeCheck())
					property.stringValue = newValue;
			}

			// Select file button.
			var rButton = new Rect(r.x + r.width - 1, r.y, 20, 17);
			if (GUI.Button(rButton, EditorGUIUtility.FindTexture("project"), EditorStyles.label))
			{
				// If you select a file, convert to relative path.
				string path = EditorUtility.OpenFilePanel(title, directory, extension);
				if (!string.IsNullOrEmpty(path))
				{
					property.stringValue = FileUtil.GetProjectRelativePath(path);
				}
			}
			EditorGUI.EndProperty();
		}

		public static void TextFieldWithTemplate(SerializedProperty property, string[] displayedOptions, bool maskable, params GUILayoutOption[] options)
		{
			TextFieldWithTemplate(property, EditorGUIEx.GetContent(property.displayName), displayedOptions, maskable, options);
		}

		public static void TextFieldWithTemplate(SerializedProperty property, GUIContent label, string[] displayedOptions, bool maskable, params GUILayoutOption[] options)
		{
			TextFieldWithTemplate(GUILayoutUtility.GetRect(label, EditorStyles.textField, options), property, EditorGUIEx.GetContent(property.displayName), displayedOptions, maskable, options);
		}

		public static void TextFieldWithTemplate(Rect r, SerializedProperty property, GUIContent label, string[] displayedOptions, bool maskable, params GUILayoutOption[] options)
		{
			var content = EditorGUI.BeginProperty(r, label, property);
			if (maskable)
				content.text += " (;)";

			// TextField (free edit).
			EditorGUI.BeginChangeCheck();
			{
				r.width -= 14;
				string newValue = EditorGUI.TextField(r, content, property.stringValue);
				if (EditorGUI.EndChangeCheck())
					property.stringValue = newValue;
			}

			// Template menu button.
			var rButton = new Rect(r.x + r.width + 2, r.y + 5, 14, 10);
			if (GUI.Button(rButton, EditorGUIUtility.FindTexture("icon dropdown"), EditorStyles.label))
			{
				// Create menu and add all items. When you clicked item, enable/disable itself.
				var menu = new GenericMenu();
				foreach (var op in displayedOptions)
				{
					string item = op;
					bool active = maskable ? property.stringValue.Contains(item) : property.stringValue == item;
					menu.AddItem(EditorGUIEx.GetContent(item), active, 
						() =>
						{
							if (maskable)
							{
								property.stringValue = active ? property.stringValue.Replace(item, "") : property.stringValue + ";" + item;
								property.stringValue = property.stringValue.Replace(";;", ";").Trim(';');
							}
							else
							{
								property.stringValue = item;
							}
							property.serializedObject.ApplyModifiedProperties();
						});
				}

				// Show template menu.
				GUIUtility.keyboardControl = 0;
				menu.DropDown(new Rect(r.x + EditorGUIUtility.labelWidth, r.y, r.width - EditorGUIUtility.labelWidth + 14, r.height));
			}
			EditorGUI.EndProperty();
		}


		/// <summary>折りたたみスコープ(インスペクタ専用).</summary>
		internal class GroupScope : IDisposable
		{
			//---- ▼ GUIキャッシュ ▼ ----
			static GUIStyle styleHeader;
			static GUIStyle styleInner;

			static void CacheGUI()
			{
				if (styleHeader != null)
					return;

				styleHeader = new GUIStyle("RL Header");
				styleHeader.alignment = TextAnchor.MiddleLeft;
				styleHeader.richText = true;
				styleHeader.fontSize = 11;
				styleHeader.stretchWidth = true;
				styleHeader.margin = new RectOffset(0, 0, 0, 0);
				styleHeader.padding = new RectOffset(8, 8, 0, 0);
				styleHeader.stretchWidth = true;
				styleHeader.stretchHeight = false;
				styleHeader.normal.textColor = EditorStyles.label.normal.textColor;

				styleInner = new GUIStyle("RL Background");
				styleInner.border = new RectOffset(10, 10, 1, 8);
				styleInner.margin = new RectOffset(0, 0, 0, 4);
				styleInner.padding = new RectOffset(4, 4, 3, 6);
				styleInner.clipping = TextClipping.Clip;
			}
			//---- ▲ GUIキャッシュ ▲ ----

			/// <summary>折りたたみスコープを設定.</summary>
			void SetScope(GUIContent content, params GUILayoutOption[] option)
			{
				CacheGUI();

				//ヘッダー.
				Rect r = GUILayoutUtility.GetRect(18, 18, styleHeader);
				GUI.Label(r, content, styleHeader);

				//インナー表示.
				Color backgroundColor = GUI.backgroundColor;
				GUI.backgroundColor = Color.white;
				EditorGUILayout.BeginVertical(styleInner, option);
				GUI.backgroundColor = backgroundColor;
			}

			/// <summary>折りたたみスコープを開始します(usingの使用を推奨).</summary>
			public GroupScope(string text, params GUILayoutOption[] option)
			{
				SetScope(EditorGUIEx.GetContent(text), option);
			}

			/// <summary>
			/// Releases all resource used by the <see cref="UnityEditorTools.GroupScope"/> object.
			/// </summary>
			public void Dispose()
			{
				EditorGUILayout.EndVertical();
			}
		}
	}
}
