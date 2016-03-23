using UnityEngine;
using System.Collections.Generic;

/**
 * ViewController.cs
 * Author: Luke Holland (http://lukeholland.me/)
 */

namespace UView {

	/// <summary>
	/// The <c>ViewController</c> class manages the presentation of views, with two separate systems, locations and overlays.
	/// With the location system only one view is shown at a time, moving to a new location forces the current location to hide. With the overlay
	/// system multiple views can be presented at once, independently of each other and the location system.
	/// 
	/// The <c>ViewController</c> stores <c>ViewAsset</c> objects, created via the Unity editor interface that hold a reference to a prefab that
	/// represents a view and that a view can be created from. 
	/// 
	/// All views are created via a <c>ViewController</c> and communicate via a <c>ViewController</c> when updating their state. 
	/// </summary>
	public class ViewController : MonoBehaviour
	{

		public delegate void ViewEvent(ViewController sender, System.Type view, ViewDisplayMode displayMode);

		/// <summary>Dispatched when a view begins to show and is transitioning in.</summary>
		public event ViewEvent EventShowStart;
		/// <summary>Dispatched when a view has finished transition in and is active.</summary>
		public event ViewEvent EventShowComplete;
		/// <summary>Dispatched when a view begins to hide and is transitioning out.</summary>
		public event ViewEvent EventHideStart;
		/// <summary>Dispatched when a view has finished transitioning out and is no longer active.</summary>
		public event ViewEvent EventHideComplete;
		/// <summary>Dispatched when a view has been requested .</summary>
		public event ViewEvent EventViewRequested;
		/// <summary>Dispatched when a view is created inside the <c>ViewController</c>. This happens before a ShowStart event.</summary>
		public event ViewEvent EventViewCreated;

		/// <summary>
		/// <c>True</c> if the <c>ViewController</c> is setup and ready to use.
		/// </summary>
		public bool IsSetup { get; private set; }
		/// <summary>
		/// Transform all newly created views are parented to. If <c>null</c> views will be created without a transform parent.
		/// </summary>
		public Transform viewParent = null;

		[SerializeField] private string _startingLocation = null;
		[SerializeField] private bool _autoSetup = true;
		[SerializeField] private bool _debug = false;
		[SerializeField] private bool _dontDestroyOnLoad = true;
		[SerializeField] private List<ViewAsset> _viewAssets;

		private Dictionary<System.Type,ViewAsset> _assetLookup;

		private AbstractView _currentLocation;
		private System.Type _targetLocation;
		private System.Type _lastLocation;	
		private object _targetLocationData;

		private List<AbstractView> _showingOverlays;
		private System.Type _targetOverlay;
		private object _targetOverlayData;

		protected void Start()
		{
			if(_autoSetup) Setup(System.Type.GetType(_startingLocation));
		}

		/// <summary>
		/// Setup the <c>ViewController</c> so it's ready to use. If a starting location is set that view
		/// will be created and shown.
		/// </summary>
		/// <param name="startLocation">Type of view to start the location system in.</param>
		public void Setup(System.Type startLocation)
		{
			if(IsSetup) return;

			if(_dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

			_assetLookup = new Dictionary<System.Type, ViewAsset>();
			_showingOverlays = new List<AbstractView> ();

			int i = 0, l = _viewAssets.Count;
			for(; i<l; ++i){
				ViewAsset asset = _viewAssets[i];

				System.Type viewType = asset.viewType;
				if(viewType==null){
					Debug.LogWarningFormat("Invalid View, try rebuilding the ViewController: {0}",asset.viewTypeID);
				} else {
					_assetLookup.Add(asset.viewType,asset);
				}
			}

			IsSetup = true;

			if(startLocation!=null){
				ChangeLocation(startLocation,null);
			}
		}

		/// <summary>
		/// Get the view currently showing in the location system.
		/// </summary>
		public AbstractView currentLocation {
			get { return _currentLocation; }	
		}

		/// <summary>
		/// Get the view that's been requested and will show once the current location finishes hiding.
		/// </summary>
		public System.Type targetLocation {
			get { return _targetLocation; }	
		}

		/// <summary>
		/// Gets the view that was last shown before the current location.
		/// </summary>
		public System.Type lastLocation{
			get { return _lastLocation;	}
		}

		/// <summary>
		/// Gets the view that's been requested and will show as the next overlay.
		/// </summary>
		public System.Type targetOverlay { 
			get { return _targetOverlay; }
		}

		/// <summary>
		/// An array containing all views that are currently open as overlays.
		/// </summary>
		public AbstractView[] showingOverlays {
			get { return _showingOverlays.ToArray(); }
		}

		/// <summary>
		/// Reference count for loaded view resources.
		/// </summary>
		public int loadedResourceCount {
			get { 
				int count = 0;
				foreach(ViewAsset asset in _assetLookup.Values) if(asset.IsResourceLoaded) count++;

				return count; 
			}
		}

		/// <returns><c>true</c> if the specified view type is open as an overlay.</returns>
		/// <typeparam name="T">Type of view.</typeparam>
		public bool IsOverlayOpen<T>() where T : AbstractView
		{
			return IsOverlayOpen(typeof(T));
		}

		/// <returns><c>true</c> if the specified view type is open as an overlay.</returns>
		/// <param name="view">Type of view.</param>
		public bool IsOverlayOpen(System.Type view)
		{
			int i = 0, l = _showingOverlays.Count;
			for(; i<l; ++i){
				AbstractView overlay = _showingOverlays[i];
				if(overlay.GetType()==view) return true;
			}

			return false;
		}

		/// <returns><c>true</c> if the specified view is open as an overlay.</returns>
		public bool IsOverlayOpen(AbstractView view) 
		{
			return _showingOverlays.Contains(view);
		}

		/// <returns><c>true</c> if the <c>ViewController</c> contains a view asset for the specified view type.</returns>
		public bool HasView<T>() where T : AbstractView
		{
			return HasView(typeof(T));
		}

		/// <returns><c>true</c> if the <c>ViewController</c> contains a view asset for the specified view type.</returns>
		public bool HasView(System.Type view)
		{
			return _assetLookup.ContainsKey(view);
		}

		/// <returns><c>true</c> if the associated prefab resource is currently loaded for the specified view type.</returns>
		public bool IsViewLoaded<T>() where T : AbstractView
		{
			return IsViewLoaded(typeof(T));
		}

		/// <returns><c>true</c> if the associated prefab resource is currently loaded for the specified view type.</returns>
		public bool IsViewLoaded(System.Type view)
		{
			if(_assetLookup.ContainsKey(view)){
				return _assetLookup[view].IsResourceLoaded;
			} else {
				return false;
			}
		}

		/// <summary>
		/// Change location to the specified view. <c>currentLocation</c> will first be hidden, and the specified view will become the <c>targetLocation</c>, once
		/// <c>currentLocation</c> is hidden the specified view is created and shown.
		/// </summary>
		/// <param name="data">Data to pass onto the specified view when it begins to show <c>OnShowStart</c>.</param>
		/// <param name="immediate">If set to <c>true</c> the <c>ViewController</c> won't wait for <c>currentLocation</c> to hide before creating the specified view.</param>
		/// <typeparam name="T">The type of view to change location to.</typeparam>
		public void ChangeLocation<T>(object data, bool immediate) where T : AbstractView
		{
			ChangeLocation(typeof(T),data,immediate);
		}

		/// <summary>
		/// Change location to the specified view. <c>currentLocation</c> will first be hidden, and the specified view will become the <c>targetLocation</c>, once
		/// <c>currentLocation</c> is hidden the specified view is created and shown.
		/// </summary>
		/// <param name="view">The type of view to change location to.</param>
		/// <param name="data">Data to pass onto the specified view when it begins to show <c>OnShowStart</c>.</param>
		/// <param name="immediate">If set to <c>true</c> the <c>ViewController</c> won't wait for <c>currentLocation</c> to hide before creating the specified view.</param>
		public void ChangeLocation(System.Type view, object data, bool immediate = false)
		{
			if(!HasView(view)){
				throw new UnityException (string.Format("Invalid view type: {0}",view));
			}

			if(_currentLocation==null || view!=_currentLocation.GetType()){

				if(_debug) Debug.LogFormat("[ViewController] Requesting Location: {0}, immediate: {1}",view.Name,immediate);
				
				if(EventViewRequested!=null) EventViewRequested(this,view,ViewDisplayMode.Location);

				if(_currentLocation==null){
					CreateViewAsLocation(view,data);
				} else if(immediate){
					_currentLocation._Hide();
					CreateViewAsLocation(view,data);
				} else {
					_targetLocation = view;
					_targetLocationData = data;
					_currentLocation._Hide();
				}
			}
		}

		/// <summary>
		/// Open the specified view as an overlay. You can open multiple overlays of the same view type if required.
		/// </summary>
		/// <param name="data">Data to pass onto the specified view when it begins to show <c>OnShowStart</c>.</param>
		/// <param name="waitForViewToClose">If set the <c>ViewController</c> will first hide this view (assuming it's showing as an overlay) before opening the specified view. This mimics the location system.</param>
		/// <typeparam name="T">The type of view to open as an overlay.</typeparam>
		public void OpenOverlay<T>(object data, AbstractView waitForViewToClose) where T : AbstractView
		{
			OpenOverlay(typeof(T),data,waitForViewToClose);
		}

		/// <summary>
		/// Open the specified view as an overlay. You can open multiple overlays of the same view type if required.
		/// </summary>
		/// <param name="view">The type of view to open as an overlay.</param>
		/// <param name="data">Data to pass onto the specified view when it begins to show <c>OnShowStart</c>.</param>
		/// <param name="waitForViewToClose">If set the <c>ViewController</c> will first hide this view (assuming it's showing as an overlay) before opening the specified view. This mimics the location system.</param>
		public void OpenOverlay(System.Type view, object data, AbstractView waitForViewToClose)
		{
			if(!HasView(view)){
				throw new UnityException (string.Format("Invalid view type: {0}",view));
			}

			if(_debug) Debug.LogFormat("[ViewController] Requesting Overlay: {0}",view.Name);

			if(EventViewRequested!=null) EventViewRequested(this,view,ViewDisplayMode.Overlay);
			
			if(waitForViewToClose!=null && IsOverlayOpen(waitForViewToClose)){
				_targetOverlay = view;
				_targetOverlayData = data;

				CloseOverlay(waitForViewToClose);
			} else {
				CreateViewAsOverlay(view,data);
			}
		}

		/// <summary>
		/// Open the specified view as an overlay. You can open multiple overlays of the same view type if required.
		/// </summary>
		/// <param name="data">Data to pass onto the specified view when it begins to show <c>OnShowStart</c>.</param>
		/// <param name="waitForAllOverlaysToClose">If <c>true</c> the <c>ViewController</c> will first hide all views open as overlays before opening the specified view.</param>
		/// <typeparam name="T">The type of view to open as an overlay.</typeparam>
		public void OpenOverlay<T>(object data, bool waitForAllOverlaysToClose) where T : AbstractView
		{
			OpenOverlay(typeof(T),data,waitForAllOverlaysToClose);
		}


		/// <summary>
		/// Open the specified view as an overlay. You can open multiple overlays of the same view type if required.
		/// </summary>
		/// <param name="data">Data to pass onto the specified view when it begins to show <c>OnShowStart</c>.</param>
		/// <param name="waitForAllOverlaysToClose">If <c>true</c> the <c>ViewController</c> will first hide all views open as overlays before opening the specified view.</param>
		/// <param name="view">The type of view to open as an overlay.</param>
		public void OpenOverlay(System.Type view, object data, bool waitForAllOverlaysToClose)
		{
			if(!HasView(view)){
				throw new UnityException (string.Format("Invalid view name: {0}",view));
			}

			if(waitForAllOverlaysToClose && _showingOverlays.Count>0){
				_targetOverlay = view;
				_targetOverlayData = data;

				CloseAllOverlays();
			} else {
				CreateViewAsOverlay(view,data);
			}
		}

		/// <summary>
		/// Close the specified view.
		/// </summary>
		/// <typeparam name="T">The type of view to close.</typeparam>
		public void CloseOverlay<T>() where T : AbstractView
		{
			CloseOverlay(typeof(T));
		}

		/// <summary>
		/// Close the specified view.
		/// </summary>
		/// <param name="view">The type of view to close.</param>
		public void CloseOverlay(System.Type view)
		{
			if(!HasView(view)){
				throw new UnityException (string.Format("Invalid view type: {0}",view));
			}

			int i = _showingOverlays.Count-1;
			for(; i>=0; --i){
				AbstractView o = _showingOverlays[i];
				if(o.GetType()==view){
					CloseOverlay (o);
				}
			}
		}

		/// <summary>
		/// Close the specified view.
		/// </summary>
		/// <param name="view">The view to close.</param>
		public void CloseOverlay(AbstractView view)
		{
			if(IsOverlayOpen(view)){
				view._Hide();
			}
		}

		/// <summary>
		/// Closes all views currently open as overlays.
		/// </summary>
		public void CloseAllOverlays()
		{
			AbstractView[] e = showingOverlays;
			foreach(AbstractView view in e){
				view._Hide();
			}	
		}

		/// <summary>
		/// Unload the specified view, this clears all references to the view internally in the <c>ViewController</c>.
		/// </summary>
		/// <typeparam name="T">The type of view to unload.</typeparam>
		public void Unload<T>() where T : AbstractView
		{
			Unload(typeof(T));
		}

		/// <summary>
		/// Unload the specified view, this clears all references to the view internally in the <c>ViewController</c>.
		/// </summary>
		/// <param name="view">The type of view to unload.</param>
		public void Unload(System.Type view)
		{
			if(IsViewLoaded(view)){
				if(_debug) Debug.LogFormat("[ViewController] Unload View: {0}",view.Name);

				_assetLookup[view].Unload();
			}
		}

		/// <summary>
		/// Forces all currently loaded views to unload, clearing all references to the views internally in the <c>ViewController</c>.
		/// </summary>
		public void UnloadAll()
		{
			foreach(ViewAsset viewAsset in _assetLookup.Values){
				viewAsset.Unload(true);
			}
		}

		internal void _OnShowStart(AbstractView view)
		{
			if(_debug) Debug.LogFormat("[ViewController] Show Start: {0}",view.ToString());

			if(view!=null && EventShowStart!=null) EventShowStart(this,view.GetType(),view.displayMode);	
		}

		internal void _OnShowComplete(AbstractView view)
		{
			if(_debug) Debug.LogFormat("[ViewController] Show Complete: {0}",view.ToString());

			if(view!=null && EventShowComplete!=null) EventShowComplete(this,view.GetType(),view.displayMode);	
		}

		internal void _OnHideStart(AbstractView view)
		{
			if(_debug) Debug.LogFormat("[ViewController] Hide Start: {0}",view.ToString());

			if(view!=null && EventHideStart!=null) EventHideStart(this,view.GetType(),view.displayMode);	
		}

		internal void _OnHideComplete(AbstractView view, bool destroy = true)
		{
			if(view==null) throw new UnityException("View cannot be null");

			if(_debug) Debug.LogFormat("[ViewController] Hide Complete: {0}, destroy: {1}",view.ToString(),destroy);

			if(EventHideComplete!=null) EventHideComplete(this,view.GetType(),view.displayMode);

			if(destroy) view.DestroyView();

			if(view.displayMode==ViewDisplayMode.Overlay){

				// remove overlay from showing list
				if(_showingOverlays.Contains(view)){
					_showingOverlays.Remove(view);
				}
				
				// process next overlay if one is queued
				if(_targetOverlay!=null){
					CreateViewAsOverlay(_targetOverlay,_targetOverlayData);

					// clear data
					_targetOverlay = null;
					_targetOverlayData = null;
				}
			} else if(view.displayMode==ViewDisplayMode.Location){

				// process next location is one is queued
				if(view==_currentLocation && _targetLocation!=null){
					CreateViewAsLocation(_targetLocation,_targetLocationData);

					// clear data
					_targetLocation = null;
					_targetLocationData = null;
				}

			}
		}

		private void CreateViewAsLocation(System.Type view, object data)
		{
			// remove last location
			if (_currentLocation != null){
				_lastLocation = _currentLocation.GetType();
			}

			// create next location
			_currentLocation = CreateView (_assetLookup[view], ViewDisplayMode.Location);
			_currentLocation._Show(data);
		}

		private void CreateViewAsOverlay(System.Type view, object data)
		{			
			AbstractView overlay = CreateView (_assetLookup[view],ViewDisplayMode.Overlay) as AbstractView;

			_showingOverlays.Add(overlay);
			overlay._Show(data);
		}

		protected virtual AbstractView CreateView(ViewAsset asset, ViewDisplayMode displayMode)
		{
			if(_debug) Debug.LogFormat("[ViewController] Creating View: {0}, displayMode: {1}",asset.viewType.Name,displayMode);

			// load the view resource
			GameObject resource = asset.Load() as GameObject;

			if(resource!=null){
				// create an instance of the view resource
				AbstractView view = (Instantiate (resource) as GameObject).GetComponent<AbstractView>();

				if(view==null){
					Unload(asset.viewType);
					throw new UnityException(string.Format("Resource for {0} has no view component attached!",asset.viewType));
				}

				// setup view inside viewParent
				view.transform.SetParent(viewParent,false);
				int siblingIndex = view.GetSiblingIndex(viewParent,displayMode);
				if(siblingIndex>-1) view.transform.SetSiblingIndex(siblingIndex);

				// finish view creation
				view._Create (this,displayMode);

				if (EventViewCreated != null)
					EventViewCreated (this, asset.viewType, view.displayMode);

				return view;
			} else {
				throw new UnityException(string.Format("Resource not found for: {0}",asset.viewType));
			}
		}

	}



	[System.Serializable]
	public class ViewAsset 
	{

		public string viewTypeID;
		public string resourcePath;
		public string assetID;

		internal int referenceCount { get; private set; }
		internal Object resource { get; private set; }

		public ViewAsset(string viewTypeID, string resourcePath, string assetID)
		{
			this.viewTypeID = viewTypeID;
			this.resourcePath = resourcePath;
			this.assetID = assetID;
		}

		internal bool IsResourceLoaded{
			get { return resource!=null && referenceCount>0; }
		}

		internal Object Load()
		{
			if(this.resource==null){
				this.resource = Resources.Load(resourcePath);
				this.referenceCount = 1;
			} else {
				this.referenceCount++;
			}

			return resource;
		}

		internal void Unload(bool force = false)
		{
			if(resource==null) return;

			this.referenceCount = force ? 0 : Mathf.Max(0,referenceCount-1);

			if(referenceCount<=0){
				this.resource = null;
			}
		}

		public System.Type viewType {
			get {
				return System.Type.GetType(viewTypeID);
			}
		}

	}

}