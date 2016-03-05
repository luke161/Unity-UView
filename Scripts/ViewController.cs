using UnityEngine;
using System.Collections.Generic;

/**
 * ViewController.cs
 * Author: Luke Holland (http://lukeholland.me/)
 */

namespace UView {

	[System.Serializable]
	public class ViewAsset 
	{

		public string viewTypeID;
		public string resourcePath;
		public string assetID;

		public ViewAsset(string viewTypeID, string resourcePath, string assetID)
		{
			this.viewTypeID = viewTypeID;
			this.resourcePath = resourcePath;
			this.assetID = assetID;
		}

		public System.Type viewType {
			get {
				return System.Type.GetType(viewTypeID);
			}
		}

	}

	public class ViewController : MonoBehaviour
	{

		public delegate void ViewEvent(ViewController sender, System.Type view);

		public event ViewEvent EventShowStart;
		public event ViewEvent EventShowComplete;
		public event ViewEvent EventHideStart;
		public event ViewEvent EventHideComplete;
		public event ViewEvent EventLocationRequested;
		public event ViewEvent EventViewCreated;

		public bool IsSetup { get; private set; }
		public Transform viewParent = null;

		[SerializeField] private string _startingLocation = null;
		[SerializeField] private bool _autoSetup = true;
		[SerializeField] private bool _debug = false;
		[SerializeField] private bool _dontDestroyOnLoad = true;
		[SerializeField] private List<ViewAsset> _viewAssets;

		private Dictionary<System.Type,ViewAsset> _assetLookup;
		private Dictionary<System.Type,Object> _loadedResources;

		private AbstractView _currentLocation;
		private System.Type _targetLocation;
		private object _targetLocationData;
		private System.Type _lastLocation;	

		private List<AbstractView> _showingOverlays;
		private System.Type _targetOverlay;
		private object _targetOverlayData;

		protected void Start()
		{
			if(_autoSetup) Setup(System.Type.GetType(_startingLocation));
		}

		public void Setup(System.Type startLocation)
		{
			if(IsSetup) return;

			if(_dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

			_assetLookup = new Dictionary<System.Type, ViewAsset>();
			_loadedResources = new Dictionary<System.Type, Object>();
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

		public AbstractView currentLocation {
			get { return _currentLocation; }	
		}

		public System.Type targetLocation {
			get { return _targetLocation; }	
		}

		public System.Type lastLocation{
			get { return _lastLocation;	}
		}

		public System.Type targetOverlay { 
			get { return _targetOverlay; }
		}

		public AbstractView[] showingOverlays {
			get { return _showingOverlays.ToArray(); }
		}

		public int loadedResourceCount {
			get { return _loadedResources.Count; }
		}

		public bool IsOverlayShowing<T>() where T : AbstractView
		{
			return IsOverlayShowing(typeof(T));
		}

		public bool IsOverlayShowing(System.Type view){
			int i = 0, l = _showingOverlays.Count;
			for(; i<l; ++i){
				AbstractView overlay = _showingOverlays[i];
				if(overlay.GetType()==view) return true;
			}

			return false;
		}

		public bool IsOverlayShowing(AbstractView overlay) {
			return _showingOverlays.Contains(overlay);
		}

		public bool HasView<T>() where T : AbstractView
		{
			return HasView(typeof(T));
		}

		public bool HasView(System.Type view)
		{
			return _assetLookup.ContainsKey(view);
		}

		public bool IsViewLoaded<T>() where T : AbstractView
		{
			return IsViewLoaded(typeof(T));
		}

		public bool IsViewLoaded(System.Type viewId)
		{
			return _loadedResources.ContainsKey(viewId);
		}

		public void ChangeLocation<T>(object data, bool immediate) where T : AbstractView
		{
			ChangeLocation(typeof(T),data,immediate);
		}

		/// <summary>
		/// Requests a new location. <c>currentLocation</c> will be hidden before the <c>targetLocation</c> is shown.
		/// </summary>
		/// <param name='viewName'>
		/// Name of the new <c>targetLocation</c>.
		/// </param>
		/// <param name='data'>
		/// Optional data <c>object</c> to be passed onto the <c>targetLocation</c> when it's shown.
		/// </param>
		public void ChangeLocation(System.Type view, object data, bool immediate = false)
		{
			if(!HasView(view)){
				throw new UnityException (string.Format("Invalid view type: {0}",view));
			}

			if(_currentLocation==null || view!=_currentLocation.GetType()){

				if(_debug) Debug.LogFormat("[ViewController] Requesting Location: {0}, immediate: {1}",view.Name,immediate);
				
				if(EventLocationRequested!=null) EventLocationRequested(this,_targetLocation);

				if(_currentLocation==null){
					CreateViewAsLocation(view,data);
				} else if(immediate){
					_currentLocation.Hide();
					CreateViewAsLocation(view,data);
				} else {
					_targetLocation = view;
					_targetLocationData = data;
					_currentLocation.Hide();
				}
			}
		}

		public void OpenOverlay<T>(object data, AbstractView closeOverlay) where T : AbstractView
		{
			OpenOverlay(typeof(T),data,closeOverlay);
		}

		public void OpenOverlay(System.Type view, object data, AbstractView closeOverlay)
		{
			if(!HasView(view)){
				throw new UnityException (string.Format("Invalid view type: {0}",view));
			}
			
			if(closeOverlay!=null && IsOverlayShowing(closeOverlay)){
				_targetOverlay = view;
				_targetOverlayData = data;

				CloseOverlay(closeOverlay);
			} else {
				CreateViewAsOverlay(view,data);
			}
		}

		public void OpenOverlay<T>(object data, bool closeAllOpenOverlays) where T : AbstractView
		{
			OpenOverlay(typeof(T),data,closeAllOpenOverlays);
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
		/// <param name='closeAll'>
		/// Setting <c>closeAll</c> to true will close all open overlays and delay the <c>targetOverlay</c> from showing until they have all hidden.
		/// </param>
		public void OpenOverlay(System.Type view, object data, bool closeAllOpenOverlays)
		{
			if(!HasView(view)){
				throw new UnityException (string.Format("Invalid view name: {0}",view));
			}

			if(closeAllOpenOverlays && _showingOverlays.Count>0){
				_targetOverlay = view;
				_targetOverlayData = data;

				CloseAllOverlays();
			} else {
				CreateViewAsOverlay(view,data);
			}
		}

		public void CloseOverlay<T>() where T : AbstractView
		{
			CloseOverlay(typeof(T));
		}

		/// <summary>
		/// Close all open overlays with the given overlay name
		/// </summary>
		/// <param name="overlayName">Overlay name.</param>
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

		public void CloseOverlay(AbstractView overlay)
		{
			if(IsOverlayShowing(overlay)){
				overlay.Hide();
			}
		}

		public void CloseAllOverlays()
		{
			AbstractView[] e = showingOverlays;
			foreach(AbstractView overlay in e){
				overlay.Hide();
			}	
		}

		public void Unload<T>() where T : AbstractView
		{
			Unload(typeof(T));
		}

		/// <summary>
		/// Unload the view resource associated with viewName.
		/// </summary>
		/// <param name="viewName">View name.</param>
		public void Unload(System.Type view)
		{
			if(IsViewLoaded(view)){
				if(_debug) Debug.LogFormat("[ViewController] Unload View: {0}",view.Name);

				Object resource = _loadedResources[view];
				_loadedResources.Remove(view);

				Resources.UnloadAsset(resource);
			}
		}

		/// <summary>
		/// Unload all cached view resources.
		/// </summary>
		public void UnloadAll()
		{
			foreach(Object viewResource in _loadedResources.Values){
				Resources.UnloadAsset(viewResource);
			}

			_loadedResources.Clear();
		}

		// All view behavour comes through the ViewController, this allows events which other views or gameObjects
		// can listen for to respond to updates in the model.

		internal void _OnShowStart(AbstractView view)
		{
			if(_debug) Debug.LogFormat("[ViewController] Show Start: {0}",view.ToString());

			if(view!=null && EventShowStart!=null) EventShowStart(this,view.GetType());	
		}

		internal void _OnShowComplete(AbstractView view)
		{
			if(_debug) Debug.LogFormat("[ViewController] Show Complete: {0}",view.ToString());

			if(view!=null && EventShowComplete!=null) EventShowComplete(this,view.GetType());	
		}

		internal void _OnHideStart(AbstractView view)
		{
			if(_debug) Debug.LogFormat("[ViewController] Hide Start: {0}",view.ToString());

			if(view!=null && EventHideStart!=null) EventHideStart(this,view.GetType());	
		}

		internal void _OnHideComplete(AbstractView view, bool destroy = true)
		{
			if(view==null) throw new UnityException("View cannot be null");

			if(_debug) Debug.LogFormat("[ViewController] Hide Complete: {0}, destroy: {1}",view.ToString(),destroy);

			if(EventHideComplete!=null) EventHideComplete(this,view.GetType());

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
			_currentLocation.Show(data);
		}

		private void CreateViewAsOverlay(System.Type view, object data)
		{			
			AbstractView overlay = CreateView (_assetLookup[view],ViewDisplayMode.Overlay) as AbstractView;

			_showingOverlays.Add(overlay);
			overlay.Show(data);
		}

		private AbstractView CreateView(ViewAsset asset, ViewDisplayMode displayMode)
		{
			if(_debug) Debug.LogFormat("[ViewController] Creating View: {0}, displayMode: {1}",asset.viewType.Name,displayMode);

			GameObject resource = null;

			if(!IsViewLoaded(asset.viewType)){
				resource = Resources.Load<GameObject>(asset.resourcePath);
				_loadedResources.Add(asset.viewType,resource);
			} else {
				resource = _loadedResources[asset.viewType] as GameObject;
			}

			if(resource!=null){
				AbstractView view = (Instantiate (resource) as GameObject).GetComponent<AbstractView>();

				if(view==null){
					Unload(asset.viewType);
					throw new UnityException(string.Format("Resource for {0} has no view component attached!",asset.viewType));
				}

				view.transform.SetParent(viewParent,false);
				view._Create (this,displayMode);

				if (EventViewCreated != null)
					EventViewCreated (this, asset.viewType);

				return view;
			} else {
				throw new UnityException(string.Format("Resource not found for: {0}",asset.viewType));
			}
		}

	}

}