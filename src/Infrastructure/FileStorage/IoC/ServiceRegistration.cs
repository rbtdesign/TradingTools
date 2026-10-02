using Microsoft.Extensions.DependencyInjection;
using TradingTools.StrategyEvaluation.Application;
namespace TradingTools.Infrastructure.FileStorage.IoC;
public static class ServiceRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterStrategyRunStorage()
        {
            services.AddSingleton<IStrategyRunStore, JsonStrategyRunStore>(); return services;
        }
    }
}
