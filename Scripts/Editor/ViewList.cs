using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;

namespace UView {

	public class ViewList : ReorderableList
	{

		private SerializedProperty _propertyViewParent;

		public ViewList(SerializedObject serializedObject, SerializedProperty elements) : base(serializedObject,elements)
		{
			_propertyViewParent = serializedObject.FindProperty("viewParent");

			this.drawHeaderCallback = DrawHeaderCallback;
			this.drawElementCallback = DrawElementCallback;
			this.onRemoveCallback = OnRemoveCallback;
			this.onAddDropdownCallback = OnAddDropdownCallback;
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

			AbstractView viewAsset = AssetDatabase.LoadAssetAtPath<AbstractView>(AssetDatabase.GUIDToAssetPath(propertyAssetID.stringValue));
			if(viewAsset!=null){

				EditorGUI.LabelField(new Rect(rect.x,rect.y,rect.width,rect.height),viewName);

				AbstractView sceneInstance = UViewEditorUtils.FindLoadedView(viewAsset.GetType());

				bool existsInScene = sceneInstance!=null;
				if(existsInScene && GUI.Button(new Rect(rect.x+rect.width-55,rect.y,55,rect.height-4), "Unload", EditorStyles.miniButton)){
					GameObject.DestroyImmediate(sceneInstance.gameObject);
				} else if(!existsInScene && GUI.Button(new Rect(rect.x+rect.width-55,rect.y,55,rect.height-4),"Load", EditorStyles.miniButton)){
					AbstractView instance = PrefabUtility.InstantiatePrefab(viewAsset) as AbstractView;
					instance.gameObject.hideFlags = HideFlags.DontSaveInEditor;
					instance.transform.SetParent(_propertyViewParent.objectReferenceValue as Transform,false);

					Selection.activeGameObject = instance.gameObject;
				}
			} else {
				EditorGUI.LabelField(new Rect(rect.x,rect.y,rect.width,rect.height),string.Format("{0} (Asset Missing)",viewName),EditorStyles.boldLabel);
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

		private void OnAddDropdownCallback(Rect buttonRect, ReorderableList list) 
		{  
			AssetDatabase.Refresh();

			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Create New"),false,OnCreateViewCallback);
			menu.AddSeparator(string.Empty);

			string[] currentViews = UViewEditorUtils.GetViewNames(serializedProperty,false);
			string[] assets = AssetDatabase.FindAssets("t:Prefab");

			int i = 0, l = assets.Length;
			for(; i<l; ++i){
				
				AbstractView view = AssetDatabase.LoadAssetAtPath<AbstractView>( AssetDatabase.GUIDToAssetPath( assets[i] ));
				if(view!=null && System.Array.IndexOf<string>(currentViews,view.GetType().AssemblyQualifiedName)==-1){
					menu.AddItem(new GUIContent(view.ToString()),false,OnAddViewCallback,view);
				}
			}

			menu.ShowAsContext();
		}

		private void OnCreateViewCallback()
		{
			UViewEditorUtils.MenuCreateView();
		}

		private void OnAddViewCallback(object view)
		{
			serializedProperty.serializedObject.Update();

			int index = serializedProperty.arraySize;
			serializedProperty.InsertArrayElementAtIndex(index);
			UViewEditorUtils.CreateViewAsset(serializedProperty.GetArrayElementAtIndex(index),view as AbstractView);

			serializedProperty.serializedObject.ApplyModifiedProperties();
		}

	}

}