using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.Serialization.SystemTextJson;

namespace NotificationsAPI.Function;

public static class Program
{
    public static async Task Main()
    {
        var function = new Function();
        Func<SQSEvent, ILambdaContext, Task<SQSBatchResponse>> handler = function.FunctionHandler;

        await LambdaBootstrapBuilder
            .Create(handler, new DefaultLambdaJsonSerializer())
            .Build()
            .RunAsync();
    }
}
