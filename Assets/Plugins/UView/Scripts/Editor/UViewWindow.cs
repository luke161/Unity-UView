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
		private UViewSettings _settings;
		private Editor _viewControllerEditor;
		private Editor _settingsEditor;

		protected void OnEnable()
		{
			_tabIndex = 0;

			_settings = UViewEditorUtils.GetSettings();

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
						if(index<0) index = 0;

						index = EditorGUILayout.Popup("View Controller",index,names);
						controller = index>=0 && index<controllers.Length ? controllers[index] : null;
					}

					if(controller!=_viewController){
						if(_viewControllerEditor!=null) DestroyImmediate(_viewControllerEditor);
						_viewControllerEditor = Editor.CreateEditor(controller);
						_viewController = controller;
					}
						
					if(_viewControllerEditor!=null) _viewControllerEditor.OnInspectorGUI();

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
				if(_settingsEditor==null) _settingsEditor = Editor.CreateEditor(_settings);

				_settingsEditor.OnInspectorGUI();
			}

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndScrollView();
		}

	}

}