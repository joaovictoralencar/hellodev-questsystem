using System;
using System.Collections;
using System.Collections.Generic;
using HelloDev.Conditions;
using UnityEngine;

namespace HelloDev.QuestSystem
{
    /// <summary>
    /// Wrapper for List&lt;Condition_SO&gt; to enable CustomPropertyDrawer support
    /// in Graph Toolkit node options.
    /// </summary>
    /// <remarks>
    /// Graph Toolkit's internal types require CustomPropertyDrawer for fields to appear
    /// in node inspectors. Unity doesn't provide a PropertyDrawer for generic List&lt;T&gt;,
    /// so this wrapper enables custom drawing via ConditionListDrawer.
    /// </remarks>
    [Serializable]
    public class ConditionList : IEnumerable<Condition_SO>
    {
        [SerializeField]
        private List<Condition_SO> _conditions = new();

        /// <summary>
        /// Gets or sets the underlying conditions list.
        /// </summary>
        public List<Condition_SO> Conditions
        {
            get => _conditions;
            set => _conditions = value ?? new List<Condition_SO>();
        }

        /// <summary>
        /// Gets the number of conditions in the list.
        /// </summary>
        public int Count => _conditions.Count;

        /// <summary>
        /// Gets or sets the condition at the specified index.
        /// </summary>
        public Condition_SO this[int index]
        {
            get => _conditions[index];
            set => _conditions[index] = value;
        }

        /// <summary>
        /// Adds a condition to the list.
        /// </summary>
        public void Add(Condition_SO condition) => _conditions.Add(condition);

        /// <summary>
        /// Removes the condition at the specified index.
        /// </summary>
        public void RemoveAt(int index) => _conditions.RemoveAt(index);

        /// <summary>
        /// Removes all conditions from the list.
        /// </summary>
        public void Clear() => _conditions.Clear();

        /// <summary>
        /// Returns an enumerator that iterates through the conditions.
        /// </summary>
        public IEnumerator<Condition_SO> GetEnumerator() => _conditions.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Implicit conversion to List&lt;Condition_SO&gt; for convenience.
        /// </summary>
        public static implicit operator List<Condition_SO>(ConditionList wrapper)
            => wrapper?._conditions ?? new List<Condition_SO>();

        /// <summary>
        /// Implicit conversion from List&lt;Condition_SO&gt; for convenience.
        /// </summary>
        public static implicit operator ConditionList(List<Condition_SO> list)
            => new ConditionList { _conditions = list ?? new List<Condition_SO>() };
    }
}
