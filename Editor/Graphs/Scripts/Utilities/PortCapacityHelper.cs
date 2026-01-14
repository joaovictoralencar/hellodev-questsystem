using System;
using System.Reflection;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Utilities
{
    /// <summary>
    /// Helper class to set port capacity using reflection.
    /// The Unity Graph Toolkit marks PortCapacity as internal, but we need to override
    /// the default behavior for input ports using custom flow types like StageFlow.
    /// </summary>
    public static class PortCapacityHelper
    {
        private static PropertyInfo s_CapacityProperty;
        private static Type s_PortCapacityEnum;
        private static object s_MultiValue;
        private static object s_SingleValue;
        private static bool s_Initialized;
        private static bool s_InitFailed;

        /// <summary>
        /// Initializes reflection cache for port capacity manipulation.
        /// </summary>
        private static void Initialize()
        {
            if (s_Initialized || s_InitFailed)
                return;

            try
            {
                // Find the internal PortModel type
                var graphToolkitAssembly = typeof(IPort).Assembly;
                var portModelType = graphToolkitAssembly.GetType("Unity.GraphToolkit.Editor.PortModel");

                if (portModelType == null)
                {
                    // Try internal editor assembly
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (asm.GetName().Name == "Unity.GraphToolkit.Internal.Editor")
                        {
                            portModelType = asm.GetType("Unity.GraphToolkit.Editor.PortModel");
                            if (portModelType != null)
                                break;
                        }
                    }
                }

                if (portModelType == null)
                {
                    Debug.LogWarning("[PortCapacityHelper] Could not find PortModel type");
                    s_InitFailed = true;
                    return;
                }

                // Get the Capacity property
                s_CapacityProperty = portModelType.GetProperty("Capacity",
                    BindingFlags.Public | BindingFlags.Instance);

                if (s_CapacityProperty == null)
                {
                    Debug.LogWarning("[PortCapacityHelper] Could not find Capacity property");
                    s_InitFailed = true;
                    return;
                }

                // Get the PortCapacity enum type and values
                s_PortCapacityEnum = s_CapacityProperty.PropertyType;
                s_MultiValue = Enum.Parse(s_PortCapacityEnum, "Multi");
                s_SingleValue = Enum.Parse(s_PortCapacityEnum, "Single");

                s_Initialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PortCapacityHelper] Initialization failed: {ex.Message}");
                s_InitFailed = true;
            }
        }

        /// <summary>
        /// Sets the port to accept multiple connections.
        /// </summary>
        /// <param name="port">The port to modify.</param>
        /// <returns>The same port for chaining.</returns>
        public static IPort SetMultiCapacity(this IPort port)
        {
            Initialize();

            if (s_InitFailed || port == null)
                return port;

            try
            {
                s_CapacityProperty?.SetValue(port, s_MultiValue);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PortCapacityHelper] Failed to set multi capacity: {ex.Message}");
            }

            return port;
        }

        /// <summary>
        /// Sets the port to accept only a single connection.
        /// </summary>
        /// <param name="port">The port to modify.</param>
        /// <returns>The same port for chaining.</returns>
        public static IPort SetSingleCapacity(this IPort port)
        {
            Initialize();

            if (s_InitFailed || port == null)
                return port;

            try
            {
                s_CapacityProperty?.SetValue(port, s_SingleValue);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PortCapacityHelper] Failed to set single capacity: {ex.Message}");
            }

            return port;
        }
    }
}
