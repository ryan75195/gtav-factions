namespace FactionWars.ScriptHookV
{
    public interface IServiceContainer
    {
        TService Resolve<TService>() where TService : class;
        bool TryResolve<TService>(out TService? service) where TService : class;
        bool IsRegistered<TService>() where TService : class;
    }
}
