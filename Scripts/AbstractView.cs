using UnityEngine;
using System.Collections;
using System.Timers;

/**
 * AbstractView.cs
 * Author: Luke Holland (http://lukeholland.me/)
 */

namespace UView {

	/// <summary>
	/// View display mode.
	/// </summary>
	public enum ViewDisplayMode {
		Location,
		Overlay
	}

	public enum ViewState {
		Creating,
		Showing,
		Hiding,
		Active,
		Destroyed
	}

	/// <summary>
	/// Abstract view.
	/// </summary>
	public abstract class AbstractView : MonoBehaviour{

		public ViewDisplayMode displayMode { get; private set; }
		public ViewState state { get; private set; }

		/// <summary>
		/// Reference to the ViewController that manages this view.
		/// </summary>
		protected ViewController _controller;
						
		/// <summary>
		/// Initializes a new instance of the <see cref="AbstractView"/> class.
		/// </summary>
		public AbstractView()
		{
			state = ViewState.Creating;
		}

		public override string ToString ()
		{
			return GetType().Name;
		}
		
		/// <summary>
		/// Show the view.
		/// </summary>
		/// <param name='data'>
		/// Optional data property, usually passed through from the ViewController by another view.
		/// </param>
		public void Show(object data=null){
			
			if(state==ViewState.Active || state==ViewState.Showing){
				OnHideComplete();	
			}

			if(!gameObject.activeSelf) gameObject.SetActive(true);

			state = ViewState.Showing;
			_controller._OnShowStart(this);
			OnShowStart(data);		
		}
		/// <summary>
		/// Hide the view.
		/// </summary>
		public void Hide()
		{
			if(state==ViewState.Active || state==ViewState.Showing){
				state = ViewState.Hiding;

				_controller._OnHideStart(this);
				OnHideStart();	
			}
		}	

		/// <summary>
		/// Called from ViewController after a view has finished hiding.
		/// </summary>
		public virtual void DestroyView()
		{
			state = ViewState.Destroyed;

			_controller.Unload(GetType());
			_controller = null;

			Destroy(gameObject);
		}

		public virtual int GetSiblingIndex(Transform viewParent, ViewDisplayMode displayMode)
		{
			return viewParent==null ? -1 : displayMode== ViewDisplayMode.Location ? 0 : viewParent.childCount;
		}

		public void ChangeLocation<T>(object data = null) where T : AbstractView
		{
			ChangeLocation(typeof(T),data);
		}
		
		/// <summary>
		/// Requests a new location. <c>currentLocation</c> will be hidden before the <c>targetLocation</c> is shown.
		/// </summary>
		/// <param name='locationName'>
		/// Name of the new <c>targetLocation</c>.
		/// </param>
		/// <param name='data'>
		/// Optional data <c>object</c> to be passed onto the <c>targetLocation</c> when it's shown.
		/// </param>
		public void ChangeLocation(System.Type view, object data = null)
		{
			if(_controller!=null && state==ViewState.Active) _controller.ChangeLocation(view,data);	
		}

		public void OpenOverlay<T>(object data = null) where T : AbstractView
		{
			OpenOverlay(typeof(T),data);
		}

		/// <summary>
		/// Opens the specified overlay.
		/// </summary>
		/// <param name='overlayName'>
		/// Name of the overlay to open.
		/// </param>
		/// <param name='data'>
		/// Optional data object to be passed onto the <c>targetOverlay</c> when it's shown.
		/// </param>
		/// <param name='close'>
		/// Overlay to close first and delay the <c>targetOverlay</c> from showing until they have all hidden.
		/// </param>
		public void OpenOverlay(System.Type view, object data = null, AbstractView close = null)
		{
			if (_controller!=null) _controller.OpenOverlay(view,data,close);
		}

		public void CloseOverlay<T>() where T : AbstractView
		{
			CloseOverlay(typeof(T));
		}

		/// <summary>
		/// Closes the specified overlay
		/// </summary>
		/// <param name="overlayName">Overlay name.</param>
		public void CloseOverlay(System.Type view)
		{
			if(_controller!=null) _controller.CloseOverlay(view);	
		}

		public void CloseOverlay(AbstractView view)
		{
			if(_controller!=null) _controller.CloseOverlay(view);
		}
			
		internal void _Create(ViewController controller, ViewDisplayMode displayMode)
		{
			_controller = controller;

			this.displayMode = displayMode;
			this.state = ViewState.Creating;
			
			OnCreate();
		}	
		
		/// <summary>
		/// Called after <c>show()</c>, override this method to provide custom setup and transition behaviour for your view.
		/// </summary>
		/// <param name='data'>
		/// Optional data property, usually passed through from the ViewProxy by another view.
		/// </param>
		protected virtual void OnShowStart(object data=null)
		{
			OnShowComplete();
		}
		/// <summary>
		/// Called once <c>onShowStart()</c> is complete. If you override <c>onShowStart</c> or <c>onShowComplete</c> make sure you 
		/// call the base method so that view events are dispatched correctly.
		/// </summary>
		protected virtual void OnShowComplete()
		{
			state = ViewState.Active;
			_controller._OnShowComplete(this);
		}
		/// <summary>
		/// Called after <c>hide()</c>, override this method to provide custom transition behaviour for your view.
		/// </summary>
		protected virtual void OnHideStart()
		{
			OnHideComplete();	
		}
		/// <summary>
		/// Called once <c>onHideStart()</c> is complete. If you override <c>onHideStart</c> or <c>onShowComplete</c> make sure you
		/// call the base method so that view events are dispatched correctly.
		/// </summary>
		protected virtual void OnHideComplete()
		{
			_controller._OnHideComplete(this);
		}
		/// <summary>
		/// Called once when the view is registered to the <c>ViewProxy</c>, this happens after <c>ViewProxy.Awake()</c>. Override to 
		/// setup your view and register any events with the <c>ViewProxy</c> using the <c>_manager</c> property.
		/// </summary>
		protected virtual void OnCreate()
		{
			
		}

	}
}