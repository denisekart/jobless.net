using Microsoft.Extensions.Hosting;

namespace Jobless;

public static class DependencyInjectionExtensions
{
    public static T AddJobless<T>(this T builder, Action<JoblessRuntimeBuilder>? configure = null) where T : IHostApplicationBuilder
    {
        var joblessBuilder = new JoblessRuntimeBuilder(builder);
        configure?.Invoke(joblessBuilder);
        joblessBuilder.Build();
        return builder;
    }
}
