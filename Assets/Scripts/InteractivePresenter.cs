using System;
using InteractiveFloor;
using InteractiveFloor.Detect;
using VContainer.Unity;

public class InteractivePresenter : ITickable ,IStartable ,IDisposable
{
    private readonly InteractiveModel _model;
    private readonly InteractiveView _view;
    private readonly EntityManager _entityManager;
    private readonly SensorObjectManager _sensorObjectManager;
    private readonly DetectViewerManager _detectViewerManager;

    public InteractivePresenter(InteractiveModel model, EntityManager entityManager, SensorObjectManager sensorObjectManager, DetectViewerManager detectViewerManager, InteractiveView view)
    {
        _model = model;
        _entityManager = entityManager;
        _view = view;
        _sensorObjectManager = sensorObjectManager;
        _detectViewerManager = detectViewerManager;
    }

    public void Start()
    {
        
    }

    public void Tick()
    {
        var viewers = _detectViewerManager.GetViewer(_sensorObjectManager.SensorList);
        _view.SyncEntities(viewers);
    }
    
    public void Dispose()
    {
        
    }
}
