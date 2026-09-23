using NUnit.Framework;
using StageFlow.Editor;
using UnityEditor;

namespace StageFlow.Tests
{
    public sealed class SampleAssetTests
    {
        [Test]
        public void SampleStagesHaveValidPlansWithFiveAndEightEnemies()
        {
            foreach (var sample in new[] { ("StageOne", 5), ("StageTwo", 8) })
            {
                var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(SampleProjectGenerator.Root + "/Data/" + sample.Item1 + ".asset");
                Assert.That(StagePlan.TryCreate(stage, out var plan, out var error), Is.True, error);
                Assert.That(plan.Total, Is.EqualTo(sample.Item2));
                Assert.That(plan.Items[0].Prefab, Is.Not.EqualTo(plan.Items[1].Prefab));
            }
        }
    }
}
