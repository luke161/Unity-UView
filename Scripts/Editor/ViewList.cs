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

			System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
			if(assetType!=null && assetType==typeof(GameObject)){

				EditorGUI.LabelField(new Rect(rect.x,rect.y,rect.width,rect.height),viewName);

				System.Type viewType = System.Type.GetType(propertyViewTypeID.stringValue);
				AbstractView sceneInstance = _loadedViews.ContainsKey(viewType) ? _loadedViews[viewType] : null;

				bool existsInScene = sceneInstance!=null;
				if(existsInScene && GUI.Button(new Rect(rect.x+rect.width-55,rect.y,55,rect.height-4), "Unload", EditorStyles.miniButton)){
					GameObject.DestroyImmediate(sceneInstance.gameObject);
				} else if(!existsInScene && GUI.Button(new Rect(rect.x+rect.width-55,rect.y,55,rect.height-4),"Load", EditorStyles.miniButton)){
					AbstractView viewAsset = AssetDatabase.LoadAssetAtPath<AbstractView>(assetPath);
					AbstractView instance = PrefabUtility.InstantiatePrefab(viewAsset) as AbstractView;
					instance.gameObject.hideFlags = HideFlags.DontSaveInEditor;
					instance.transform.SetParent(_propertyViewParent.objectReferenceValue as Transform,false);

					Selection.activeGameObject = instance.gameObject;
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

	}

}