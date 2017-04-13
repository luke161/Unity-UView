using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

using UView;

public class ViewLevelSelect : AbstractView 
{

	[SerializeField] private LevelData[] _levels;
	[SerializeField] private Button _buttonBack;
	[SerializeField] private RectTransform _containerLevels;
	[SerializeField] private Button _prefabLevelButton;

	protected override void OnCreate ()
	{
		_buttonBack.onClick.AddListener(HandleBackPressed);

		int i = 0, l = _levels.Length;
		for(; i<l; ++i){
			LevelData levelData = _levels[i];
			Button levelButton = Instantiate<Button>(_prefabLevelButton,_containerLevels,false);
			levelButton.onClick.AddListener(()=>{ HandleLevelPressed(levelData); });

			levelButton.GetComponentInChildren<Text>().text = (i+1).ToString();
		}
	}

	protected override void OnShowStart (object data)
	{
		OnShowComplete();
	}

	protected override void OnHideStart ()
	{
		OnHideComplete();
	}

	private void HandleBackPressed()
	{
		ChangeLocation<ViewTitle>();
	}

	private void HandleLevelPressed(LevelData levelData)
	{
		ChangeLocation<ViewLevel>(levelData);
	}

}
