using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

using UView;

public class ViewQuit : AbstractView 
{

	[SerializeField] private Button _buttonConfirm;
	[SerializeField] private Button _buttonCancel;
	[SerializeField] private Animator _animator;

	protected override void OnCreate ()
	{
		_buttonConfirm.onClick.AddListener(HandleConfirmPressed);
		_buttonCancel.onClick.AddListener(HandleCancelPressed);
	}

	protected override void OnShowStart (object data)
	{
		_animator.Play("Show");
		Invoke("OnShowComplete",0.2f);
	}

	protected override void OnHideStart ()
	{
		_animator.Play("Hide");
		Invoke("OnHideComplete",0.2f);
	}

	private void HandleConfirmPressed()
	{
		Application.Quit();
	}

	private void HandleCancelPressed()
	{
		CloseOverlay(this);
	}

}
