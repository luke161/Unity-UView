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

		private ViewList _viewList;
		private bool _attemptedRebuild;
		private bool _showInvalidWarning;

		protected void OnEnable()
		{
			_attemptedRebuild = false;
			_showInvalidWarning = false;

			_propertyAutoSetup = serializedObject.FindProperty("_autoSetup");
			_propertyDontDestroyOnLoad = serializedObject.FindProperty("_dontDestroyOnLoad");
			_propertyDebug = serializedObject.FindProperty("_debug");
			_propertyViewParent = serializedObject.FindProperty("viewParent");
			_propertyStartingLocation = serializedObject.FindProperty("_startingLocation");
			_propertyViewAssets = serializedObject.FindProperty("_viewAssets");

			_viewList = new ViewList(serializedObject,_propertyViewAssets);
		}

		public override void OnInspectorGUI ()
		{
			serializedObject.Update();

			EditorGUILayout.Space();

			if(Application.isPlaying){
				DrawStatsGUI();

				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}

			EditorGUI.BeginDisabledGroup(Application.isPlaying);
			DrawViewGUI();
			EditorGUI.EndDisabledGroup();

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawStatsGUI()
		{
			ViewController viewController = target as ViewController;

			UViewEditorUtils.LayoutLabelWithPrefix("Loaded Resources",viewController.loadedResourceCount);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Locations",EditorStyles.boldLabel);

			UViewEditorUtils.LayoutLabelWithPrefix("Current Location",string.Format("{0} ({1})",viewController.currentLocation.GetType(),viewController.currentLocation.state));
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

			_viewList.requiresRebuild = false;
			_viewList.UpdateLoadedViews();
			_viewList.DoLayoutList();
			if(_viewList.requiresRebuild && !_attemptedRebuild){
				Debug.LogWarning("Views missing or changed, rebuilding...");
				UViewEditorUtils.Rebuild(_propertyViewAssets);
				_viewList.requiresRebuild = false;
				_attemptedRebuild = true;
			}

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5);

			EditorGUILayout.LabelField("Add Existing",GUILayout.Width(80));

			GameObject viewGameObject = EditorGUILayout.ObjectField(null,typeof(GameObject),false) as GameObject;
			if(viewGameObject!=null){
				AbstractView view = viewGameObject.GetComponent<AbstractView>();
				if(view!=null){
					// TODO: check this view isn't already in the list

					_showInvalidWarning = false;

					int index = _propertyViewAssets.arraySize;
					_propertyViewAssets.InsertArrayElementAtIndex(index);
					UViewEditorUtils.CreateViewAsset(_propertyViewAssets.GetArrayElementAtIndex(index),view as AbstractView);
				} else {
					_showInvalidWarning = true;
				}
			}

			if(GUILayout.Button("Create New",GUILayout.Width(100))){
				UViewEditorUtils.ContextCreateView();
			}

			GUILayout.Space(5);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5);

			if(GUILayout.Button("Rebuild List",GUILayout.Width(110))){
				_attemptedRebuild = false;
				UViewEditorUtils.Rebuild(_propertyViewAssets);
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();

			EditorGUI.EndDisabledGroup();

			if(_showInvalidWarning){
				EditorGUILayout.HelpBox("Asset must have an AbstractView component attached",MessageType.Warning);
			}

			EditorGUILayout.Space();

			if(locked){
				EditorGUILayout.Space();
				EditorGUILayout.Space();

				EditorGUILayout.HelpBox("Creating View...",MessageType.Info);
			}
		}

	}

}