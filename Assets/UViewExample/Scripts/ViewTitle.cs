using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

using UView;

public class ViewTitle : AbstractView 
{

	[SerializeField] private Button _buttonStart;
	[SerializeField] private Button _buttonQuit;

	protected override void OnCreate ()
	{
		_buttonStart.onClick.AddListener(HandleStartPressed);
		_buttonQuit.onClick.AddListener(HandleQuitPressed);
	}

	protected override void OnShowStart (object data)
	{
		OnShowComplete();
	}

	protected override void OnHideStart ()
	{
		OnHideComplete();
	}

	private void HandleStartPressed()
	{
		ChangeLocation<ViewLevelSelect>();
	}

	private void HandleQuitPressed()
	{
		OpenOverlay<ViewQuit>();
	}

}
