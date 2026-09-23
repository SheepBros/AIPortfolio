using NUnit.Framework;
using UnityEngine;

namespace StageFlow.Tests
{
    public sealed class StagePlanTests
    {
        private GameObject prefab;
        private EnemyDefinition enemy;
        private StageDefinition stage;

        [SetUp]
        public void SetUp()
        {
            prefab = new GameObject("Test Enemy", typeof(EnemyMover));
            enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            enemy.Id = "enemy"; enemy.DisplayName = "Enemy"; enemy.Prefab = prefab; enemy.MoveSpeed = 2f;
            stage = ScriptableObject.CreateInstance<StageDefinition>();
            stage.Id = "test"; stage.DisplayName = "Test"; stage.SpawnInterval = 0.25f;
            stage.Entries = new[] { new SpawnEntry { Enemy = enemy, Count = 3 }, new SpawnEntry { Enemy = enemy, Count = 2 } };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(stage);
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void PlanPreservesOrderCountsAndSnapshotWithoutChangingSource()
        {
            Assert.That(StagePlan.TryCreate(stage, out var plan, out var error), Is.True, error);
            Assert.That(plan.Total, Is.EqualTo(5));
            Assert.That(plan.Items[0].Count, Is.EqualTo(3));
            Assert.That(plan.Items[1].Count, Is.EqualTo(2));
            Assert.That(stage.Entries[0].Count, Is.EqualTo(3));
            stage.Entries[0].Count = 7;
            enemy.MoveSpeed = 9f;
            stage.SpawnInterval = 4f;
            Assert.That(plan.Total, Is.EqualTo(5));
            Assert.That(plan.Items[0].Speed, Is.EqualTo(2f));
            Assert.That(plan.Interval, Is.EqualTo(0.25f));
        }

        [Test]
        public void MissingPrefabRejectsWholePlan()
        {
            enemy.Prefab = null;
            Assert.That(StagePlan.TryCreate(stage, out var plan, out var error), Is.False);
            Assert.That(plan, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonPositiveCountIsRejected(int count)
        {
            stage.Entries[1].Count = count;
            Assert.That(StagePlan.TryCreate(stage, out _, out _), Is.False);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidIntervalIsRejected(float interval)
        {
            stage.SpawnInterval = interval;
            Assert.That(StagePlan.TryCreate(stage, out _, out _), Is.False);
        }

        [Test]
        public void TotalOverflowIsRejectedWithoutAllocatingEverySpawn()
        {
            stage.Entries[0].Count = int.MaxValue;
            Assert.That(StagePlan.TryCreate(stage, out _, out _), Is.False);
        }
    }
}
