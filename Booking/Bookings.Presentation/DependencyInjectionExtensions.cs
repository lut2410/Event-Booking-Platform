using System.Reflection;

namespace Bookings.Presentation
{
    public static class DependencyInjectionExtensions
    {
        public static void RegisterServicesFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
        {
            var allTypes = assemblies.SelectMany(a => a.GetTypes()).ToList();

            var interfaceTypes = allTypes.Where(t => t.IsInterface && t.Name.StartsWith("I")).ToList();
            var implementationTypes = allTypes.Where(t => t.IsClass && !t.IsAbstract).ToList();

            foreach (var interfaceType in interfaceTypes)
            {
                var implementationType = implementationTypes.FirstOrDefault(impl =>
                    impl.Name == interfaceType.Name.Substring(1) &&
                    interfaceType.IsAssignableFrom(impl));

                if (implementationType != null)
                {
                    services.AddScoped(interfaceType, implementationType);
                }
            }
        }
    }
}
