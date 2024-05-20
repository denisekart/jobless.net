using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Jobless.UnitTests
{
    public class JoblessRuntimeBuilderTests
    {
        private readonly IHostApplicationBuilder _hostBuilder;
        private readonly IServiceCollection _services;

        public JoblessRuntimeBuilderTests()
        {
            _hostBuilder = Substitute.For<IHostApplicationBuilder>();
            _services = Substitute.For<IServiceCollection>();
            _hostBuilder.Services.Returns(_services);
        }

        [Fact]
        public void WithJobsExecutionStrategy_AddsValidService()
        {
            var builder = new JoblessRuntimeBuilder(_hostBuilder);
            builder.WithJobsExecutionStrategy<DefaultJobsExecutionStrategy>();

            _services.Received(1)
                .Add(Arg.Is<ServiceDescriptor>(sd => sd.ServiceType == typeof(IJobsExecutionStrategy) &&
                    sd.ImplementationType == typeof(DefaultJobsExecutionStrategy)));
        }

        [Fact]
        public void WithJobsExecutionStrategy_ReturnsSameInstance()
        {
            var builder = new JoblessRuntimeBuilder(_hostBuilder);
            var returnedBuilder = builder.WithJobsExecutionStrategy<DefaultJobsExecutionStrategy>();

            Assert.Same(builder, returnedBuilder);
        }

        [Fact]
        public void WithJobExecutor_AddsValidService()
        {
            var builder = new JoblessRuntimeBuilder(_hostBuilder);
            builder.WithJobExecutor<DefaultJobExecutor>();

            _services.Received(1)
                .Add(Arg.Is<ServiceDescriptor>(sd => sd.ServiceType == typeof(IJobExecutor) &&
                    sd.KeyedImplementationType == typeof(DefaultJobExecutor)
                    && sd.ServiceKey == IJobDefinition.DefaultJobCategory));
        }

        [Fact]
        public void WithJobPriorityComparer_AddsValidService_WhenCategoryIsNull()
        {
            var builder = new JoblessRuntimeBuilder(_hostBuilder);
            var comparer = new SequentialPrioritizedJobDefinitionComparer();
            builder.WithJobPriorityComparer(comparer);

            _services.Received(1)
                .Add(Arg.Is<ServiceDescriptor>(sd => sd.ServiceType == typeof(IComparer<IJobDefinition>) &&
                    sd.ImplementationInstance == comparer));
        }

        [Fact]
        public void WithJobPriorityComparer_AddsValidService_WhenCategoryIsNotNull()
        {
            var builder = new JoblessRuntimeBuilder(_hostBuilder);
            var comparer = new SequentialPrioritizedJobDefinitionComparer();
            const string category = "category";
            builder.WithJobPriorityComparer(comparer, category);

            _services.Received(1)
                .Add(Arg.Is<ServiceDescriptor>(sd => sd.ServiceType == typeof(IComparer<IJobDefinition>) &&
                    sd.KeyedImplementationInstance == comparer
                    && sd.ServiceKey == category));
        }
    }

    public class DefaultJobExecutor : IJobExecutor
    {
        public Task Execute(IJobDefinition jobDefinition, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
