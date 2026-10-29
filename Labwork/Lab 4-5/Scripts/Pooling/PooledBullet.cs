// CMP5402 Mobile Development - Week 5: Optimisation Techniques
// PooledBullet: a bullet that goes back to its pool instead of being destroyed ("Object pooling" slide).
// Put it on your bullet prefab with a Rigidbody and a Collider. BulletPool assigns Pool when it creates it.
//
// The important habit: reset EVERYTHING in OnEnable. A pooled object remembers its old velocity, its old
// trail, its old timer. That is the number one cause of "pooling broke my game".

using UnityEngine;

namespace CMP5402.Week5
{
    [RequireComponent(typeof(Rigidbody))]
    public class PooledBullet : MonoBehaviour
    {
        [SerializeField] float speed = 30f;
        [SerializeField] float lifetime = 3f;

        public BulletPool Pool { get; set; }   // set by the pool that made us

        Rigidbody body;
        TrailRenderer trail;   // optional
        float dieAt;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            trail = GetComponent<TrailRenderer>();
        }

        // Called by BulletPool after Get, once the bullet has been positioned.
        public void Fire(Vector3 position, Vector3 direction)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            body.linearVelocity = direction.normalized * speed;
            body.angularVelocity = Vector3.zero;
            dieAt = Time.time + lifetime;
            if (trail != null) trail.Clear();   // don't draw a line from where it last died
        }

        void Update()
        {
            if (Time.time >= dieAt) Despawn();
        }

        void OnCollisionEnter(Collision collision) => Despawn();

        void Despawn()
        {
            if (!gameObject.activeSelf) return;      // already released this frame
            if (Pool != null) Pool.Despawn(this);    // back to the pool
            else Destroy(gameObject);                // not pooled: behave like a normal bullet
        }
    }
}
