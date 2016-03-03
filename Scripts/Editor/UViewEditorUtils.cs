﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections;
using System.IO;

namespace UView {

	public static class UViewEditorUtils 
	{

		public const string KEY_SCRIPT_PATH = "keyViewControllerScriptPath";
		public const string KEY_PREFAB_PATH = "keyViewControllerPrefabPath";

		public static string[] kTabs = new string[]{"View Controller","Settings"};

		public const string kSettingsPath = "Assets/";
		public const string kSettingsAssetName = "-UViewSettings.asset";
		public const string kResources = "Resources/";

		[MenuItem("GameObject/Create Other/UView/ViewController")]
		public static void ContextCreateViewController()
		{
			MenuCreateViewController();
		}

		[MenuItem("Tools/UView/Create ViewController")]
		public static void MenuCreateViewController()
		{
			GameObject viewController = new GameObject("ViewController");
			viewController.AddComponent<ViewController>();
		}

		[MenuItem("Assets/Create/UView/View")]
		public static void ContextCreateView()
		{
			MenuCreateView();
		}

		[MenuItem("Tools/UView/Create View")]
		public static void MenuCreateView()
		{
			CreateViewWindow window = ScriptableObject.CreateInstance<CreateViewWindow>();
			window.titleContent = new GUIContent("Create View");
			window.minSize = new Vector2(400,250);
			window.ShowUtility();
		}

		public static UViewSettings GetSettings()
		{
			string settingsPath = string.Concat(UViewEditorUtils.kSettingsPath,UViewEditorUtils.kSettingsAssetName);
			UViewSettings settings = AssetDatabase.LoadAssetAtPath<UViewSettings>(settingsPath);

			if(settings==null){
				settings = ScriptableObject.CreateInstance<UViewSettings>();
				settings.RestoreDefaults();

				if(!Directory.Exists(UViewEditorUtils.kSettingsPath)) Directory.CreateDirectory(UViewEditorUtils.kSettingsPath);

				AssetDatabase.CreateAsset(settings,settingsPath);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			return settings;
		}

		public static  string GetResourcePath(string assetPath)
		{
			string result = assetPath.Substring(assetPath.IndexOf(kResources)+kResources.Length);
			result = result.Substring(0,result.LastIndexOf("."));

			return result;
		}

		public static void LayoutLabelWithPrefix(string prefix, object obj)
		{
			string label = obj==null ? string.Empty : obj.ToString();
			if(string.IsNullOrEmpty(label)) label = "(none)";

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(prefix);
			EditorGUILayout.SelectableLabel(label,GUILayout.Height(EditorGUIUtility.singleLineHeight));
			EditorGUILayout.EndHorizontal();
		}

		public static string LayoutPathSelector(string currentPath, string label)
		{
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.PrefixLabel(label);
			EditorGUILayout.SelectableLabel(currentPath,GUILayout.Height(EditorGUIUtility.singleLineHeight));
			if(GUILayout.Button("...",EditorStyles.miniButton)){
				string path = EditorUtility.OpenFolderPanel(label,currentPath,string.Empty);
				if(!string.IsNullOrEmpty(path)){
					currentPath = path.Replace(Application.dataPath.Substring(0,Application.dataPath.Length-6),string.Empty);
				}
			}

			EditorGUILayout.EndHorizontal();

			return currentPath;
		}

		public static AbstractView FindLoadedView(System.Type type)
		{
			Object[] gameObjects = Resources.FindObjectsOfTypeAll(type);
			int i = 0, l = gameObjects.Length;
			for(; i<l; ++i){

				AbstractView view = gameObjects[i] as AbstractView;
				if(!EditorUtility.IsPersistent(view)){
					return view;
				}
			}

			return null;
		}

		public static string GetViewName(SerializedProperty property)
		{
			try {
				return System.Type.GetType(property.stringValue).Name;
			} catch {
				return string.Format("{0} (Not Found)",property.stringValue.Substring(0,property.stringValue.IndexOf(',')));
			}
		}

		public static string[] GetViewNames(SerializedProperty propertyViewAssets, bool shortNames)
		{
			int i = 0, l = propertyViewAssets.arraySize;
			string[] names = new string[l];

			for(; i<l; ++i){
				SerializedProperty propertyViewAsset = propertyViewAssets.GetArrayElementAtIndex(i);
				SerializedProperty propertyViewTypeID = propertyViewAsset.FindPropertyRelative("viewTypeID");
				if(shortNames){
					names[i] = GetViewName(propertyViewTypeID);
				} else {
					names[i] = propertyViewTypeID.stringValue;
				}
			}

			return names;
		}

		public static void CreateViewAsset(SerializedProperty property, AbstractView view)
		{
			string assetPath = AssetDatabase.GetAssetPath(view);

			SerializedProperty propertyViewTypeID = property.FindPropertyRelative("viewTypeID");
			SerializedProperty propertyResourcePath = property.FindPropertyRelative("resourcePath");
			SerializedProperty propertyAssetID = property.FindPropertyRelative("assetID");
			propertyViewTypeID.stringValue = view.GetType().AssemblyQualifiedName;
			propertyResourcePath.stringValue = UViewEditorUtils.GetResourcePath(assetPath);
			propertyAssetID.stringValue = AssetDatabase.AssetPathToGUID(assetPath);
		}

		public static void RemoveViewAssets(SerializedProperty property)
		{
			SerializedProperty propertyAssetID = property.FindPropertyRelative("assetID");
			string assetPath = AssetDatabase.GUIDToAssetPath(propertyAssetID.stringValue);

			AbstractView view = AssetDatabase.LoadAssetAtPath<AbstractView>(assetPath);
			if(view!=null){
				MonoScript script = MonoScript.FromMonoBehaviour(view);
				string scriptPath = AssetDatabase.GetAssetPath(script);

				AssetDatabase.DeleteAsset(scriptPath);
			}

			AssetDatabase.DeleteAsset(assetPath);
			AssetDatabase.Refresh();
		}

		public static bool ValidateResourcePath(string path)
		{
			return path.Contains(string.Concat(Path.DirectorySeparatorChar,kResources));
		}

		public static void Rebuild(SerializedProperty propertyViewAssets)
		{
			int i = 0, l = propertyViewAssets.arraySize;
			for(; i<l; ++i){
				SerializedProperty propertyViewAsset = propertyViewAssets.GetArrayElementAtIndex(i);
				SerializedProperty propertyAssetID = propertyViewAsset.FindPropertyRelative("assetID");

				string assetPath = AssetDatabase.GUIDToAssetPath(propertyAssetID.stringValue);
				AbstractView view = AssetDatabase.LoadAssetAtPath<AbstractView>(assetPath);
				if(view!=null){
					CreateViewAsset(propertyViewAsset,view);
				}
			}
		}

	}

}