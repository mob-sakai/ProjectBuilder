using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Mobcast.Coffee.Build
{
	/// <summary>
	/// プロジェクトビルダーエディタ.
	/// インスペクタをオーバーライドして、ビルド設定エディタを構成します.
	/// エディタから直接ビルドパイプラインを実行できます.
	/// </summary>
	internal class ProjectBuilderEditor : EditorWindow
	{
		Vector2 scrollPosition;
		ProjectBuilder[] targets;
		SerializedObject serializedObject;


		static GUIContent contentOpen;
		static GUIContent contentTitle = new GUIContent();
		static ReorderableList roSceneList;
		static ReorderableList roExcludeDirectoriesList;
		static ReorderableList roBuilderList;

		static GUIStyle styleCommand;
		static GUIStyle styleSymbols;
		static GUIStyle styleTitle;

		static string s_EndBasePropertyName = "";
		static string[] s_AvailableScenes;
		static List<ProjectBuilder> s_BuildersInProject;

		static readonly Dictionary<BuildTarget, IBuildTargetSettings> s_BuildTargetSettings =
			typeof(ProjectBuilder).Assembly
				.GetTypes()
				.Where(x => x.IsPublic && !x.IsInterface && typeof(IBuildTargetSettings).IsAssignableFrom(x))
				.Select(x => Activator.CreateInstance(x) as IBuildTargetSettings)
				.OrderBy(x => x.buildTarget)
				.ToDictionary(x => x.buildTarget);

		static readonly int[] s_BuildTargetValues = s_BuildTargetSettings.Keys.Cast<int>().ToArray();
		static readonly GUIContent[] s_BuildTargetLabels = s_BuildTargetSettings.Keys.Select(x => new GUIContent(x.ToString())).ToArray();
		
		public static Texture GetBuildTargetIcon(ProjectBuilder builder)
		{
			return builder.buildApplication && s_BuildTargetSettings.ContainsKey(builder.buildTarget)
				? s_BuildTargetSettings[builder.buildTarget].icon
					: EditorGUIUtility.FindTexture("BuildSettings.Editor.Small");
		}

		[MenuItem("Coffee/Project Builder")]
		public static void OnOpenFromMenu()
		{
			EditorWindow.GetWindow<ProjectBuilderEditor>("Project Builder");
		}

		void Initialize()
		{
			if (styleCommand != null)
				return;

			styleTitle = new GUIStyle("IN BigTitle");
			styleTitle.alignment = TextAnchor.UpperLeft;
			styleTitle.fontSize = 12;
			styleTitle.stretchWidth = true;
			styleTitle.margin = new RectOffset();

			styleSymbols = new GUIStyle(EditorStyles.textArea);
			styleSymbols.wordWrap = true;

			styleCommand = new GUIStyle(EditorStyles.textArea);
			styleCommand.stretchWidth = false;
			styleCommand.fontSize = 9;
			contentOpen = new GUIContent(EditorGUIUtility.FindTexture("project"));

			// Find end property in ProjectBuilder.
			var dummy = ScriptableObject.CreateInstance<ProjectBuilder>();
			var sp = new SerializedObject(dummy).GetIterator();
			sp.Next(true);
			while (sp.Next(false))
				s_EndBasePropertyName = sp.name;


			// Scene list.
			roSceneList = new ReorderableList(new List<ProjectBuilder.SceneSetting>(), typeof(ProjectBuilder.SceneSetting));
			roSceneList.drawElementCallback += (rect, index, isActive, isFocused) =>
			{
				var element = roSceneList.serializedProperty.GetArrayElementAtIndex(index);
				EditorGUI.PropertyField(new Rect(rect.x, rect.y, 16, rect.height - 2), element.FindPropertyRelative("enable"), GUIContent.none);
				EditorGUIEx.TextFieldWithTemplate(new Rect(rect.x + 16, rect.y, rect.width - 16, rect.height - 2), element.FindPropertyRelative("name"), GUIContent.none, s_AvailableScenes, false);
			};
			roSceneList.headerHeight = 0;
			roSceneList.elementHeight = 18;


			// Exclude Directories List
			roExcludeDirectoriesList = new ReorderableList(new List<string>(), typeof(string));
			roExcludeDirectoriesList.drawElementCallback += (rect, index, isActive, isFocused) =>
				{
					var element = roExcludeDirectoriesList.serializedProperty.GetArrayElementAtIndex(index);
					EditorGUIEx.DirectoryPathField(rect, element, GUIContent.none, "Selcet exclude directory in build.");
				};
			roExcludeDirectoriesList.headerHeight = 0;
			roExcludeDirectoriesList.elementHeight = 18;


			// Builder list.
			roBuilderList = new ReorderableList(s_BuildersInProject, typeof(ProjectBuilder));
			roBuilderList.onSelectCallback = (list) => Selection.activeObject = list.list[list.index] as ProjectBuilder;
			roBuilderList.onAddCallback += (list) =>
			{
				EditorApplication.delayCall += () =>
				{
					Util.CreateBuilderAsset();
					OnSelectionChanged();
				};
			};
			roBuilderList.onRemoveCallback += (list) =>
			{
				EditorApplication.delayCall += () =>
				{
					AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(list.list[list.index] as ProjectBuilder));
					AssetDatabase.Refresh();
					OnSelectionChanged();
				};
			};
			roBuilderList.drawElementCallback += (rect, index, isActive, isFocused) =>
			{
				var b = roBuilderList.list[index] as ProjectBuilder;    //オブジェクト取得.
				if (!b)
					return;

				GUI.DrawTexture(new Rect(rect.x, rect.y + 2, 16, 16), GetBuildTargetIcon(b));
				GUI.Label(new Rect(rect.x + 16, rect.y + 2, rect.width - 16, rect.height - 2), new GUIContent(string.Format("{0} ({1})", b.name, b.productName)));
			};
			roBuilderList.headerHeight = 0;
			roBuilderList.draggable = false;

			contentTitle = new GUIContent(Util.GetAssets<Texture2D>(typeof(ProjectBuilder).Name + " Icon").FirstOrDefault());
			DestroyImmediate(dummy);
		}
		//---- ▲ GUIキャッシュ ▲ ----


		//-------------------------------
		//	Unityコールバック.
		//-------------------------------
		/// <summary>
		/// Raises the enable event.
		/// </summary>
		void OnEnable()
		{
			SelectBuilder(Selection.objects.OfType<ProjectBuilder>().ToArray());
			Selection.selectionChanged += OnSelectionChanged;
			minSize = new Vector2(300, 300);
		}

		/// <summary>
		/// Raises the disable event.
		/// </summary>
		void OnDisable()
		{
			Selection.selectionChanged -= OnSelectionChanged;
		}

		void SelectBuilder(ProjectBuilder[] builders)
		{
			// Get all scenes in build from BuildSettings.
			s_AvailableScenes = EditorBuildSettings.scenes.Select(x => Path.GetFileName(x.path)).ToArray();
			
			// Get all builder assets in project.
			s_BuildersInProject = new List<ProjectBuilder>(
				Util.GetAssets<ProjectBuilder>()
				.OrderBy(b => b.buildApplication)
				.ThenBy(b => b.buildTarget)
			);

			targets = 0 < builders.Length
					? builders
					: s_BuildersInProject.Take(1).ToArray();
			
			serializedObject = null;

			contentTitle.text = 0 < targets.Length
				? targets.Select(x => "  " + x.name).Aggregate((a, b) => a + "\n" + b)
				: "";
		}

		void OnSelectionChanged()
		{
			var builders = Selection.objects.OfType<ProjectBuilder>().ToArray();
					
			if (0 < builders.Length || targets.Any(x => !x))
			{
				SelectBuilder(builders);
				Repaint();
			}
		}

		void OnGUI()
		{
			Initialize();

			if (targets == null || targets.Length == 0)
			{
				if (GUILayout.Button("Create New ProjectBuilder Asset"))
					Selection.activeObject = Util.CreateBuilderAsset();
				return;
			}


			using (var svs = new EditorGUILayout.ScrollViewScope(scrollPosition))
			{
				scrollPosition = svs.scrollPosition;
				
				serializedObject = serializedObject ?? new SerializedObject(targets);
				serializedObject.Update();

				GUILayout.Label(contentTitle, styleTitle);

				DrawCustomProjectBuilder();
				DrawApplicationBuildSettings();
				DrawAssetBundleBuildSettings();
				DrawBuildTragetSettings();
				DrawControlPanel();

				serializedObject.ApplyModifiedProperties();
			}
		}


		//-------------------------------
		//	メソッド.
		//-------------------------------
		/// <summary>
		/// カスタムプロジェクトビルダーで定義しているプロパティを全て描画します.
		/// Draw all propertyies declared in Custom-ProjectBuilder.
		/// </summary>
		void DrawCustomProjectBuilder()
		{
			System.Type type = serializedObject.targetObject.GetType();
			if (type == typeof(ProjectBuilder))
				return;

			GUI.backgroundColor = Color.green;
			using (new EditorGUIEx.GroupScope(type.Name))
			{
				GUI.backgroundColor = Color.white;

				GUILayout.Space(-20);
				Rect rButton = EditorGUILayout.GetControlRect();
				rButton.x += rButton.width - 50;
				rButton.width = 50;
				if (GUI.Button(rButton, "Edit", EditorStyles.miniButton))
				{
					InternalEditorUtility.OpenFileAtLineExternal(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(serializedObject.targetObject as ScriptableObject)), 1);
				}

				var itr = serializedObject.GetIterator();

				// Skip properties declared in ProjectBuilder.
				itr.NextVisible(true);
				while (itr.NextVisible(false) && itr.name != s_EndBasePropertyName)
					;

				// Draw properties declared in Custom-ProjectBuilder.
				while (itr.NextVisible(false))
					EditorGUILayout.PropertyField(itr, true);
			}
		}

		/// <summary>
		/// アプリケーションビルド設定を描画します.
		/// Draw application build settings.
		/// </summary>
		void DrawApplicationBuildSettings()
		{
			var spBuildApplication = serializedObject.FindProperty("buildApplication");
			var spBuildTarget = serializedObject.FindProperty("buildTarget");
			using (new EditorGUIEx.GroupScope("Application Build Setting"))
			{
				EditorGUILayout.PropertyField(spBuildApplication);
				if (spBuildApplication.boolValue)
				{
					// Basic Options
					EditorGUILayout.IntPopup(spBuildTarget, s_BuildTargetLabels, s_BuildTargetValues);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("companyName"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("productName"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("applicationIdentifier"));

					// Advanced Options
					GUILayout.Space(8);
					EditorGUILayout.LabelField("Advanced Options", EditorStyles.boldLabel);
					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField(serializedObject.FindProperty("developmentBuild"));

					EditorGUILayout.LabelField("Scripting Define Symbols");
					GUILayout.Space(-18);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("defineSymbols"), GUIContent.none);

					// Scenes In Build.
					EditorGUILayout.LabelField("Enable/Disable Scenes In Build");
					roSceneList.serializedProperty = serializedObject.FindProperty("scenes");

					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.Space(16);
						using (new EditorGUILayout.VerticalScope())
						{
							EditorGUI.indentLevel--;
							roSceneList.DoLayoutList();
							EditorGUI.indentLevel++;
						}
					}

					// Exclude Directories.
					EditorGUILayout.LabelField("Exclude Directories");
					roExcludeDirectoriesList.serializedProperty = serializedObject.FindProperty("excludeDirectories");

					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.Space(16);
						using (new EditorGUILayout.VerticalScope())
						{
							EditorGUI.indentLevel--;
							roExcludeDirectoriesList.DoLayoutList();
							EditorGUI.indentLevel++;
						}
					}
					EditorGUI.indentLevel--;

					// Version.
					EditorGUILayout.LabelField("Version Settings", EditorStyles.boldLabel);
					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField(serializedObject.FindProperty("version"));

					// Internal version for the build target.
					switch ((BuildTarget)spBuildTarget.intValue)
					{
						case BuildTarget.Android:
							EditorGUILayout.PropertyField(serializedObject.FindProperty("versionCode"), new GUIContent("Version Code"));
							break;
						case BuildTarget.iOS:
							EditorGUILayout.PropertyField(serializedObject.FindProperty("versionCode"), new GUIContent("Build Number"));
							break;
					}
					EditorGUI.indentLevel--;
				}
			}
		}

		/// <summary>
		/// Draw asset bundle build settings.
		/// </summary>
		void DrawAssetBundleBuildSettings()
		{
			// AssetBundle building.
			using (new EditorGUIEx.GroupScope("AssetBundle Build Setting"))
			{
				var spBuildAssetBundle = serializedObject.FindProperty("buildAssetBundle");
				EditorGUILayout.PropertyField(spBuildAssetBundle);
				if (spBuildAssetBundle.boolValue)
				{
					EditorGUILayout.PropertyField(serializedObject.FindProperty("bundleOptions"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("copyToStreamingAssets"));
				}
			}
		}

		/// <summary>
		/// ターゲットごとのビルド設定を描画します.
		/// Draw build target settings.
		/// </summary>
		void DrawBuildTragetSettings()
		{
			var spBuildApplication = serializedObject.FindProperty("buildApplication");
			var spBuildTarget = serializedObject.FindProperty("buildTarget");
			var buildTarget = (BuildTarget)spBuildTarget.intValue;
			if (spBuildApplication.boolValue && s_BuildTargetSettings.ContainsKey(buildTarget))
				s_BuildTargetSettings[buildTarget].DrawSetting(serializedObject);
		}

		/// <summary>
		/// アセットバンドルビルド設定を描画します.
		/// Control panel for builder.
		/// </summary>
		void DrawControlPanel()
		{
			var builder = serializedObject.targetObject as ProjectBuilder;

			GUILayout.FlexibleSpace();
			using (new EditorGUILayout.VerticalScope("box"))
			{
				if (builder.buildApplication)
				{
					GUILayout.Label(new GUIContent(string.Format("{0} ver.{1} ({2})", builder.productName, builder.version, builder.versionCode), GetBuildTargetIcon(builder)), EditorStyles.largeLabel);
				}
				else if (builder.buildAssetBundle)
				{
					GUILayout.Label(new GUIContent(string.Format("{0} AssetBundles", AssetDatabase.GetAllAssetBundleNames().Length), GetBuildTargetIcon(builder)), EditorStyles.largeLabel);
				}

				using (new EditorGUILayout.HorizontalScope())
				{
					// Apply settings from current builder asset.
					if (GUILayout.Button(new GUIContent("Apply Setting", EditorGUIUtility.FindTexture("vcs_check"))))
					{
						builder.DefineSymbol();
						builder.ApplySettings();
					}

					// Open PlayerSettings.
					if (GUILayout.Button(new GUIContent("Player Setting", EditorGUIUtility.FindTexture("EditorSettings Icon")), GUILayout.Height(21), GUILayout.Width(110)))
					{
						EditorApplication.ExecuteMenuItem("Edit/Project Settings/Player");
					}
				}

				//ビルドターゲットが同じ場合のみビルド可能.
				EditorGUI.BeginDisabledGroup(builder.actualBuildTarget != EditorUserBuildSettings.activeBuildTarget);

				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUI.BeginDisabledGroup(!builder.buildAssetBundle);
					// Build.
					if (GUILayout.Button(new GUIContent("Build AssetBundles", EditorGUIUtility.FindTexture("buildsettings.editor.small")), "LargeButton"))
					{
						EditorApplication.delayCall += () => Util.StartBuild(builder, false, true);
					}

					// Open output.
					var r = EditorGUILayout.GetControlRect(false, GUILayout.Width(15));
					if (GUI.Button(new Rect(r.x - 2, r.y + 5, 20, 20), contentOpen, EditorStyles.label))
					{
						Directory.CreateDirectory(builder.bundleOutputPath);
						Util.RevealOutputInFinder(builder.bundleOutputPath);
					}
					EditorGUI.EndDisabledGroup();
				}

				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUI.BeginDisabledGroup(!builder.buildApplication);
					// Build.
					if (GUILayout.Button(new GUIContent(string.Format("Build to '{0}'", builder.outputPath), EditorGUIUtility.FindTexture("preAudioPlayOff")), "LargeButton"))
					{
						EditorApplication.delayCall += () => Util.StartBuild(builder, false, false);
					}

					// Open output.
					var r = EditorGUILayout.GetControlRect(false, GUILayout.Width(15));
					if (GUI.Button(new Rect(r.x - 2, r.y + 5, 20, 20), contentOpen, EditorStyles.label))
						Util.RevealOutputInFinder(builder.outputFullPath);
					EditorGUI.EndDisabledGroup();
				}

				// Build & Run.
				if (GUILayout.Button(new GUIContent("Build & Run", EditorGUIUtility.FindTexture("preAudioPlayOn")), "LargeButton"))
				{
					EditorApplication.delayCall += () => Util.StartBuild(builder, true, false);
				}
				EditorGUI.EndDisabledGroup();


				// Create custom builder script.
				if (Util.builderType == typeof(ProjectBuilder) && GUILayout.Button("Create Custom Project Builder Script"))
				{
					Util.CreateCustomProjectBuilder();
				}

				// Available builders.
				GUILayout.Space(10);
				GUILayout.Label("Available Project Builders", EditorStyles.boldLabel);
				roBuilderList.list = s_BuildersInProject;
				roBuilderList.index = s_BuildersInProject.FindIndex(x => x == serializedObject.targetObject);
				roBuilderList.DoLayoutList();
			}
		}
	}
}