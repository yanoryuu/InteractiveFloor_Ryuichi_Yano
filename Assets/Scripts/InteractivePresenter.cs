using System;
using InteractiveFloor;
using InteractiveFloor.Detect;
using VContainer.Unity;

public class InteractivePresenter : ITickable, IStartable, IDisposable
{
    private readonly InteractiveModel _model;
    private readonly InteractiveView _view;
    private readonly EntityManager _entityManager;
    private readonly ManuChanFlockController _flockController;
    private readonly SensorObjectManager _sensorObjectManager;
    private readonly DetectViewerManager _detectViewerManager;
    private readonly LightManager _lightManager;

    public InteractivePresenter(
        InteractiveModel model,
        EntityManager entityManager,
        SensorObjectManager sensorObjectManager,
        DetectViewerManager detectViewerManager,
        InteractiveView view,
        ManuChanFlockController flockController,
        LightManager lightManager)
    {
        _model = model;
        _entityManager = entityManager;
        _view = view;
        _flockController = flockController;
        _sensorObjectManager = sensorObjectManager;
        _detectViewerManager = detectViewerManager;
        _lightManager = lightManager;
    }

    public void Start()
    {
        _lightManager.StartRotate();
    }

    public void Tick()
    {
        var viewers = _detectViewerManager.GetViewer(_sensorObjectManager.SensorList);
        _view.SyncViewers(viewers);
        _flockController.UpdateFlock(_view.ViewerPositions);
        _entityManager.ProcessContacts();
    }

    public void Dispose() { }
}
