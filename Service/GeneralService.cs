using PCL.Core.LifecycleManagement;

namespace PCL.Core.Service;

/// <summary>
/// General service class for constructing <see cref="ILifecycleService"/> in a more convenient way.
/// </summary>
public abstract class GeneralService : ILifecycleService
{
    public string Identifier { get; }
    public string Name { get; }
    public bool SupportAsyncStart { get; }

    /// <summary>
    /// The context of the service instance,
    /// used for declaration, logging, lifecycle operation, etc.
    /// </summary>
    protected LifecycleContext ServiceContext => Lifecycle.GetContext(this);
    
    /// <summary>
    /// Initialize a general service instance.
    /// This constructor should only be extended rather than invoked directly.
    /// </summary>
    /// <param name="identifier">see <see cref="Identifier"/></param>
    /// <param name="name">see <see cref="Name"/></param>
    /// <param name="asyncStart">see <see cref="SupportAsyncStart"/></param>
    protected GeneralService(string identifier, string name, bool asyncStart = true)
    {
        Identifier = identifier;
        Name = name;
        SupportAsyncStart = asyncStart;
    }
    
    /// <summary>
    /// Start the service, will be invoked on the specified state
    /// from <see cref="LifecycleService"/> attribute.
    /// </summary>
    public virtual void Start() { }

    /// <summary>
    /// Stop the service, will be invoked while program exiting,
    /// or if <see cref="Start"/> throws an exception.
    /// </summary>
    public virtual void Stop() { }
}
