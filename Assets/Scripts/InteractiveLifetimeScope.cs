using UnityEngine;
using VContainer;
using VContainer.Unity;

public class InteractiveLifetimeScope : LifetimeScope
{
    [SerializeField] private InteractiveView _view;
    [SerializeField] private EntityManager _entityManager;
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(_view);
        builder.RegisterComponent(_entityManager);
        builder.Register<InteractiveModel>(Lifetime.Scoped);
        builder.Register<InteractivePresenter>(Lifetime.Scoped);
    }
}