using UnityEngine;
using UnityEditor;
using System.Collections;

/**
 * UViewSettings.cs
 * Author: Luke Holland (http://lukeholland.me/)
 */

namespace UView {

	public class UViewSettings : ScriptableObject
	{

		private const string kDefaultPrefabPath = "Assets/Resources/View";
		private const string kDefaultScriptPath = "Assets/Scripts/View";

		public string prefabsPath;
		public string scriptsPath;
		public TextAsset scriptTemplate;
		public Transform prefabTemplate;

		public void RestoreDefaults()
		{
			prefabsPath = kDefaultPrefabPath;
			scriptsPath = kDefaultScriptPath;
		}

	}

	[CustomEditor(typeof(UViewSettings))]
	public class UViewSettingsEditor : Editor
	{

		private UViewSettings _settings;
		private SerializedProperty _propertySettingsPrefabsPath;
		private SerializedProperty _propertySettingsScriptsPath;
		private SerializedProperty _propertySettingsScriptTemplate;
		private SerializedProperty _propertySettingsPrefabTemplate;

		protected void OnEnable()
		{
			_settings = target as UViewSettings;
			_propertySettingsPrefabsPath = serializedObject.FindProperty("prefabsPath");
			_propertySettingsScriptsPath = serializedObject.FindProperty("scriptsPath");
			_propertySettingsScriptTemplate = serializedObject.FindProperty("scriptTemplate");
			_propertySettingsPrefabTemplate = serializedObject.FindProperty("prefabTemplate");
		}

		public override void OnInspectorGUI ()
		{
			serializedObject.Update();

			_propertySettingsPrefabsPath.stringValue = UViewEditorUtils.LayoutPathSelector(_propertySettingsPrefabsPath.stringValue,"Default Prefabs Path");

			if(!UViewEditorUtils.ValidateResourcePath(_propertySettingsPrefabsPath.stringValue)){
				EditorGUILayout.HelpBox(string.Format("Prefabs should be stored in a '{0}' folder",UViewEditorUtils.kResources),MessageType.Error);
			}

			_propertySettingsScriptsPath.stringValue = UViewEditorUtils.LayoutPathSelector(_propertySettingsScriptsPath.stringValue,"Default Scripts Path");

			EditorGUILayout.PropertyField(_propertySettingsScriptTemplate);
			EditorGUILayout.PropertyField(_propertySettingsPrefabTemplate);

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();

			GUILayout.FlexibleSpace();

			if(GUILayout.Button("Reset to Defaults",GUILayout.Width(120))){
				_settings.RestoreDefaults();
			}

			EditorGUILayout.EndHorizontal();

			serializedObject.ApplyModifiedProperties();
		}

	}

}