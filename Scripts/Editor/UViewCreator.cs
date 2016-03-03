using UnityEngine;
using UnityEditor;
using System.Collections;

namespace UView {

	[InitializeOnLoad]
	public class UViewCreator 
	{

		static UViewCreator()
		{
			EditorApplication.update += HandleUpdate;
		}

		public static void HandleUpdate()
		{
			if(!EditorApplication.isCompiling && EditorPrefs.HasKey(UViewEditorUtils.KEY_SCRIPT_PATH)){

				MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(EditorPrefs.GetString(UViewEditorUtils.KEY_SCRIPT_PATH));
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorPrefs.GetString(UViewEditorUtils.KEY_PREFAB_PATH));

				if(script!=null && prefab!=null){

					prefab.AddComponent(script.GetClass());

					ViewController viewController = GameObject.FindObjectOfType<ViewController>();
					if(viewController!=null){
						SerializedObject serializedObject = new SerializedObject(viewController);
						SerializedProperty propertyViewAssets = serializedObject.FindProperty("_viewAssets");

						serializedObject.Update();

						int index = propertyViewAssets.arraySize;
						propertyViewAssets.InsertArrayElementAtIndex(index);

						SerializedProperty propertyViewAsset = propertyViewAssets.GetArrayElementAtIndex(index);
						UViewEditorUtils.CreateViewAsset(propertyViewAsset,prefab.GetComponent<AbstractView>());

						serializedObject.ApplyModifiedProperties();
					}

					AssetDatabase.Refresh();
				}

				EditorPrefs.DeleteKey(UViewEditorUtils.KEY_SCRIPT_PATH);
				EditorPrefs.DeleteKey(UViewEditorUtils.KEY_PREFAB_PATH);
			}
		}

	}

}