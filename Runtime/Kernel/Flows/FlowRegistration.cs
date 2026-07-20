using System;

namespace Tritone.Flows
{
    /// <summary>
    /// Stores one explicit flow factory as a compact immutable value.
    /// </summary>
    internal readonly struct FlowRegistration
    {
        /// <summary>
        /// Stores the concrete flow type used as the lookup key.
        /// </summary>
        internal readonly Type FlowType;

        /// <summary>
        /// Stores the factory invoked for each fresh flow instance.
        /// </summary>
        internal readonly Func<IFlow> Factory;

        /// <summary>
        /// Initializes one flow registration.
        /// </summary>
        /// <param name="flowType">The concrete registered flow type.</param>
        /// <param name="factory">The factory invoked for each transition.</param>
        internal FlowRegistration(Type flowType, Func<IFlow> factory)
        {
            FlowType = flowType ?? throw new ArgumentNullException(nameof(flowType));
            Factory  = factory ?? throw new ArgumentNullException(nameof(factory));
        }
    }
}
