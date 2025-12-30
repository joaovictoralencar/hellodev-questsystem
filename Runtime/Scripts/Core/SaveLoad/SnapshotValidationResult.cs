using System.Collections.Generic;

namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// Severity level for validation issues.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>Informational - no action required.</summary>
        Info,
        /// <summary>Warning - save may have minor issues but will load.</summary>
        Warning,
        /// <summary>Error - save will partially fail to load.</summary>
        Error,
        /// <summary>Critical - save cannot be loaded at all.</summary>
        Critical
    }

    /// <summary>
    /// A single validation issue found in a snapshot.
    /// </summary>
    public class ValidationIssue
    {
        /// <summary>Gets the severity of this issue.</summary>
        public ValidationSeverity Severity { get; }

        /// <summary>Gets the category of this issue (Quest, Task, WorldFlag, etc.).</summary>
        public string Category { get; }

        /// <summary>Gets the human-readable description of the issue.</summary>
        public string Message { get; }

        /// <summary>Gets the identifier of the affected item (GUID or name).</summary>
        public string AffectedItem { get; }

        public ValidationIssue(ValidationSeverity severity, string category, string message, string affectedItem = null)
        {
            Severity = severity;
            Category = category;
            Message = message;
            AffectedItem = affectedItem;
        }

        public override string ToString()
        {
            var item = string.IsNullOrEmpty(AffectedItem) ? "" : $" [{AffectedItem}]";
            return $"[{Severity}] {Category}{item}: {Message}";
        }
    }

    /// <summary>
    /// Result of validating a QuestSystemSnapshot before restoration.
    /// Use this to check if a save file is compatible with the current game version.
    /// </summary>
    public class SnapshotValidationResult
    {
        private readonly List<ValidationIssue> _issues = new();

        /// <summary>
        /// Gets all validation issues found.
        /// </summary>
        public IReadOnlyList<ValidationIssue> Issues => _issues;

        /// <summary>
        /// Gets whether the snapshot is valid (no errors or critical issues).
        /// </summary>
        public bool IsValid => !HasErrors && !HasCritical;

        /// <summary>
        /// Gets whether there are any critical issues that prevent loading.
        /// </summary>
        public bool HasCritical { get; private set; }

        /// <summary>
        /// Gets whether there are any error-level issues.
        /// </summary>
        public bool HasErrors { get; private set; }

        /// <summary>
        /// Gets whether there are any warnings.
        /// </summary>
        public bool HasWarnings { get; private set; }

        /// <summary>
        /// Gets the total number of issues.
        /// </summary>
        public int IssueCount => _issues.Count;

        /// <summary>
        /// Gets a summary of the validation result.
        /// </summary>
        public string Summary
        {
            get
            {
                if (IssueCount == 0)
                    return "Snapshot is valid.";

                int critical = 0, errors = 0, warnings = 0, info = 0;
                foreach (var issue in _issues)
                {
                    switch (issue.Severity)
                    {
                        case ValidationSeverity.Critical: critical++; break;
                        case ValidationSeverity.Error: errors++; break;
                        case ValidationSeverity.Warning: warnings++; break;
                        case ValidationSeverity.Info: info++; break;
                    }
                }

                var parts = new List<string>();
                if (critical > 0) parts.Add($"{critical} critical");
                if (errors > 0) parts.Add($"{errors} errors");
                if (warnings > 0) parts.Add($"{warnings} warnings");
                if (info > 0) parts.Add($"{info} info");

                return $"Validation found: {string.Join(", ", parts)}";
            }
        }

        /// <summary>
        /// Adds a validation issue.
        /// </summary>
        public void AddIssue(ValidationSeverity severity, string category, string message, string affectedItem = null)
        {
            _issues.Add(new ValidationIssue(severity, category, message, affectedItem));

            switch (severity)
            {
                case ValidationSeverity.Critical:
                    HasCritical = true;
                    break;
                case ValidationSeverity.Error:
                    HasErrors = true;
                    break;
                case ValidationSeverity.Warning:
                    HasWarnings = true;
                    break;
            }
        }

        /// <summary>
        /// Adds a critical issue.
        /// </summary>
        public void AddCritical(string category, string message, string affectedItem = null)
            => AddIssue(ValidationSeverity.Critical, category, message, affectedItem);

        /// <summary>
        /// Adds an error issue.
        /// </summary>
        public void AddError(string category, string message, string affectedItem = null)
            => AddIssue(ValidationSeverity.Error, category, message, affectedItem);

        /// <summary>
        /// Adds a warning issue.
        /// </summary>
        public void AddWarning(string category, string message, string affectedItem = null)
            => AddIssue(ValidationSeverity.Warning, category, message, affectedItem);

        /// <summary>
        /// Adds an info issue.
        /// </summary>
        public void AddInfo(string category, string message, string affectedItem = null)
            => AddIssue(ValidationSeverity.Info, category, message, affectedItem);

        /// <summary>
        /// Gets all issues of a specific severity.
        /// </summary>
        public IEnumerable<ValidationIssue> GetIssuesBySeverity(ValidationSeverity severity)
        {
            foreach (var issue in _issues)
            {
                if (issue.Severity == severity)
                    yield return issue;
            }
        }

        /// <summary>
        /// Logs all issues to the Unity console.
        /// </summary>
        public void LogToConsole()
        {
            if (IssueCount == 0)
            {
                UnityEngine.Debug.Log("[SnapshotValidation] Snapshot is valid.");
                return;
            }

            UnityEngine.Debug.Log($"[SnapshotValidation] {Summary}");

            foreach (var issue in _issues)
            {
                switch (issue.Severity)
                {
                    case ValidationSeverity.Critical:
                    case ValidationSeverity.Error:
                        UnityEngine.Debug.LogError($"[SnapshotValidation] {issue}");
                        break;
                    case ValidationSeverity.Warning:
                        UnityEngine.Debug.LogWarning($"[SnapshotValidation] {issue}");
                        break;
                    default:
                        UnityEngine.Debug.Log($"[SnapshotValidation] {issue}");
                        break;
                }
            }
        }
    }
}
