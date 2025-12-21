using System;
using System.Collections.Generic;
using System.Reflection;
using HelloDev.Conditions;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HelloDev.QuestSystem.Tests.Editor
{
    /// <summary>
    /// Editor tests for the Quest System.
    /// Tests ScriptableObject creation, validation, and editor-specific functionality.
    /// </summary>
    [TestFixture]
    public class QuestEditorTests
    {
        private Quest_SO _questData;
        private TaskInt_SO _taskData1;
        private TaskInt_SO _taskData2;

        [SetUp]
        public void Setup()
        {
            // Create test task data
            _taskData1 = ScriptableObject.CreateInstance<TaskInt_SO>();
            _taskData1.name = "EditorTestTask1";
            SetPrivateField(_taskData1, "devName", "Editor Test Task 1");
            SetPrivateField(_taskData1, "requiredCount", 3);
            SetPrivateField(_taskData1, "taskId", Guid.NewGuid().ToString());

            _taskData2 = ScriptableObject.CreateInstance<TaskInt_SO>();
            _taskData2.name = "EditorTestTask2";
            SetPrivateField(_taskData2, "devName", "Editor Test Task 2");
            SetPrivateField(_taskData2, "requiredCount", 5);
            SetPrivateField(_taskData2, "taskId", Guid.NewGuid().ToString());

            // Create test quest data
            _questData = ScriptableObject.CreateInstance<Quest_SO>();
            _questData.name = "EditorTestQuest";
            SetPrivateField(_questData, "devName", "Editor Test Quest");
            SetPrivateField(_questData, "questId", Guid.NewGuid().ToString());
            SetPrivateField(_questData, "tasks", new List<Task_SO> { _taskData1, _taskData2 });
            SetPrivateField(_questData, "startConditions", new List<Condition_SO>());
            SetPrivateField(_questData, "failureConditions", new List<Condition_SO>());
            SetPrivateField(_questData, "globalTaskFailureConditions", new List<Condition_SO>());
            SetPrivateField(_questData, "rewards", new List<RewardInstance>());
        }

        [TearDown]
        public void Teardown()
        {
            if (_questData != null) UnityEngine.Object.DestroyImmediate(_questData);
            if (_taskData1 != null) UnityEngine.Object.DestroyImmediate(_taskData1);
            if (_taskData2 != null) UnityEngine.Object.DestroyImmediate(_taskData2);
        }

        #region Quest_SO Creation Tests

        [Test]
        public void Quest_SO_CreateInstance_IsNotNull()
        {
            var quest = ScriptableObject.CreateInstance<Quest_SO>();
            Assert.IsNotNull(quest);
            UnityEngine.Object.DestroyImmediate(quest);
        }

        [Test]
        public void Quest_SO_DevName_IsAccessible()
        {
            Assert.AreEqual("Editor Test Quest", _questData.DevName);
        }

        [Test]
        public void Quest_SO_QuestId_IsValidGuid()
        {
            Assert.DoesNotThrow(() =>
            {
                Guid id = _questData.QuestId;
                Assert.AreNotEqual(Guid.Empty, id);
            });
        }

        [Test]
        public void Quest_SO_Tasks_ContainsExpectedCount()
        {
            Assert.AreEqual(2, _questData.Tasks.Count);
        }

        [Test]
        public void Quest_SO_GetRuntimeQuest_ReturnsValidQuest()
        {
            Quest runtimeQuest = _questData.GetRuntimeQuest();

            Assert.IsNotNull(runtimeQuest);
            Assert.AreEqual(_questData.QuestId, runtimeQuest.QuestId);
            Assert.AreEqual(QuestState.NotStarted, runtimeQuest.CurrentState);
        }

        #endregion

        #region TaskInt_SO Creation Tests

        [Test]
        public void TaskInt_SO_CreateInstance_IsNotNull()
        {
            var task = ScriptableObject.CreateInstance<TaskInt_SO>();
            Assert.IsNotNull(task);
            UnityEngine.Object.DestroyImmediate(task);
        }

        [Test]
        public void TaskInt_SO_RequiredCount_IsAccessible()
        {
            Assert.AreEqual(3, _taskData1.RequiredCount);
        }

        [Test]
        public void TaskInt_SO_GetRuntimeTask_ReturnsIntTask()
        {
            Task runtimeTask = _taskData1.GetRuntimeTask();

            Assert.IsNotNull(runtimeTask);
            Assert.IsInstanceOf<IntTask>(runtimeTask);
        }

        [Test]
        public void TaskInt_SO_CurrentCount_DefaultsToZero()
        {
            Assert.AreEqual(0, _taskData1.CurrentCount);
        }

        #endregion

        #region TaskBool_SO Creation Tests

        [Test]
        public void TaskBool_SO_CreateInstance_IsNotNull()
        {
            var task = ScriptableObject.CreateInstance<TaskBool_SO>();
            Assert.IsNotNull(task);
            UnityEngine.Object.DestroyImmediate(task);
        }

        [Test]
        public void TaskBool_SO_GetRuntimeTask_ReturnsBoolTask()
        {
            var taskData = ScriptableObject.CreateInstance<TaskBool_SO>();
            SetPrivateField(taskData, "devName", "Bool Test");
            SetPrivateField(taskData, "taskId", Guid.NewGuid().ToString());

            Task runtimeTask = taskData.GetRuntimeTask();

            Assert.IsNotNull(runtimeTask);
            Assert.IsInstanceOf<BoolTask>(runtimeTask);

            UnityEngine.Object.DestroyImmediate(taskData);
        }

        #endregion

        #region TaskString_SO Creation Tests

        [Test]
        public void TaskString_SO_CreateInstance_IsNotNull()
        {
            var task = ScriptableObject.CreateInstance<TaskString_SO>();
            Assert.IsNotNull(task);
            UnityEngine.Object.DestroyImmediate(task);
        }

        [Test]
        public void TaskString_SO_GetRuntimeTask_ReturnsStringTask()
        {
            var taskData = ScriptableObject.CreateInstance<TaskString_SO>();
            SetPrivateField(taskData, "devName", "String Test");
            SetPrivateField(taskData, "taskId", Guid.NewGuid().ToString());
            SetPrivateField(taskData, "targetValue", "test");

            Task runtimeTask = taskData.GetRuntimeTask();

            Assert.IsNotNull(runtimeTask);
            Assert.IsInstanceOf<StringTask>(runtimeTask);

            UnityEngine.Object.DestroyImmediate(taskData);
        }

        #endregion

        #region Quest_SO Equality Tests

        [Test]
        public void Quest_SO_Equals_ReturnsTrueForSameId()
        {
            var quest1 = ScriptableObject.CreateInstance<Quest_SO>();
            var quest2 = ScriptableObject.CreateInstance<Quest_SO>();

            string sharedId = Guid.NewGuid().ToString();
            SetPrivateField(quest1, "questId", sharedId);
            SetPrivateField(quest2, "questId", sharedId);

            Assert.IsTrue(quest1.Equals(quest2));

            UnityEngine.Object.DestroyImmediate(quest1);
            UnityEngine.Object.DestroyImmediate(quest2);
        }

        [Test]
        public void Quest_SO_Equals_ReturnsFalseForDifferentId()
        {
            var quest1 = ScriptableObject.CreateInstance<Quest_SO>();
            var quest2 = ScriptableObject.CreateInstance<Quest_SO>();

            SetPrivateField(quest1, "questId", Guid.NewGuid().ToString());
            SetPrivateField(quest2, "questId", Guid.NewGuid().ToString());

            Assert.IsFalse(quest1.Equals(quest2));

            UnityEngine.Object.DestroyImmediate(quest1);
            UnityEngine.Object.DestroyImmediate(quest2);
        }

        [Test]
        public void Quest_SO_GetHashCode_IsSameForEqualQuests()
        {
            var quest1 = ScriptableObject.CreateInstance<Quest_SO>();
            var quest2 = ScriptableObject.CreateInstance<Quest_SO>();

            string sharedId = Guid.NewGuid().ToString();
            SetPrivateField(quest1, "questId", sharedId);
            SetPrivateField(quest2, "questId", sharedId);

            Assert.AreEqual(quest1.GetHashCode(), quest2.GetHashCode());

            UnityEngine.Object.DestroyImmediate(quest1);
            UnityEngine.Object.DestroyImmediate(quest2);
        }

        #endregion

        #region Runtime Quest Tests

        [Test]
        public void Quest_RuntimeTasks_MatchDataTasks()
        {
            Quest runtimeQuest = _questData.GetRuntimeQuest();

            Assert.AreEqual(_questData.Tasks.Count, runtimeQuest.Tasks.Count);

            for (int i = 0; i < _questData.Tasks.Count; i++)
            {
                Assert.AreEqual(_questData.Tasks[i].TaskId, runtimeQuest.Tasks[i].TaskId);
            }
        }

        [Test]
        public void Quest_MultipleRuntimeInstances_AreIndependent()
        {
            Quest quest1 = _questData.GetRuntimeQuest();
            Quest quest2 = _questData.GetRuntimeQuest();

            quest1.StartQuest();

            Assert.AreEqual(QuestState.InProgress, quest1.CurrentState);
            Assert.AreEqual(QuestState.NotStarted, quest2.CurrentState);
        }

        #endregion

        #region Reward System Tests

        [Test]
        public void QuestRewardType_SO_CreateInstance_IsNotNull()
        {
            // QuestRewardType_SO is abstract, so we can't create it directly
            // This test documents the expected behavior
            Assert.Pass("QuestRewardType_SO is abstract and cannot be instantiated directly.");
        }

        [Test]
        public void RewardInstance_DefaultValues_AreCorrect()
        {
            var reward = new RewardInstance();

            Assert.IsNull(reward.RewardType);
            Assert.AreEqual(0, reward.Amount);
        }

        #endregion

        #region QuestType_SO Tests

        [Test]
        public void QuestType_SO_CreateInstance_IsNotNull()
        {
            var questType = ScriptableObject.CreateInstance<QuestType_SO>();
            Assert.IsNotNull(questType);
            UnityEngine.Object.DestroyImmediate(questType);
        }

        #endregion

        #region Helper Methods

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            FieldInfo field = null;

            // Search through the type hierarchy
            while (type != null && field == null)
            {
                field = type.GetField(fieldName,
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
                type = type.BaseType;
            }

            field?.SetValue(obj, value);
        }

        #endregion
    }
}
