using System;
using UnityEngine;

namespace StageFlow
{
    [Serializable]
    public sealed class SpawnEntry
    {
        public EnemyDefinition Enemy;
        [Min(1)] public int Count = 1;
    }

    [CreateAssetMenu(menuName = "StageFlow/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public SpawnEntry[] Entries = Array.Empty<SpawnEntry>();
        [Min(0f)] public float SpawnInterval = 0.6f;
    }
}
