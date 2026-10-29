// CMP5402 Mobile Development - Week 5: Optimisation Techniques
// FastEnemy: the fixed version of SlowEnemy from the Week 4 pack ("Cache it once, use it every frame" slide).
//
// FIXES (compare with the four PROBLEM comments in SlowEnemy.cs):
//   1. Rigidbody cached once in Awake, not GetComponent every frame.
//   2. A static list that enemies add themselves to, instead of FindObjectsByType every frame.
//   3. A plain loop with a best-so-far variable, instead of LINQ Where / OrderBy / FirstOrDefault.
//   4. No string building: the nearest enemy is kept in a field you can read, not written into the name.
//   5. Bonus: the nearest-enemy search runs 4 times a second, not every frame. Nobody can tell.
//
// Drop it on the same prefab as SlowEnemy (or swap the component) and compare the CPU Usage module:
// Update.ScriptRunBehaviourUpdate time and the GC Alloc column.

using System.Collections.Generic;
using UnityEngine;

namespace CMP5402.Week5
{
    public class FastEnemy : MonoBehaviour
    {
        public int id;
        public float speed = 2f;
        public float arenaRadius = 18f;

        [Tooltip("How often to look for the nearest enemy. AI doesn't need to think every frame.")]
        [SerializeField] float thinkInterval = 0.25f;

        // FIX 2: every active enemy is in this list. No scene searches, no arrays allocated.
        public static readonly List<FastEnemy> All = new List<FastEnemy>();

        public FastEnemy Nearest { get; private set; }   // FIX 4: data, not a string

        Rigidbody body;          // FIX 1: cached
        Vector3 target;
        float nextThink;

        void Awake() => body = GetComponent<Rigidbody>();

        void OnEnable()
        {
            All.Add(this);
            nextThink = Time.time + Random.Range(0f, thinkInterval); // spread the thinking across frames
            PickNewTarget();
        }

        void OnDisable() => All.Remove(this);

        void Update()
        {
            if (Time.time >= nextThink)
            {
                nextThink = Time.time + thinkInterval;
                Nearest = FindNearest();   // FIX 3 and 5
            }

            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.25f) PickNewTarget();
            Vector3 step = toTarget.normalized * speed * Time.deltaTime;

            if (body != null && body.isKinematic) body.MovePosition(body.position + step);
            else transform.position += step;
        }

        // FIX 3: one pass over the list, no allocations, no square roots.
        FastEnemy FindNearest()
        {
            FastEnemy best = null;
            float bestDist = float.MaxValue;
            Vector3 here = transform.position;
            for (int i = 0; i < All.Count; i++)
            {
                FastEnemy other = All[i];
                if (other == this) continue;
                float d = (other.transform.position - here).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = other; }
            }
            return best;
        }

        void PickNewTarget()
        {
            Vector2 p = Random.insideUnitCircle * arenaRadius;
            target = new Vector3(p.x, transform.position.y, p.y);
        }
    }
}
