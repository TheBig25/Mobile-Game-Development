// CMP5402 Mobile Development - Week 7: Saving, Cloud Data & Online
// NetProbe: an honest "are we online?" check and a server time source ("A request, with every way it can fail" and
// "Design for the connection you actually have" slides).
//
// Application.internetReachability only reports the KIND of connection (none, mobile data, Wi-Fi). Unity's docs say not to
// use it to determine actual connectivity: a phone on a hotspot with no route still says ReachableViaLocalAreaNetwork.
// A real check is a small request with a timeout. This component makes one, with retry and backoff, and reads the HTTP
// Date header so daily rewards and cooldowns can use server time instead of the phone's clock.
//
// Unity 6: UnityWebRequest.SendWebRequest() returns an AsyncOperation, and all AsyncOperations can be awaited, so the
// coroutine in Unity's example becomes an async Awaitable method. The request is wrapped in 'using' because it holds
// native buffers (Unity's own example does the same).
//
// Server time is anchored to Time.realtimeSinceStartupAsDouble (monotonic, unaffected by the clock settings), so changing
// the phone's clock after the probe does not move ServerNow. A sample older than MaxSampleAgeHours is discarded.
// Reaching the probe endpoint proves only that endpoint is reachable, not that Cloud Save (or your server) is; and any
// reward that matters is still validated server-side. This is a hint for the UI, not a security measure.
//
// Sources: UnityWebRequest.Get, Application.internetReachability (Unity 6.3 Scripting API);
// Introduction to asynchronous programming with Awaitable (Unity 6.3 Manual).

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

namespace CMP5402.Week7
{
    public class NetProbe : MonoBehaviour
    {
        public static NetProbe Instance { get; private set; }

        [Tooltip("Any reliable HTTPS endpoint that answers quickly. Replace with your own server's /ping if you have one.")]
        [SerializeField] string probeUrl = "https://www.gstatic.com/generate_204";
        [SerializeField] int timeoutSeconds = 8;
        [Tooltip("Four attempts means three waits: 1 s, 2 s, 4 s.")]
        [SerializeField] int maxAttempts = 4;
        [SerializeField] float maxSampleAgeHours = 24f;
        [Tooltip("While offline, probe again after this many seconds, doubling up to the maximum. While online, re-check at the maximum interval.")]
        [SerializeField] float offlineRecheckSeconds = 15f, maxRecheckSeconds = 300f;
        float nextProbeAt = -1f, recheckDelay;
        bool probing;

        /// <summary>Raised when the probe result changes (Offline to Online, say). CloudSync re-syncs on reconnection.</summary>
        public static event Action<Reach> ReachabilityChanged;

        /// <summary>Result of the last probe. Unknown until the first one completes.</summary>
        public enum Reach { Unknown, Online, Offline }
        public Reach Last { get; private set; } = Reach.Unknown;
        public string LastDetail { get; private set; } = "";
        public float LastProbeTime { get; private set; } = -1f;

        DateTimeOffset serverAtSample;      // the server's Date header
        double sampleRealtime = -1;         // Time.realtimeSinceStartupAsDouble when it arrived

        /// <summary>Server time minus the phone's clock at the last sample, for the overlay. null if never learned.</summary>
        public double? ClockOffsetSeconds { get; private set; }

        public static string ConnectionKind => Application.internetReachability switch
        {
            NetworkReachability.NotReachable => "none",
            NetworkReachability.ReachableViaCarrierDataNetwork => "mobile data",
            _ => "Wi-Fi / LAN",
        };

        /// <summary>Server 'now': the sampled server time plus the monotonic time elapsed since the sample. Independent of
        /// the phone's clock. null if never learned or the sample is stale. Callers must handle null: 'unknown', never 'free'.</summary>
        public DateTimeOffset? ServerNow
        {
            get
            {
                if (sampleRealtime < 0) return null;
                double elapsed = Time.realtimeSinceStartupAsDouble - sampleRealtime;
                if (elapsed > maxSampleAgeHours * 3600.0) return null;
                return serverAtSample.AddSeconds(elapsed);
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        async void Start()
        {
            await ProbeAsync();
        }

        // Reconnection is detected by re-probing: quickly at first while offline, backing off to maxRecheckSeconds,
        // and once immediately when the app comes back from the background (the usual moment a connection changes).
        void Update()
        {
            if (!probing && nextProbeAt >= 0f && Time.unscaledTime >= nextProbeAt) _ = ProbeAsync();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused && !probing) _ = ProbeAsync();
        }

        void ScheduleNext()
        {
            if (Last == Reach.Online)
            {
                recheckDelay = 0f;                         // the next offline episode starts its backoff from the beginning
                nextProbeAt = Time.unscaledTime + maxRecheckSeconds;
                return;
            }
            recheckDelay = recheckDelay <= 0f ? offlineRecheckSeconds : Mathf.Min(recheckDelay * 2f, maxRecheckSeconds);
            nextProbeAt = Time.unscaledTime + recheckDelay;
        }

        /// <summary>One probe with retry and doubling backoff (four attempts: waits of 1 s, 2 s, 4 s). Never blocks gameplay: await it only where
        /// the player chose to wait (a 'Sync now' button) or fire-and-forget it.</summary>
        public async Awaitable<bool> ProbeAsync()
        {
            if (probing) return Last == Reach.Online;
            probing = true;
            try { return await ProbeCore(); }
            finally { probing = false; ScheduleNext(); }
        }

        async Awaitable<bool> ProbeCore()
        {
            // The cheap pre-check is still useful for the obvious case.
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                SetResult(Reach.Offline, "no network interface");
                return false;
            }

            float delay = 1f;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var (ok, detail, serverDate) = await GetAsync(probeUrl);
                if (ok)
                {
                    if (serverDate.HasValue)
                    {
                        serverAtSample = serverDate.Value;
                        sampleRealtime = Time.realtimeSinceStartupAsDouble;
                        ClockOffsetSeconds = (serverDate.Value - DateTimeOffset.UtcNow).TotalSeconds;
                    }
                    SetResult(Reach.Online, detail);
                    return true;
                }
                Debug.Log($"[NetProbe] attempt {attempt}: {detail}");
                if (attempt < maxAttempts)
                {
                    await Awaitable.WaitForSecondsAsync(delay);
                    delay *= 2f;
                }
            }
            SetResult(Reach.Offline, "probe failed after " + maxAttempts + " attempts");
            return false;
        }

        void SetResult(Reach r, string detail)
        {
            bool changed = r != Last;
            Last = r;
            LastDetail = detail;
            LastProbeTime = Time.unscaledTime;
            Debug.Log($"[NetProbe] {r} ({ConnectionKind}): {detail}");
            if (changed) ReachabilityChanged?.Invoke(r);
        }

        /// <summary>GET with a timeout, classifying every failure the way Unity's result enum does. Returns the HTTP Date
        /// header parsed as UTC when present. Safe to call with any URL; never throws.</summary>
        public async Awaitable<(bool ok, string detail, DateTimeOffset? serverDate)> GetAsync(string url)
        {
            using var req = UnityWebRequest.Get(url);
            req.timeout = timeoutSeconds;
            try
            {
                await req.SendWebRequest();
            }
            catch (Exception e)
            {
                return (false, "exception: " + e.Message, null);
            }

            switch (req.result)
            {
                case UnityWebRequest.Result.Success:
                    return (true, "HTTP " + req.responseCode, ParseDate(req.GetResponseHeader("Date")));
                case UnityWebRequest.Result.ConnectionError:      // no route, DNS, TLS, timeout
                    return (false, "connection error: " + req.error, null);
                case UnityWebRequest.Result.DataProcessingError:  // body could not be handled
                    return (false, "data error: " + req.error, null);
                case UnityWebRequest.Result.ProtocolError:        // the server answered with 4xx / 5xx
                    // A server that answers at all proves the route works; the Date header is still trustworthy.
                    return (false, "HTTP " + req.responseCode + ": " + req.error, ParseDate(req.GetResponseHeader("Date")));
                default:
                    return (false, "in progress?", null);
            }
        }

        static DateTimeOffset? ParseDate(string header)
        {
            if (string.IsNullOrEmpty(header)) return null;
            return DateTimeOffset.TryParse(header, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d) ? d : (DateTimeOffset?)null;
        }
    }
}
