namespace PCL.Core.App.Configuration.Implementations;

public class AsyncTrafficCenter : CommonTrafficCenter
{
    protected override void OnRequest<TInput, TOutput>(PreviewTrafficEventArgs<TInput, TOutput> e)
    {
        throw new System.NotImplementedException();
    }
}
