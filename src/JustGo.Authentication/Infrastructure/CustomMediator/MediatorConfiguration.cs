using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.CustomMediator
{
    /// <summary>
    /// Configuration for custom mediator
    /// </summary>
    public class MediatorConfiguration
    {
        internal List<Assembly> Assemblies { get; } = new();
        internal List<Type> OpenBehaviors { get; } = new();
        internal List<Type> ClosedBehaviors { get; } = new();

        /// <summary>
        /// Register services from assembly
        /// </summary>
        /// <param name="assembly">Assembly to scan</param>
        public void RegisterServicesFromAssembly(Assembly assembly)
        {
            Assemblies.Add(assembly);
        }

        /// <summary>
        /// Add open generic behavior
        /// </summary>
        /// <param name="behaviorType">Open generic behavior type</param>
        public void AddOpenBehavior(Type behaviorType)
        {
            if (!behaviorType.IsGenericTypeDefinition)
                throw new ArgumentException("Behavior type must be an open generic type", nameof(behaviorType));

            OpenBehaviors.Add(behaviorType);
        }

        /// <summary>
        /// Add closed behavior
        /// </summary>
        /// <param name="behaviorType">Closed behavior type</param>
        public void AddBehavior(Type behaviorType)
        {
            ClosedBehaviors.Add(behaviorType);
        }

        /// <summary>
        /// Add closed behavior
        /// </summary>
        /// <typeparam name="TBehavior">Behavior type</typeparam>
        public void AddBehavior<TBehavior>()
            where TBehavior : class
        {
            AddBehavior(typeof(TBehavior));
        }
    }
}
