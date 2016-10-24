using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;

/**
 * CreateViewWindow.cs
 * Author: Luke Holland (http://lukeholland.me/)
 */

namespace UView {

	public class CreateViewWindow : EditorWindow
	{

		private string _createViewName = "";
		private string _scriptPath = "";
		private string _prefabPath = "";

		private UViewSettings _settings;

		protected void OnEnable()
		{
			_settings = UViewEditorUtils.GetSettings();
			_scriptPath = _settings.scriptsPath;
			_prefabPath = _settings.prefabsPath;
		}

		public void OnGUI()
		{
			bool error = false;
			EditorGUILayout.Space();

			_createViewName = EditorGUILayout.TextField("View Name",_createViewName);

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			_prefabPath = UViewEditorUtils.LayoutPathSelector(_prefabPath,"Prefab Path");
			if(!UViewEditorUtils.ValidateResourcePath(_prefabPath)){
				error = true;
				EditorGUILayout.HelpBox(string.Format("Prefabs should be stored in a '{0}' folder",UViewEditorUtils.kResources),MessageType.Error);
			}

			_scriptPath = UViewEditorUtils.LayoutPathSelector(_scriptPath,"Script Path");

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Reset Paths",EditorStyles.miniButton)){
				_scriptPath = _settings.scriptsPath;
				_prefabPath = _settings.prefabsPath;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);

			string pathViewName = string.IsNullOrEmpty(_createViewName) ? "{VIEW_NAME}" : _createViewName;
			EditorGUILayout.LabelField(CreatePath(pathViewName,_prefabPath,"prefab"));
			EditorGUILayout.LabelField(CreatePath(pathViewName,_scriptPath,"cs"));

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();

			EditorGUILayout.BeginVertical();
			GUILayout.FlexibleSpace();

			EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_createViewName) || error);
			if(GUILayout.Button("Create")){
				CreateView(_createViewName,_prefabPath,_scriptPath);
				Close();
			}
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndVertical();

			EditorGUILayout.Space();
		}

		public void OnInspectorUpdate()
		{
			Repaint();
		}

		private string CreatePath(string viewName, string folder, string extension)
		{			
			viewName = viewName.Replace(" ","");
			return Path.Combine(folder,string.Format("{0}.{1}",viewName,extension));
		}

		private void CreateView(string viewName, string prefabsFolder, string scriptsFolder)
		{
			viewName = viewName.Replace(" ","");

			if(!Directory.Exists(prefabsFolder)) Directory.CreateDirectory(prefabsFolder);
			if(!Directory.Exists(scriptsFolder)) Directory.CreateDirectory(scriptsFolder);

			// create script asset
			string scriptData = _settings.scriptTemplate.ToString().Replace("{VIEW_NAME}",viewName);
			string scriptPath = CreatePath(viewName,scriptsFolder,"cs");

			File.WriteAllText(scriptPath,scriptData);

			// create prefab asset
			string prefabPath = CreatePath(viewName,prefabsFolder,"prefab");

			Component prefabTemplate = PrefabUtility.InstantiatePrefab(_settings.prefabTemplate) as Component;
			prefabTemplate.gameObject.name = viewName;

			PrefabUtility.CreatePrefab(prefabPath,prefabTemplate.gameObject);

			// store vars for post processor
			EditorPrefs.SetString(UViewEditorUtils.KEY_PREFAB_PATH,prefabPath);
			EditorPrefs.SetString(UViewEditorUtils.KEY_SCRIPT_PATH,scriptPath);

			AssetDatabase.Refresh();

			// cleanup
			DestroyImmediate(prefabTemplate.gameObject);
		}

	}

}
