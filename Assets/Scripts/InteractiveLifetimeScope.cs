using UnityEngine;
using VContainer;
using VContainer.Unity;
using InteractiveFloor;
using InteractiveFloor.Detect;

public class InteractiveLifetimeScope : LifetimeScope
{
    [SerializeField] private EntityManager _entityManager;
    [SerializeField] private SensorObjectManager _sensorObjectManager;
    [SerializeField] private InteractiveView _interactiveView;
    [SerializeField] private ManuChanFlockController _flockController;
    [SerializeField] private LightManager _lightManager;
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(_interactiveView);
        builder.RegisterComponent(_entityManager);
        builder.RegisterComponent(_sensorObjectManager);
        builder.RegisterComponent(_flockController);
        builder.RegisterComponent(_lightManager);
        builder.Register<InteractiveModel>(Lifetime.Scoped);
        builder.RegisterEntryPoint<InteractivePresenter>(Lifetime.Scoped);
        builder.Register<DetectViewerManager>(Lifetime.Scoped);
    }
}
