using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.IO;

/**
 * ViewControllerEditor.cs
 * Author: Luke Holland (http://lukeholland.me/)
 */

namespace UView {

	// TODO : Code comments and author tags
	// TODO : Support multiple view controllers in scene

	[CustomEditor(typeof(ViewController))]
	public class ViewControllerEditor : Editor 
	{

		private SerializedProperty _propertyAutoSetup;
		private SerializedProperty _propertyDontDestroyOnLoad;
		private SerializedProperty _propertyDebug;
		private SerializedProperty _propertyViewParent;
		private SerializedProperty _propertyStartingLocation;
		private SerializedProperty _propertyViewAssets;

		private SerializedProperty _propertySettingsPrefabsPath;
		private SerializedProperty _propertySettingsScriptsPath;
		private SerializedProperty _propertySettingsScriptTemplate;
		private SerializedProperty _propertySettingsPrefabTemplate;

		private UViewSettings _settings;
		private SerializedObject _settingsObject;

		private ViewList _viewList;
		private int _tabIndex;

		protected void OnEnable()
		{
			_propertyAutoSetup = serializedObject.FindProperty("_autoSetup");
			_propertyDontDestroyOnLoad = serializedObject.FindProperty("_dontDestroyOnLoad");
			_propertyDebug = serializedObject.FindProperty("_debug");
			_propertyViewParent = serializedObject.FindProperty("viewParent");
			_propertyStartingLocation = serializedObject.FindProperty("_startingLocation");
			_propertyViewAssets = serializedObject.FindProperty("_viewAssets");

			_tabIndex = 0;
			_viewList = new ViewList(serializedObject,_propertyViewAssets);

			_settings = UViewEditorUtils.GetSettings();
			_settingsObject = new SerializedObject(_settings);
			_propertySettingsPrefabsPath = _settingsObject.FindProperty("prefabsPath");
			_propertySettingsScriptsPath = _settingsObject.FindProperty("scriptsPath");
			_propertySettingsScriptTemplate = _settingsObject.FindProperty("scriptTemplate");
			_propertySettingsPrefabTemplate = _settingsObject.FindProperty("prefabTemplate");
		}

		public override void OnInspectorGUI ()
		{
			serializedObject.Update();

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			_tabIndex = GUILayout.Toolbar(_tabIndex,UViewEditorUtils.kTabs, GUILayout.ExpandWidth(true), GUILayout.Height(30)  );

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if(_tabIndex==0){

				if(Application.isPlaying){
					DrawStatsGUI();

					EditorGUILayout.Space();
					EditorGUILayout.Space();
				}

				EditorGUI.BeginDisabledGroup(Application.isPlaying);
				DrawViewGUI();
				EditorGUI.EndDisabledGroup();
			} else {
				DrawSettingsGUI();
			}

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawStatsGUI()
		{
			ViewController viewController = target as ViewController;

			UViewEditorUtils.LayoutLabelWithPrefix("Loaded Resources",viewController.loadedResourceCount);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Locations",EditorStyles.boldLabel);

			UViewEditorUtils.LayoutLabelWithPrefix("Current Location",viewController.currentLocation);
			UViewEditorUtils.LayoutLabelWithPrefix("Last Location",viewController.lastLocation);
			UViewEditorUtils.LayoutLabelWithPrefix("Target Location",viewController.targetLocation);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Overlays",EditorStyles.boldLabel);

			int l = viewController.showingOverlays.Length;
			UViewEditorUtils.LayoutLabelWithPrefix("Overlays Showing",l.ToString());
			UViewEditorUtils.LayoutLabelWithPrefix("Target Overlay",viewController.targetOverlay);
		}

		private void DrawViewGUI()
		{
			EditorGUILayout.LabelField("Configuration",EditorStyles.boldLabel);

			EditorGUILayout.PropertyField(_propertyAutoSetup);
			EditorGUILayout.PropertyField(_propertyDontDestroyOnLoad);
			EditorGUILayout.PropertyField(_propertyDebug);
			EditorGUILayout.PropertyField(_propertyViewParent);

			string[] viewNames = UViewEditorUtils.GetViewNames(_propertyViewAssets,false);
			string[] viewNamesShort = UViewEditorUtils.GetViewNames(_propertyViewAssets,true);

			int startLocationIndex = System.Array.IndexOf<string>(viewNames,_propertyStartingLocation.stringValue);
			startLocationIndex = EditorGUILayout.Popup("Start Location",startLocationIndex,viewNamesShort);
			_propertyStartingLocation.stringValue = startLocationIndex==-1 ? "" : viewNames[startLocationIndex];

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			bool locked = EditorApplication.isCompiling && EditorPrefs.HasKey(UViewEditorUtils.KEY_SCRIPT_PATH);
			EditorGUI.BeginDisabledGroup(locked);

			_viewList.DoLayoutList();

			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Rebuild",GUILayout.Width(80))){
				UViewEditorUtils.Rebuild(_propertyViewAssets);
			}
			EditorGUILayout.EndHorizontal();

			if(locked){
				EditorGUILayout.Space();
				EditorGUILayout.Space();

				EditorGUILayout.HelpBox("Creating View...",MessageType.Info);
			}
		}

		private void DrawSettingsGUI()
		{
			_settingsObject.Update();

			_propertySettingsPrefabsPath.stringValue = UViewEditorUtils.LayoutPathSelector(_propertySettingsPrefabsPath.stringValue,"Default Prefabs Path");

			if(!UViewEditorUtils.ValidateResourcePath(_propertySettingsPrefabsPath.stringValue)){
				EditorGUILayout.HelpBox(string.Format("Prefabs should be stored in a '{0}' folder",UViewEditorUtils.kResources),MessageType.Error);
			}

			_propertySettingsScriptsPath.stringValue = UViewEditorUtils.LayoutPathSelector(_propertySettingsScriptsPath.stringValue,"Default Scripts Path");

			EditorGUILayout.PropertyField(_propertySettingsScriptTemplate);
			EditorGUILayout.PropertyField(_propertySettingsPrefabTemplate);

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if(GUILayout.Button("Reset to Defaults")){
				_settings.RestoreDefaults();
			}

			_settingsObject.ApplyModifiedProperties();
		}

	}

}