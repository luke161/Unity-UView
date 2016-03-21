using UnityEngine;
using System.Collections.Generic;

/**
 * ViewController.cs
 * Author: Luke Holland (http://lukeholland.me/)
 */

namespace UView {

	public class ViewController : MonoBehaviour
	{

		public delegate void ViewEvent(ViewController sender, System.Type view, ViewDisplayMode displayMode);

		public event ViewEvent EventShowStart;
		public event ViewEvent EventShowComplete;
		public event ViewEvent EventHideStart;
		public event ViewEvent EventHideComplete;
		public event ViewEvent EventViewRequested;
		public event ViewEvent EventViewCreated;

		public bool IsSetup { get; private set; }
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
			get { 
				int count = 0;
				foreach(ViewAsset asset in _assetLookup.Values) if(asset.IsResourceLoaded) count++;

				return count; 
			}
		}

		public bool IsOverlayShowing<T>() where T : AbstractView
		{
			return IsOverlayShowing(typeof(T));
		}

		public bool IsOverlayShowing(System.Type view)
		{
			int i = 0, l = _showingOverlays.Count;
			for(; i<l; ++i){
				AbstractView overlay = _showingOverlays[i];
				if(overlay.GetType()==view) return true;
			}

			return false;
		}

		public bool IsOverlayShowing(AbstractView view) 
		{
			return _showingOverlays.Contains(view);
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
			if(_assetLookup.ContainsKey(viewId)){
				return _assetLookup[viewId].IsResourceLoaded;
			} else {
				return false;
			}
		}

		public void ChangeLocation<T>(object data, bool immediate) where T : AbstractView
		{
			ChangeLocation(typeof(T),data,immediate);
		}

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

		public void OpenOverlay<T>(object data, AbstractView waitForViewToClose) where T : AbstractView
		{
			OpenOverlay(typeof(T),data,waitForViewToClose);
		}

		public void OpenOverlay(System.Type view, object data, AbstractView waitForViewToClose)
		{
			if(!HasView(view)){
				throw new UnityException (string.Format("Invalid view type: {0}",view));
			}

			if(_debug) Debug.LogFormat("[ViewController] Requesting Overlay: {0}",view.Name);

			if(EventViewRequested!=null) EventViewRequested(this,view,ViewDisplayMode.Overlay);
			
			if(waitForViewToClose!=null && IsOverlayShowing(waitForViewToClose)){
				_targetOverlay = view;
				_targetOverlayData = data;

				CloseOverlay(waitForViewToClose);
			} else {
				CreateViewAsOverlay(view,data);
			}
		}

		public void OpenOverlay<T>(object data, bool waitForAllOverlaysToClose) where T : AbstractView
		{
			OpenOverlay(typeof(T),data,waitForAllOverlaysToClose);
		}

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

		public void CloseOverlay<T>() where T : AbstractView
		{
			CloseOverlay(typeof(T));
		}

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

		public void CloseOverlay(AbstractView view)
		{
			if(IsOverlayShowing(view)){
				view._Hide();
			}
		}

		public void CloseAllOverlays()
		{
			AbstractView[] e = showingOverlays;
			foreach(AbstractView view in e){
				view._Hide();
			}	
		}

		public void Unload<T>() where T : AbstractView
		{
			Unload(typeof(T));
		}

		public void Unload(System.Type view)
		{
			if(IsViewLoaded(view)){
				if(_debug) Debug.LogFormat("[ViewController] Unload View: {0}",view.Name);

				_assetLookup[view].Unload();
			}
		}

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
				//Resources.UnloadAsset(resource);
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