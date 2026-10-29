// CMP5402 Mobile Development - Week 5: Optimisation Techniques
// Log: a logging wrapper that compiles to nothing in builds ("Debug.Log is not free" slide).
// This is the fix for the Log Spam module in the Week 4 game.
//
// How it works: [Conditional("ENABLE_LOGS")] tells the C# compiler to remove every CALL to the method
// unless ENABLE_LOGS is defined. The arguments are removed too, so the string is never built.
//
// Turning logs on in a build: Project Settings > Player > Other Settings > Scripting Define Symbols, add ENABLE_LOGS.
// In the Editor the calls always stay: with two Conditional attributes, the call is kept if EITHER symbol is
// defined, and UNITY_EDITOR is always defined for Editor compilation.
// Errors and exceptions are NOT conditional: you always want those in crash reports.

using System.Diagnostics;

namespace CMP5402.Week5
{
    public static class Log
    {
        [Conditional("ENABLE_LOGS"), Conditional("UNITY_EDITOR")]
        public static void Info(string message) => UnityEngine.Debug.Log(message);

        [Conditional("ENABLE_LOGS"), Conditional("UNITY_EDITOR")]
        public static void Info(string message, UnityEngine.Object context) => UnityEngine.Debug.Log(message, context);

        [Conditional("ENABLE_LOGS"), Conditional("UNITY_EDITOR")]
        public static void Warning(string message) => UnityEngine.Debug.LogWarning(message);

        // Always compiled in.
        public static void Error(string message) => UnityEngine.Debug.LogError(message);
        public static void Exception(System.Exception e) => UnityEngine.Debug.LogException(e);
    }
}
