using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;

/**
 * ViewList.cs
 * Author: Luke Holland (http://lukeholland.me/)
 */

namespace UView {

	public class ViewList : ReorderableList
	{

		public bool requiresRebuild = false;

		private Dictionary<System.Type,AbstractView> _loadedViews;
		private SerializedProperty _propertyViewParent;

		public ViewList(SerializedObject serializedObject, SerializedProperty elements) : base(serializedObject,elements,true,true,true,true)
		{
			_loadedViews = new Dictionary<System.Type,AbstractView>();
			_propertyViewParent = serializedObject.FindProperty("viewParent");

			this.drawHeaderCallback = DrawHeaderCallback;
			this.drawElementCallback = DrawElementCallback;
			this.onRemoveCallback = OnRemoveCallback;
			this.onAddCallback = OnAddCallback;
			this.onSelectCallback = OnSelectedCallback;
		}

		public void UpdateLoadedViews()
		{
			_loadedViews.Clear();

			Object[] gameObjects = Resources.FindObjectsOfTypeAll(typeof(AbstractView));
			int i = 0, l = gameObjects.Length;
			for(; i<l; ++i){

				AbstractView view = gameObjects[i] as AbstractView;
				if(!EditorUtility.IsPersistent(view)){
					_loadedViews.Add(view.GetType(),view);
				}
			}
		}

		private void DrawHeaderCallback(Rect rect) 
		{ 
			EditorGUI.LabelField(rect,"Views",EditorStyles.boldLabel); 
		}

		private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused) 
		{
			SerializedProperty propertyViewAsset = serializedProperty.GetArrayElementAtIndex(index);
			SerializedProperty propertyViewTypeID = propertyViewAsset.FindPropertyRelative("viewTypeID");
			SerializedProperty propertyAssetID = propertyViewAsset.FindPropertyRelative("assetID");

			string viewName = UViewEditorUtils.GetViewName(propertyViewTypeID);
			string assetPath = AssetDatabase.GUIDToAssetPath(propertyAssetID.stringValue);

			if(UViewEditorUtils.ValidateViewAsset(propertyViewAsset)){

				System.Type viewType = System.Type.GetType(propertyViewTypeID.stringValue);
				AbstractView sceneInstance = _loadedViews.ContainsKey(viewType) ? _loadedViews[viewType] : null;
				bool existsInScene = sceneInstance!=null;

				EditorGUI.LabelField(new Rect(rect.x,rect.y,rect.width,rect.height),existsInScene ? string.Format("{0} (Loaded)",viewName) : viewName, existsInScene ? EditorStyles.boldLabel : EditorStyles.label);

				if(existsInScene && GUI.Button(new Rect(rect.x+rect.width-55,rect.y,55,rect.height-4), "Unload", EditorStyles.miniButton)){
					GameObject.DestroyImmediate(sceneInstance.gameObject);
				} else if(!existsInScene && GUI.Button(new Rect(rect.x+rect.width-55,rect.y,55,rect.height-4),"Load", EditorStyles.miniButton)){

					AbstractView viewAsset = AssetDatabase.LoadAssetAtPath<AbstractView>(assetPath);
					if(viewAsset!=null){
						AbstractView instance = PrefabUtility.InstantiatePrefab(viewAsset) as AbstractView;
						instance.SetParent(_propertyViewParent.objectReferenceValue as Transform,ViewDisplayMode.Overlay);

						Selection.activeGameObject = instance.gameObject;
					} else {
						Debug.LogErrorFormat("Unable to load {0} ({1}), missing an AbstractView component",viewName,assetPath);
					}
				}
			} else {
				EditorGUI.LabelField(new Rect(rect.x,rect.y,rect.width,rect.height),string.Format("{0} (Asset Missing)",viewName),EditorStyles.boldLabel);
				requiresRebuild = true;
			}
		}

		private void OnRemoveCallback(ReorderableList list)
		{
			int response = EditorUtility.DisplayDialogComplex("Remove View","Do you also want to cleanup the assets associated with this view? (Script & Prefab)","Remove View","Cancel","Remove View & Assets");
			if(response!=1){
				if(response==2){
					UViewEditorUtils.RemoveViewAssets(serializedProperty.GetArrayElementAtIndex(list.index));
				}

				serializedProperty.DeleteArrayElementAtIndex(list.index);
			}
		}

		private void OnAddCallback(ReorderableList list)
		{
			UViewEditorUtils.ContextCreateView();
		}

		private void OnSelectedCallback(ReorderableList list)
		{
			SerializedProperty propertyViewAsset = serializedProperty.GetArrayElementAtIndex(list.index);
			SerializedProperty propertyAssetID = propertyViewAsset.FindPropertyRelative("assetID");

			string assetPath = AssetDatabase.GUIDToAssetPath(propertyAssetID.stringValue);
			if(!string.IsNullOrEmpty(assetPath)){
				EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(assetPath));
			}
		}

	}

}