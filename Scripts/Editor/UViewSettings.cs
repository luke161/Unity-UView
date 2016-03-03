using UnityEngine;
using UnityEditor;
using System.Collections;

namespace UView {

	public class UViewSettings : ScriptableObject
	{

		private const string kDefaultPrefabPath = "Assets/Resources/View";
		private const string kDefaultScriptPath = "Assets/Scripts/View";
		private const string kDefaultScriptTemplatePath = "Assets/UView/Templates/ViewScriptTemplate.txt";
		private const string kDefaultPrefabTemplatePath = "Assets/UView/Templates/ViewPrefabTemplate.prefab";

		public string prefabsPath;
		public string scriptsPath;
		public TextAsset scriptTemplate;
		public Transform prefabTemplate;

		public void RestoreDefaults()
		{
			prefabsPath = kDefaultPrefabPath;
			scriptsPath = kDefaultScriptPath;
			scriptTemplate = AssetDatabase.LoadAssetAtPath<TextAsset>(kDefaultScriptTemplatePath);
			prefabTemplate = AssetDatabase.LoadAssetAtPath<Transform>(kDefaultPrefabTemplatePath);
		}

	}

}