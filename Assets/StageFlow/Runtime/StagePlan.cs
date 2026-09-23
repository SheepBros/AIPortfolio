using System;
using System.Collections.Generic;
using UnityEngine;

namespace StageFlow
{
    // 실행 시 복사하여 원본 ScriptableObject를 카운터나 진행 상태로 사용하지 않는다.
    public sealed class StagePlan
    {
        public readonly struct Item
        {
            public readonly GameObject Prefab;
            public readonly float Speed;
            public readonly int Count;
            public Item(GameObject prefab, float speed, int count)
            { Prefab = prefab; Speed = speed; Count = count; }
        }

        public IReadOnlyList<Item> Items { get; }
        public int Total { get; }
        public float Interval { get; }

        private StagePlan(List<Item> items, int total, float interval)
        { Items = items.AsReadOnly(); Total = total; Interval = interval; }

        public static bool TryCreate(StageDefinition stage, out StagePlan plan, out string error)
        {
            plan = null;
            error = "";
            if (stage == null || string.IsNullOrWhiteSpace(stage.Id) ||
                string.IsNullOrWhiteSpace(stage.DisplayName))
                error = "Select a stage with an ID and display name.";
            else if (!Finite(stage.SpawnInterval) || stage.SpawnInterval < 0f)
                error = "Spawn interval must be finite and non-negative.";
            else if (stage.Entries == null || stage.Entries.Length == 0)
                error = "The stage needs at least one spawn entry.";
            if (error.Length > 0) return false;

            var items = new List<Item>();
            long total = 0;
            foreach (var entry in stage.Entries)
            {
                var enemy = entry?.Enemy;
                if (enemy == null || string.IsNullOrWhiteSpace(enemy.Id) ||
                    string.IsNullOrWhiteSpace(enemy.DisplayName) || enemy.Prefab == null ||
                    !enemy.Prefab.activeSelf || enemy.Prefab.GetComponent<EnemyMover>() == null ||
                    !enemy.Prefab.GetComponent<EnemyMover>().enabled ||
                    !Finite(enemy.MoveSpeed) || enemy.MoveSpeed <= 0f || entry.Count <= 0)
                { error = "Each entry needs a valid enemy, active mover prefab, positive speed and count."; return false; }
                total += entry.Count;
                if (total > int.MaxValue)
                { error = "The stage total exceeds the supported count."; return false; }
                items.Add(new Item(enemy.Prefab, enemy.MoveSpeed, entry.Count));
            }
            plan = new StagePlan(items, (int)total, stage.SpawnInterval);
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
