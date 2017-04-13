using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

using UView;

public class ViewLevel : AbstractView 
{

	[SerializeField] private Text _textLevelName;
	[SerializeField] private Image _imageBackground;
	[SerializeField] private Button _buttonBack;

	protected override void OnCreate ()
	{
		_buttonBack.onClick.AddListener(HandleBackPressed);
	}

	protected override void OnShowStart (object data)
	{
		LevelData levelData = data as LevelData;
		_textLevelName.text = levelData.levelName;
		_imageBackground.color = levelData.backgroundColor;

		OnShowComplete();
	}

	protected override void OnHideStart ()
	{
		OnHideComplete();
	}

	private void HandleBackPressed()
	{
		ChangeLocation<ViewLevelSelect>();
	}

}
