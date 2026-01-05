using System;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Validation
{
    /// <summary>
    /// Severity level for validation results.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>Critical issue that prevents export.</summary>
        Error,
        /// <summary>Issue that should be addressed but allows export.</summary>
        Warning,
        /// <summary>Informational message.</summary>
        Info
    }

    /// <summary>
    /// Represents a single validation result for a graph or node.
    /// </summary>
    [Serializable]
    public class ValidationResult
    {
        /// <summary>
        /// The severity of this validation result.
        /// </summary>
        public ValidationSeverity Severity { get; }

        /// <summary>
        /// Human-readable message describing the issue.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The node related to this issue, if any.
        /// Can be used to navigate to the problem in the graph editor.
        /// </summary>
        public INode RelatedNode { get; }

        /// <summary>
        /// Path or identifier for the node (for display purposes).
        /// </summary>
        public string NodePath { get; }

        /// <summary>
        /// The graph containing this issue.
        /// </summary>
        public Graph RelatedGraph { get; }

        /// <summary>
        /// Creates a new validation result.
        /// </summary>
        public ValidationResult(
            ValidationSeverity severity,
            string message,
            INode relatedNode = null,
            Graph relatedGraph = null,
            string nodePath = null)
        {
            Severity = severity;
            Message = message;
            RelatedNode = relatedNode;
            RelatedGraph = relatedGraph;
            NodePath = nodePath ?? relatedNode?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Creates an error result.
        /// </summary>
        public static ValidationResult Error(string message, INode node = null, Graph graph = null)
            => new ValidationResult(ValidationSeverity.Error, message, node, graph);

        /// <summary>
        /// Creates a warning result.
        /// </summary>
        public static ValidationResult Warning(string message, INode node = null, Graph graph = null)
            => new ValidationResult(ValidationSeverity.Warning, message, node, graph);

        /// <summary>
        /// Creates an info result.
        /// </summary>
        public static ValidationResult Info(string message, INode node = null, Graph graph = null)
            => new ValidationResult(ValidationSeverity.Info, message, node, graph);

        public override string ToString()
        {
            string prefix = Severity switch
            {
                ValidationSeverity.Error => "[ERROR]",
                ValidationSeverity.Warning => "[WARN]",
                ValidationSeverity.Info => "[INFO]",
                _ => "[???]"
            };

            if (!string.IsNullOrEmpty(NodePath))
                return $"{prefix} {Message} (at {NodePath})";

            return $"{prefix} {Message}";
        }
    }
}
