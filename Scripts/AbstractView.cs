using UnityEngine;
using System.Collections;
using System.Timers;

/**
 * AbstractView.cs
 * Author: Luke Holland (http://lukeholland.me/)
 */

namespace UView {

	/// <summary>
	/// The current display mode for a View, dictates how the View is handled by a <c>ViewController</c>.
	/// </summary>
	public enum ViewDisplayMode {
		/// <summary>
		/// View is displaying as a Location, within a <c>ViewController</c> only one location View can be active at a time.
		/// </summary>
		Location = 0,
		/// <summary>
		/// View is displaying as an Overlay, within a <c>ViewController</c> multiple overlay views can be showing, including
		/// multiple views of the same type.
		/// </summary>
		Overlay = 1
	}

	/// <summary>
	/// The current state of a View. 
	/// </summary>
	public enum ViewState {
		/// <summary>
		/// View is being created.
		/// </summary>
		Creating = 0,
		/// <summary>
		/// View has been created and is transitioning in.
		/// </summary>
		Showing = 1,
		/// <summary>
		/// View is transitioning out.
		/// </summary>
		Hiding = 2,
		/// <summary>
		/// View has finished transitioning in and is fully setup.
		/// </summary>
		Active = 3,
		/// <summary>
		/// View has been destroyed, references to the <c>ViewController</c> have been removed.
		/// </summary>
		Destroyed = 4
	}

	/// <summary>
	/// Base class for all views. Extend this class and implement <c>OnCreate</c>, <c>OnShowStart</c> & <c>OnHideStart</c> to create
	/// views. Views are loaded and managed by a <c>ViewController</c>.
	/// </summary>
	public abstract class AbstractView : MonoBehaviour {

		/// <summary>
		/// The current display mode for this View. Set by a <c>ViewController</c> dependent on whether the view was requested as a Location or Overlay.
		/// </summary>
		public ViewDisplayMode displayMode { get; private set; }
		/// <summary>
		/// The current state of this View, updated through internal calls.
		/// </summary>
		public ViewState state { get; private set; }

		/// <summary>
		/// Reference to the ViewController that manages this View.
		/// </summary>
		protected ViewController _controller { get; private set; }
						
		/// <summary>
		/// Initializes a new instance of the <see cref="AbstractView"/> class. Puts it in the <c>Creating</c> state.
		/// </summary>
		public AbstractView()
		{
			state = ViewState.Creating;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="UView.AbstractView"/> by type name.
		/// </summary>
		public override string ToString ()
		{
			return GetType().Name;
		}

		/// <summary>
		/// Create the View.
		/// </summary>
		/// <param name="controller">ViewController that's managing this View.</param>
		/// <param name="displayMode">Display mode for this View.</param>
		internal void _Create(ViewController controller, ViewDisplayMode displayMode)
		{
			_controller = controller;

			this.displayMode = displayMode;
			this.state = ViewState.Creating;

			OnCreate();
		}
		
		/// <summary>
		/// Show the view. Puts the view into the <c>Showing</c> state.
		/// </summary>
		/// <param name='data'>
		/// Optional data property, passed through from the <c>ViewController</c> by another view.
		/// </param>
		internal void _Show(object data=null){
			
			if(state==ViewState.Active || state==ViewState.Showing){
				OnHideComplete();	
			}

			if(!gameObject.activeSelf) gameObject.SetActive(true);

			state = ViewState.Showing;
			_controller._OnShowStart(this);
			OnShowStart(data);		
		}

		/// <summary>
		/// Hide the View. Puts the View into the <c>Hiding</c> state.
		/// </summary>
		internal void _Hide()
		{
			if(state==ViewState.Active || state==ViewState.Showing){
				state = ViewState.Hiding;

				_controller._OnHideStart(this);
				OnHideStart();	
			}
		}	

		/// <summary>
		/// Called from ViewController after a view has finished hiding and is ready to be cleaned up. Puts the View
		/// into the <c>Destroyed</c> state.
		/// </summary>
		public virtual void DestroyView()
		{
			state = ViewState.Destroyed;

			_controller.Unload(GetType());
			_controller = null;

			Destroy(gameObject);
		}

		/// <summary>
		/// Effects the position of this View within a transform hierarchy, useful when working with Unity UI. The default behaviour is to put all Locations at 0
		/// and Overlays at the top. If the view has no parent -1 is returned. Override this function to provide custom logic per View.
		/// </summary>
		/// <returns>The sibling index.</returns>
		/// <param name="viewParent">The parent transform for this View</param>
		/// <param name="displayMode">The display mode for this View.</param>
		public virtual int GetSiblingIndex(Transform viewParent, ViewDisplayMode displayMode)
		{
			return viewParent==null ? -1 : displayMode== ViewDisplayMode.Location ? 0 : viewParent.childCount;
		}

		/// <summary>
		/// Requests <c>view</c> be opened as a new location. <c>currentLocation</c> will be hidden before the next view is shown.
		/// </summary>
		/// <param name='data'>
		/// Optional data <c>object</c> to be passed onto the next location when it's shown.
		/// </param>
		/// <typeparam name="T">Type of view we want to move to.</typeparam>
		public void ChangeLocation<T>(object data = null) where T : AbstractView
		{
			ChangeLocation(typeof(T),data);
		}
		
		/// <summary>
		/// Requests <c>view</c> be opened as a new location. <c>currentLocation</c> will be hidden before the next view is shown.
		/// </summary>
		/// <param name='view'>
		/// Type of view we want to move to.
		/// </param>
		/// <param name='data'>
		/// Optional data <c>object</c> to be passed onto the next location when it's shown.
		/// </param>
		public void ChangeLocation(System.Type view, object data = null)
		{
			if(_controller!=null && state==ViewState.Active) _controller.ChangeLocation(view,data);	
		}

		/// <summary>
		/// Opens the specified view as an overlay.
		/// </summary>
		/// <param name="data">Optional data <c>object</c> to be passed onto the view when it's shown.</param>
		/// <param name="close">Optional view to close before opening the target view.</param>
		/// <typeparam name="T">Type of view we want to open.</typeparam>
		public void OpenOverlay<T>(object data = null, AbstractView waitForViewToClose = null) where T : AbstractView
		{
			OpenOverlay(typeof(T),data,waitForViewToClose);
		}

		/// <summary>
		/// Opens the specified view as an overlay.
		/// </summary>
		/// <param name="view">Type of view we want to open.</param>
		/// <param name="data">Optional data <c>object</c> to be passed onto the view when it's shown.</param>
		/// <param name="close">Optional view to close before opening the target view.</param>
		public void OpenOverlay(System.Type view, object data = null, AbstractView waitForViewToClose = null)
		{
			if (_controller!=null) _controller.OpenOverlay(view,data,waitForViewToClose);
		}

		/// <summary>
		/// Closes the specified view when showing as an overlay.
		/// </summary>
		/// <typeparam name="T">Type of view we want to close.</typeparam>
		public void CloseOverlay<T>() where T : AbstractView
		{
			CloseOverlay(typeof(T));
		}

		/// <summary>
		/// Closes the specified view when showing as an overlay.
		/// </summary>
		/// <param name="view">Type of view we want to close.</param>
		public void CloseOverlay(System.Type view)
		{
			if(_controller!=null) _controller.CloseOverlay(view);	
		}

		/// <summary>
		/// Closes the specified view when showing as an overlay.
		/// </summary>
		/// <param name="view">The view we want to close.</param>
		public void CloseOverlay(AbstractView view)
		{
			if(_controller!=null) _controller.CloseOverlay(view);
		}

		/// <summary>
		/// Called when the View is created inside the <c>ViewController</c>. Override to setup the view and register any events with the <c>ViewController</c> 
		/// using the <c>_controller</c> property.
		/// </summary>
		protected virtual void OnCreate()
		{

		}
		
		/// <summary>
		/// Called after <c>OnCreate</c> when the view should be presented. If you're overriding <c>OnShowStart</c> in your view you MUST call
		/// <c>OnShowComplete</c> once any presentation/transitions have finished.
		/// </summary>
		/// <param name='data'>
		/// Optional data property, usually passed through from the ViewProxy by another view.
		/// </param>
		protected virtual void OnShowStart(object data = null)
		{
			OnShowComplete();
		}

		/// <summary>
		/// Should be called once the view has finished transitioning in, after <c>OnShowStart</c>. This will notify the <c>ViewController</c> that
		/// the view is ready and in the <c>Active<c> state.
		/// </summary>
		protected virtual void OnShowComplete()
		{
			state = ViewState.Active;
			_controller._OnShowComplete(this);
		}

		/// <summary>
		/// Called from the <c>ViewController</c> when this view should close and stop presenting. If you're overriding <c>OnHideStart</c> in your view you MUST call
		/// <c>OnHideComplete</c> once any presentation/transitions have finished.
		/// </summary>
		protected virtual void OnHideStart()
		{
			OnHideComplete();	
		}

		/// <summary>
		/// Should be called once the view has finished transitioning out, after <c>OnHideStart</c>. This will notify the <c>ViewController</c> that
		/// the view is hidden and can be cleaned up.
		/// </summary>
		protected virtual void OnHideComplete()
		{
			_controller._OnHideComplete(this);
		}

	}
}