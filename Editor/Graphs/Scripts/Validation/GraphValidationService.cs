using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Validation
{
    /// <summary>
    /// Service for validating quest graphs and reporting issues.
    /// Split into partial classes by graph type for better organization.
    /// </summary>
    public partial class GraphValidationService
    {
        #region Helper Methods

        private void ValidateStartNode<TStartNode>(
            List<INode> nodes,
            string nodeTypeName,
            List<ValidationResult> results,
            Graph graph) where TStartNode : INode
        {
            var startNodes = nodes.OfType<TStartNode>().ToList();

            if (startNodes.Count == 0)
            {
                results.Add(ValidationResult.Error(
                    $"Graph must have a {nodeTypeName}",
                    graph: graph));
            }
            else if (startNodes.Count > 1)
            {
                foreach (var node in startNodes.Skip(1))
                {
                    results.Add(ValidationResult.Error(
                        $"Only one {nodeTypeName} is allowed",
                        node, graph));
                }
            }
        }

        private bool HasOutputConnection(INode node, string portName)
        {
            try
            {
                var port = node.GetOutputPortByName(portName);
                return port != null && port.isConnected;
            }
            catch
            {
                return false;
            }
        }

        private bool HasInputConnection(INode node, string portName)
        {
            try
            {
                var port = node.GetInputPortByName(portName);
                return port != null && port.isConnected;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Returns true if the graph has no errors (warnings are OK).
        /// </summary>
        public bool IsValid(List<ValidationResult> results)
        {
            return !results.Any(r => r.Severity == ValidationSeverity.Error);
        }

        /// <summary>
        /// Gets only error results.
        /// </summary>
        public List<ValidationResult> GetErrors(List<ValidationResult> results)
        {
            return results.Where(r => r.Severity == ValidationSeverity.Error).ToList();
        }

        /// <summary>
        /// Gets only warning results.
        /// </summary>
        public List<ValidationResult> GetWarnings(List<ValidationResult> results)
        {
            return results.Where(r => r.Severity == ValidationSeverity.Warning).ToList();
        }

        #endregion
    }
}
