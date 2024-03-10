using System.Reflection;
using Injection.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Injection;

public static class Container
{
    private const string Predicate = "ConceptualSystem";

    public static void RegisterAllDependency<T>(this IServiceCollection services, Assembly mainAssembly)
    {
        var commonType = typeof(T);

        var allAssemblies = GetAssemblies(mainAssembly).ToList();

        foreach (var assembly in allAssemblies)
        {
            var types = assembly.GetExportedTypes().Where(x => x is { IsClass: true, IsAbstract: false } && commonType.IsAssignableFrom(x)).ToList();

            foreach (var type in types)
            {
                var typeInterface = type.GetInterfaces().FirstOrDefault(x => commonType.IsAssignableFrom(x));

                if (typeInterface is not { IsInterface: true })
                {
                    throw new ApplicationException("Interface doesn't exist");
                }

                if (typeInterface.ContainsGenericParameters)
                {
                    var genericInterface = typeInterface.GetGenericTypeDefinition();
                    var genericType = type.GetGenericTypeDefinition();
                    services.Add(new ServiceDescriptor(genericInterface, genericType, GetLifetime(genericType)));
                }
                else
                {
                    services.Add(new ServiceDescriptor(typeInterface, type, GetLifetime(type)));
                }
            }
        }
    }

    private static IEnumerable<Assembly> GetAssemblies(Assembly mainAsm)
    {
        var allLoaded = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var loaded = new List<Assembly> { mainAsm };
        var other = allLoaded.Where(x => x.FullName.StartsWith(Predicate) && !loaded.Contains(x));
        loaded.AddRange(other);

        var stack = new Stack<Assembly>(new[] { mainAsm });

        do
        {
            var asm = stack.Pop();

            foreach (var reference in asm.GetReferencedAssemblies().Where(a => a.FullName.StartsWith(Predicate)))
            {
                var assembly = loaded.FirstOrDefault(x => x.GetName().FullName.Equals(reference.FullName));

                if (assembly == null)
                {
                    assembly = AppDomain.CurrentDomain.Load(reference);
                }

                if (assembly != null)
                {
                    stack.Push(assembly);

                    if (!loaded.Exists(x => x.Equals(assembly)))
                    {
                        loaded.Add(assembly);
                    }
                }
            }
        } while (stack.Count > 0);

        return loaded;
    }

    private static ServiceLifetime GetLifetime(Type type)
    {
        if (typeof(ISingleton).IsAssignableFrom(type))
        {
            return ServiceLifetime.Singleton;
        }

        if (typeof(ITransient).IsAssignableFrom(type))
        {
            return ServiceLifetime.Transient;
        }

        if (typeof(IScoped).IsAssignableFrom(type))
        {
            return ServiceLifetime.Scoped;
        }

        throw new ApplicationException("Wrong dependency");
    }
}
