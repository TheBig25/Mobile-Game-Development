// CMP5402 Mobile Development - Week 5: Optimisation Techniques
// UpdateManager: one Update loop instead of hundreds ("Do less, less often" slide).
//
// Unity's programming best practices page recommends minimising the number of active Update functions
// with a centralised update manager. Each MonoBehaviour.Update call has a fixed cost, so 500 small
// Updates cost more than one Update that loops over 500 objects.
//
// Usage: implement ITicked on any class (it doesn't need to be a MonoBehaviour) and register it:
//     void OnEnable()  => UpdateManager.Register(this);
//     void OnDisable() => UpdateManager.Unregister(this);
//     public void Tick(float deltaTime) { ... }
// Remove the Update() method from that class. Measure before and after with 200+ objects.

using System.Collections.Generic;
using UnityEngine;

namespace CMP5402.Week5
{
    public interface ITicked
    {
        void Tick(float deltaTime);
    }

    public class UpdateManager : MonoBehaviour
    {
        static UpdateManager instance;
        static readonly List<ITicked> ticked = new List<ITicked>();
        static readonly List<ITicked> pendingRemove = new List<ITicked>();

        public static int Count => ticked.Count;

        public static void Register(ITicked t)
        {
            if (instance == null)
            {
                var go = new GameObject("UpdateManager");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<UpdateManager>();
            }
            if (!ticked.Contains(t)) ticked.Add(t);
        }

        // Removal is deferred so an object can unregister itself from inside Tick without breaking the loop.
        public static void Unregister(ITicked t) => pendingRemove.Add(t);

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < ticked.Count; i++) ticked[i].Tick(dt);

            if (pendingRemove.Count > 0)
            {
                for (int i = 0; i < pendingRemove.Count; i++) ticked.Remove(pendingRemove[i]);
                pendingRemove.Clear();
            }
        }
    }
}
