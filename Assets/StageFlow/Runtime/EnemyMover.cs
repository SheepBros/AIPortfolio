using System;
using UnityEngine;

namespace StageFlow
{
    public sealed class EnemyMover : MonoBehaviour
    {
        private Vector3 destination;
        private float speed;
        private Func<float> getDeltaTime;
        private Action<EnemyMover> onArrival;

        public void Initialize(Vector3 target, float moveSpeed, Action<EnemyMover> callback,
            Func<float> deltaTime)
        {
            getDeltaTime = deltaTime ?? throw new ArgumentNullException(nameof(deltaTime));
            destination = target; speed = moveSpeed; onArrival = callback;
        }

        private void Update()
        {
            if (onArrival == null) return;
            var deltaTime = getDeltaTime();
            // Zero time must also defer arrival callbacks, even when already at the destination.
            if (deltaTime <= 0f) return;
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * deltaTime);
            if ((transform.position - destination).sqrMagnitude > 0.0001f) return;
            var callback = onArrival;
            onArrival = null;
            callback(this);
        }
    }
}
