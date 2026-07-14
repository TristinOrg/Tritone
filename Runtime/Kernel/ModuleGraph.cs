using System;
using System.Collections.Generic;

namespace Tritone.Kernel
{
    /// <summary>
    /// Validates module dependencies and produces a deterministic startup order.
    /// </summary>
    internal static class ModuleGraph
    {
        /// <summary>
        /// Sorts module registrations so every dependency appears before its consumer.
        /// </summary>
        /// <param name="registrations">The module registrations in declaration order.</param>
        /// <returns>The registrations in dependency-safe startup order.</returns>
        internal static ModuleRegistration[] Sort(IReadOnlyList<ModuleRegistration> registrations)
        {
            var registrationsByType = new Dictionary<Type, ModuleRegistration>(registrations.Count);
            for (var i = 0; i < registrations.Count; i++)
            {
                var registration = registrations[i];
                if (registrationsByType.ContainsKey(registration.ModuleType))
                    throw new InvalidOperationException($"Module '{registration.ModuleType.FullName}' is already registered.");

                registrationsByType.Add(registration.ModuleType, registration);
            }

            var states = new Dictionary<Type, byte>(registrations.Count);
            var result = new List<ModuleRegistration>(registrations.Count);
            var path   = new List<Type>(registrations.Count);
            for (var i = 0; i < registrations.Count; i++)
                Visit(registrations[i], registrationsByType, states, result, path);

            return result.ToArray();
        }

        /// <summary>
        /// Performs one depth-first traversal step for topological sorting.
        /// </summary>
        /// <param name="current">The registration currently being visited.</param>
        /// <param name="registrationsByType">All registrations indexed by concrete module type.</param>
        /// <param name="states">The traversal state of each module type.</param>
        /// <param name="result">The dependency-safe output collected so far.</param>
        /// <param name="path">The active traversal path used to report dependency cycles.</param>
        private static void Visit(ModuleRegistration current,
                                  Dictionary<Type, ModuleRegistration> registrationsByType,
                                  Dictionary<Type, byte> states,
                                  List<ModuleRegistration> result,
                                  List<Type> path)
        {
            if (states.TryGetValue(current.ModuleType, out var state))
            {
                if (state == 2)
                    return;
                if (state == 1)
                {
                    var cycleStart = path.IndexOf(current.ModuleType);
                    var cycle      = new List<string>();
                    for (var i = cycleStart; i < path.Count; i++)
                        cycle.Add(path[i].FullName);
                    cycle.Add(current.ModuleType.FullName);

                    throw new InvalidOperationException($"Circular module dependency detected: {string.Join(" -> ", cycle)}.");
                }
            }

            states[current.ModuleType] = 1;
            path.Add(current.ModuleType);
            for (var i = 0; i < current.Dependencies.Length; i++)
            {
                var dependency = current.Dependencies[i];
                if (!registrationsByType.TryGetValue(dependency, out var dependencyRegistration))
                    throw new InvalidOperationException($"Module '{current.ModuleType.FullName}' requires missing module '{dependency.FullName}'.");

                Visit(dependencyRegistration, registrationsByType, states, result, path);
            }

            path.RemoveAt(path.Count - 1);
            states[current.ModuleType] = 2;
            result.Add(current);
        }
    }
}
