// CMP5402 Mobile Development - Week 5: Optimisation Techniques
// NonAllocSensor: "who is near me?" without allocating ("Where per-frame garbage comes from" slide).
//
// Physics.OverlapSphere returns a NEW array every call: garbage every frame. The NonAlloc version fills an
// array you own and returns how many it found. The array is sized once; if it fills up, raise the size.
// The scan also runs on a timer, not every frame, and uses a LayerMask so physics does less work.

using UnityEngine;

namespace CMP5402.Week5
{
    public class NonAllocSensor : MonoBehaviour
    {
        [SerializeField] float radius = 5f;
        [SerializeField] LayerMask layers = ~0;          // everything, by default
        [SerializeField] float scanInterval = 0.2f;      // 5 scans a second is plenty
        [SerializeField] int maxResults = 32;

        Collider[] results;                              // allocated once
        float nextScan;

        public int Count { get; private set; }
        public Collider[] Results => results;             // valid entries are [0, Count)

        void Awake() => results = new Collider[maxResults];

        void Update()
        {
            if (Time.time < nextScan) return;
            nextScan = Time.time + scanInterval;

            Count = Physics.OverlapSphereNonAlloc(transform.position, radius, results, layers);
            if (Count == results.Length)
                Log.Warning(name + ": NonAllocSensor array is full. Raise Max Results.");
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
