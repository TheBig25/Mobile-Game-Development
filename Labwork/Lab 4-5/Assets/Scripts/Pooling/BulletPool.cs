// CMP5402 Mobile Development - Week 5: Optimisation Techniques
// BulletPool: object pooling with Unity's built-in UnityEngine.Pool.ObjectPool<T> ("Object pooling" slide).
// This is the fix for the Bullet Spam module in the Week 4 game.
//
// Usage: add to an empty GameObject, assign the PooledBullet prefab, then from your gun:
//     pool.Spawn(muzzle.position, muzzle.forward);
// Watch the CPU Usage module: Instantiate, Destroy and GC.Alloc disappear from the frame after warm-up.

using UnityEngine;
using UnityEngine.Pool;

namespace CMP5402.Week5
{
    public class BulletPool : MonoBehaviour
    {
        [SerializeField] PooledBullet prefab;

        [Tooltip("Bullets created up front during Start, so the first volley doesn't Instantiate mid-fight.")]
        [SerializeField] int prewarm = 64;

        [Tooltip("Bullets released when the pool already holds this many are destroyed instead of kept.")]
        [SerializeField] int maxSize = 512;

        IObjectPool<PooledBullet> pool;

        public int CountActive => pool is ObjectPool<PooledBullet> p ? p.CountActive : -1;

        void Awake()
        {
            pool = new ObjectPool<PooledBullet>(
                createFunc: Create,
                actionOnGet: b => b.gameObject.SetActive(true),
                actionOnRelease: b => b.gameObject.SetActive(false),
                actionOnDestroy: b => Destroy(b.gameObject),
                collectionCheck: true,      // throws if a bullet is released twice (Editor and dev builds)
                defaultCapacity: prewarm,
                maxSize: maxSize);
        }

        void Start()
        {
            // Warm up: take and immediately return 'prewarm' bullets so they exist before the game starts.
            var temp = new PooledBullet[prewarm];
            for (int i = 0; i < prewarm; i++) temp[i] = pool.Get();
            for (int i = 0; i < prewarm; i++) pool.Release(temp[i]);
        }

        PooledBullet Create()
        {
            PooledBullet b = Instantiate(prefab, transform);   // parented, so the Hierarchy stays tidy
            b.Pool = this;
            b.gameObject.SetActive(false);
            return b;
        }

        public PooledBullet Spawn(Vector3 position, Vector3 direction)
        {
            PooledBullet b = pool.Get();
            b.Fire(position, direction);
            return b;
        }

        public void Despawn(PooledBullet b) => pool.Release(b);

        void OnDestroy() => pool?.Clear();
    }
}
