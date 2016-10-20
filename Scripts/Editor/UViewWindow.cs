using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections;

namespace UView {

	public class UViewWindow : EditorWindow
	{

		private int _tabIndex;
		private Vector2 _scroll;
		private ViewController _viewController;
		private Editor _viewControllerEditor;

		private UViewSettings _settings;
		private SerializedObject _settingsObject;

		private SerializedProperty _propertySettingsPrefabsPath;
		private SerializedProperty _propertySettingsScriptsPath;
		private SerializedProperty _propertySettingsScriptTemplate;
		private SerializedProperty _propertySettingsPrefabTemplate;

		protected void OnEnable()
		{
			_tabIndex = 0;

			_settings = UViewEditorUtils.GetSettings();
			_settingsObject = new SerializedObject(_settings);
			_propertySettingsPrefabsPath = _settingsObject.FindProperty("prefabsPath");
			_propertySettingsScriptsPath = _settingsObject.FindProperty("scriptsPath");
			_propertySettingsScriptTemplate = _settingsObject.FindProperty("scriptTemplate");
			_propertySettingsPrefabTemplate = _settingsObject.FindProperty("prefabTemplate");

			EditorApplication.playmodeStateChanged += HandlePlayStateChanged;
		}

		protected void OnDisable()
		{
			EditorApplication.playmodeStateChanged -= HandlePlayStateChanged;
		}

		private void HandlePlayStateChanged ()
		{
			if(_viewControllerEditor!=null) DestroyImmediate(_viewControllerEditor);
			_viewController = null;
			_viewControllerEditor = null;
		}

		protected void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			_tabIndex = GUILayout.Toolbar(_tabIndex,UViewEditorUtils.kTabs, GUILayout.ExpandWidth(true), GUILayout.Height(30)  );

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();

			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();

			if(_tabIndex==0){

				ViewController[] controllers = GameObject.FindObjectsOfType<ViewController>();

				if(controllers.Length>0){

					ViewController controller = null;
						
					if(controllers.Length==1){
						controller = controllers[0];
						EditorGUILayout.ObjectField("View Controller",controller,typeof(ViewController),true);
					} else {

						string[] names = new string[controllers.Length];
						int i = 0, l = names.Length;
						for(; i<l; ++i) names[i] = controllers[i].gameObject.name;

						int index = System.Array.IndexOf<ViewController>(controllers,_viewController);
						index = EditorGUILayout.Popup("View Controller",index,names);
						controller = controllers[index];
					}

					if(controller!=_viewController){
						if(_viewControllerEditor!=null) DestroyImmediate(_viewControllerEditor);
						_viewControllerEditor = Editor.CreateEditor(controller);
						_viewController = controller;
					}
						
					_viewControllerEditor.OnInspectorGUI();

				} else {

					if(_viewControllerEditor!=null){
						DestroyImmediate(_viewControllerEditor);
						_viewController = null;
						_viewControllerEditor = null;
					}

					GUIStyle centeredTextStyle = new GUIStyle("label");
					centeredTextStyle.alignment = TextAnchor.MiddleCenter;

					string sceneName = SceneManager.GetActiveScene().name;
					string message = string.Format("No ViewController found in scene '{0}'",sceneName);

					EditorGUILayout.BeginVertical();
					GUILayout.FlexibleSpace();

					EditorGUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();

					EditorGUILayout.LabelField(message,centeredTextStyle,  GUILayout.Width(400));

					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.Space();

					EditorGUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					if(GUILayout.Button("Create ViewController", GUILayout.Width(180), GUILayout.Height(30))){
						UViewEditorUtils.ContextCreateViewController();
					}
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();

					GUILayout.FlexibleSpace();
					EditorGUILayout.EndVertical();

				}

			} else {
				DrawSettingsGUI();
			}

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndScrollView();
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

			EditorGUILayout.BeginHorizontal();

			GUILayout.FlexibleSpace();

			if(GUILayout.Button("Reset to Defaults",GUILayout.Width(120))){
				_settings.RestoreDefaults();
			}

			EditorGUILayout.EndHorizontal();

			_settingsObject.ApplyModifiedProperties();
		}

	}

}
