using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace CMP5402.Week4
{
    // Deliberate inefficiencies for diagnosis, not production recommendations.
    public class Lab4Workload : MonoBehaviour
    {
        public enum WorkloadMode { Idle, Cpu, Allocations }
        [SerializeField] WorkloadMode mode;
        [SerializeField, Range(10000, 4000000)] int cpuIterations = 250000;
        [SerializeField, Range(1024, 1048576)] int allocationBytesPerFrame = 32768;
        static readonly ProfilerMarker CpuMarker = new ProfilerMarker("Lab4.CpuWork");
        static readonly ProfilerMarker AllocationMarker = new ProfilerMarker("Lab4.Allocations");
        readonly List<byte[]> retainedBlocks = new List<byte[]>(8);
        byte[] scratch;
        double checksum;
        public WorkloadMode Mode => mode;
        public int RetainedMiB => retainedBlocks.Count * 8;
        public double Checksum => checksum;

        public void SetMode(WorkloadMode value) => mode = value;

        void Update()
        {
            if (mode == WorkloadMode.Cpu)
            {
                using (CpuMarker.Auto())
                {
                    double result = 0;
                    int count = Mathf.Clamp(cpuIterations, 10000, 4000000);
                    for (int i = 1; i <= count; i++)
                        result += Math.Sqrt(i) * Math.Sin(i * 0.001);
                    checksum = result; // Keep a visible result of the computation.
                }
            }
            else if (mode == WorkloadMode.Allocations)
            {
                using (AllocationMarker.Auto())
                {
                    scratch = new byte[Mathf.Clamp(allocationBytesPerFrame, 1024, 1048576)];
                    scratch[0] = (byte)(Time.frameCount & 255);
                    checksum = scratch[0];
                }
            }
        }

        public void RetainEightMiB()
        {
            if (retainedBlocks.Count >= 8) return; // Bound the experiment to 64 MiB.
            var block = new byte[8 * 1024 * 1024];
            for (int i = 0; i < block.Length; i += 4096) block[i] = 1;
            retainedBlocks.Add(block);
        }

        public void ReleaseRetainedMemory()
        {
            retainedBlocks.Clear();
            scratch = null;
            // No forced GC. Releasing references need not immediately shrink reserved memory.
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) mode = WorkloadMode.Idle;
        }

        void OnDestroy() => ReleaseRetainedMemory();
    }
}
