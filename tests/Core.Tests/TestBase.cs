using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ListaCompras.Core.Services;
using Xunit;

namespace ListaCompras.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly Mock<ICurrentUserProvider> CurrentUserProvider;

        protected TestBase()
        {
            var services = new ServiceCollection();

            // Mock do CurrentUserProvider
            CurrentUserProvider = new Mock<ICurrentUserProvider>();
            CurrentUserProvider.Setup(x => x.GetCurrentUserId()).Returns(1);
            services.AddSingleton(CurrentUserProvider.Object);

            // Logging
            services.AddLogging(configure => configure.AddDebug());

            // Adiciona serviços
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Serviços base
            services.AddScoped<IItemService, ItemService>();
            services.AddScoped<IListaService, ListaService>();
            services.AddScoped<ICategoriaService, CategoriaService>();
            services.AddScoped<IPrecoService, PrecoService>();
        }

        protected T GetService<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        public virtual void Dispose()
        {
            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }
    }
}