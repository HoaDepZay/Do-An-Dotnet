using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace QldtSdh.Data
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataServices(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<QldtSdhDbContext>(options =>
                options.UseSqlServer(connectionString));
            return services;
        }
    }
}
