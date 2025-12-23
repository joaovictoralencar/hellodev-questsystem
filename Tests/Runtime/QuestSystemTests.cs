using System;
using System.Collections.Generic;
using HelloDev.Conditions;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace HelloDev.QuestSystem.Tests.Runtime
{
    /// <summary>
    /// Runtime tests for the Quest System core functionality.
    /// Tests quest creation, task progression, completion, and reward distribution.
    /// </summary>
    public class QuestSystemTests
    {
        private Quest_SO _questData;
        private TaskInt_SO _taskData1;
        private TaskInt_SO _taskData2;

        [SetUp]
        public void Setup()
        {
            // Create test task data
            _taskData1 = ScriptableObject.CreateInstance<TaskInt_SO>();
            _taskData1.name = "TestTask1";
            SetPrivateField(_taskData1, "devName", "Test Task 1");
            SetPrivateField(_taskData1, "requiredCount", 3);
            SetPrivateField(_taskData1, "taskId", Guid.NewGuid().ToString());

            _taskData2 = ScriptableObject.CreateInstance<TaskInt_SO>();
            _taskData2.name = "TestTask2";
            SetPrivateField(_taskData2, "devName", "Test Task 2");
            SetPrivateField(_taskData2, "requiredCount", 5);
            SetPrivateField(_taskData2, "taskId", Guid.NewGuid().ToString());

            // Create test quest data
            _questData = ScriptableObject.CreateInstance<Quest_SO>();
            _questData.name = "TestQuest";
            SetPrivateField(_questData, "devName", "Test Quest");
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

        #region Quest Creation Tests

        [Test]
        public void Quest_Creation_InitializesWithCorrectState()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();

            Assert.IsNotNull(quest);
            Assert.AreEqual(QuestState.NotStarted, quest.CurrentState);
            Assert.AreEqual(_questData.QuestId, quest.QuestId);
            Assert.AreEqual(2, quest.Tasks.Count);
        }

        [Test]
        public void Quest_Creation_TasksAreNotStarted()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();

            foreach (TaskRuntime task in quest.Tasks)
            {
                Assert.AreEqual(TaskState.NotStarted, task.CurrentState);
            }
        }

        #endregion

        #region Quest Lifecycle Tests

        [Test]
        public void Quest_Start_ChangesStateToInProgress()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();

            quest.StartQuest();

            Assert.AreEqual(QuestState.InProgress, quest.CurrentState);
        }

        [Test]
        public void Quest_Start_StartsFirstTask()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();

            quest.StartQuest();

            Assert.AreEqual(TaskState.InProgress, quest.Tasks[0].CurrentState);
            Assert.AreEqual(TaskState.NotStarted, quest.Tasks[1].CurrentState);
        }

        [Test]
        public void Quest_Start_FiresOnQuestStartedEvent()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            bool eventFired = false;
            quest.OnQuestStarted.AddListener(_ => eventFired = true);

            quest.StartQuest();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void Quest_Complete_ChangesStateToCompleted()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();

            // Complete all tasks
            foreach (TaskRuntime task in quest.Tasks)
            {
                task.CompleteTask();
            }

            Assert.AreEqual(QuestState.Completed, quest.CurrentState);
        }

        [Test]
        public void Quest_Fail_ChangesStateToFailed()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();

            quest.FailQuest();

            Assert.AreEqual(QuestState.Failed, quest.CurrentState);
        }

        [Test]
        public void Quest_Fail_FiresOnQuestFailedEvent()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();
            bool eventFired = false;
            quest.OnQuestFailed.AddListener(_ => eventFired = true);

            quest.FailQuest();

            Assert.IsTrue(eventFired);
        }

        #endregion

        #region Task Progression Tests

        [Test]
        public void IntTask_IncrementStep_IncreasesProgress()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();
            IntTaskRuntime task = quest.Tasks[0] as IntTaskRuntime;

            Assert.AreEqual(0, task.CurrentCount);

            task.IncrementStep();

            Assert.AreEqual(1, task.CurrentCount);
        }

        [Test]
        public void IntTask_IncrementToRequired_CompletesTask()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();
            IntTaskRuntime task = quest.Tasks[0] as IntTaskRuntime;

            // Increment to required count (3)
            task.IncrementStep();
            task.IncrementStep();
            task.IncrementStep();

            Assert.AreEqual(TaskState.Completed, task.CurrentState);
        }

        [Test]
        public void IntTask_Progress_CalculatesCorrectly()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();
            IntTaskRuntime task = quest.Tasks[0] as IntTaskRuntime;

            Assert.AreEqual(0f, task.Progress, 0.001f);

            task.IncrementStep();
            Assert.AreEqual(1f / 3f, task.Progress, 0.001f);

            task.IncrementStep();
            Assert.AreEqual(2f / 3f, task.Progress, 0.001f);
        }

        [Test]
        public void Task_Complete_StartsNextTask()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();

            Assert.AreEqual(TaskState.InProgress, quest.Tasks[0].CurrentState);
            Assert.AreEqual(TaskState.NotStarted, quest.Tasks[1].CurrentState);

            quest.Tasks[0].CompleteTask();

            Assert.AreEqual(TaskState.Completed, quest.Tasks[0].CurrentState);
            Assert.AreEqual(TaskState.InProgress, quest.Tasks[1].CurrentState);
        }

        [Test]
        public void Task_DecrementStep_DecreasesCount()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();
            IntTaskRuntime task = quest.Tasks[0] as IntTaskRuntime;

            task.IncrementStep();
            task.IncrementStep();
            Assert.AreEqual(2, task.CurrentCount);

            task.DecrementStep();
            Assert.AreEqual(1, task.CurrentCount);
        }

        #endregion

        #region Quest Progress Tests

        [Test]
        public void Quest_CurrentProgress_CalculatesFromTasks()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();

            // Initial progress should be 0
            Assert.AreEqual(0f, quest.CurrentProgress, 0.001f);

            // Complete first task (task1 has 3 required, task2 has 5 required)
            quest.Tasks[0].CompleteTask();

            // First task complete (1.0), second task not started (0.0)
            // Average: (1.0 + 0.0) / 2 = 0.5
            Assert.AreEqual(0.5f, quest.CurrentProgress, 0.001f);
        }

        [Test]
        public void Quest_AllTasksComplete_CompletesQuest()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();
            bool completedEventFired = false;
            quest.OnQuestCompleted.AddListener(_ => completedEventFired = true);

            quest.Tasks[0].CompleteTask();
            quest.Tasks[1].CompleteTask();

            Assert.AreEqual(QuestState.Completed, quest.CurrentState);
            Assert.IsTrue(completedEventFired);
        }

        #endregion

        #region Task Events Tests

        [Test]
        public void Task_OnTaskUpdated_FiresOnIncrement()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();
            bool eventFired = false;
            quest.Tasks[0].OnTaskUpdated.AddListener(_ => eventFired = true);

            quest.Tasks[0].IncrementStep();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void Task_OnTaskCompleted_FiresOnComplete()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();
            bool eventFired = false;
            quest.Tasks[0].OnTaskCompleted.AddListener(_ => eventFired = true);

            quest.Tasks[0].CompleteTask();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void Task_OnTaskFailed_FiresOnFail()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();
            bool eventFired = false;
            quest.Tasks[0].OnTaskFailed.AddListener(_ => eventFired = true);

            quest.Tasks[0].FailTask();

            Assert.IsTrue(eventFired);
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Task_Reset_ResetsToNotStarted()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();
            IntTaskRuntime task = quest.Tasks[0] as IntTaskRuntime;

            task.IncrementStep();
            task.IncrementStep();

            task.ResetTask();

            Assert.AreEqual(TaskState.NotStarted, task.CurrentState);
            Assert.AreEqual(0, task.CurrentCount);
        }

        [Test]
        public void Quest_Reset_RestartsQuest()
        {
            QuestRuntime quest = _questData.GetRuntimeQuest();
            quest.StartQuest();
            quest.Tasks[0].CompleteTask();

            quest.ResetQuest();

            Assert.AreEqual(QuestState.InProgress, quest.CurrentState);
            Assert.AreEqual(TaskState.InProgress, quest.Tasks[0].CurrentState);
        }

        #endregion

        #region Helper Methods

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field == null)
            {
                // Try base type
                field = obj.GetType().BaseType?.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
            }

            field?.SetValue(obj, value);
        }

        #endregion
    }
}
