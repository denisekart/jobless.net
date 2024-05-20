using Microsoft.Extensions.Hosting;

namespace Jobless;

public static class RuntimeExtensions
{
    public static T UseJobless<T>(this T builder) where T : IHost
    {
        return builder;
    }
}
