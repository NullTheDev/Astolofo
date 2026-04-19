using System;
using System.Collections;
using System.Reflection;
using AstolfoGorillaTagMenu.UI;
using BepInEx.Logging;
using UnityEngine;

namespace AstolfoGorillaTagMenu.Core
{
    public enum LimitKind
    {
        LogErrors,
        RpcChannel
    }

    public static class LimitMonitor
    {
        private const float WarnRatio = 0.8f;

        private static ManualLogSource? _log;
        private static bool _reflectOk;
        private static bool _loggedReflectFail;

        private static FieldInfo? _fiLogErrorCount;
        private static FieldInfo? _fiUserRpcCalls;
        private static FieldInfo? _fiRpcCalls;
        private static Type? _nestedTrackerType;

        private static int _phaseLog;
        private static int _phaseRpc;

        public static event Action<LimitKind, int, int>? OnLimitWarning;
        public static event Action<LimitKind, int, int>? OnLimitExceeded;

        public static void Initialize(ManualLogSource log)
        {
            _log = log;
            TryCacheReflection();
        }

        public static void Tick()
        {
            if (!_reflectOk)
                TryCacheReflection();
            if (!_reflectOk)
                return;

            var ma = MonkeAgent.instance;
            if (ma == null)
                return;

            var logMax = Mathf.Max(1, ma.logErrorMax);
            var rpcMaxLimit = Mathf.Max(1, ma.rpcCallLimit);

            var logCount = 0;
            try
            {
                if (_fiLogErrorCount != null)
                    logCount = (int)_fiLogErrorCount.GetValue(ma)!;
            }
            catch
            {
                return;
            }

            var warnLog = (int)Mathf.Ceil(logMax * WarnRatio);
            var maxRpc = GetMaxRpcCalls(ma);

            UpdateLogPhase(logCount, logMax, warnLog);
            UpdateRpcPhase(maxRpc, rpcMaxLimit);
        }

        private static void TryCacheReflection()
        {
            if (_reflectOk)
                return;
            try
            {
                var t = typeof(MonkeAgent);
                _fiLogErrorCount = t.GetField("logErrorCount", BindingFlags.Instance | BindingFlags.NonPublic);
                _fiUserRpcCalls = t.GetField("userRPCCalls", BindingFlags.Instance | BindingFlags.NonPublic);
                _nestedTrackerType = t.GetNestedType("RPCCallTracker", BindingFlags.NonPublic);
                if (_nestedTrackerType != null)
                    _fiRpcCalls = _nestedTrackerType.GetField("RPCCalls", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                _reflectOk = _fiLogErrorCount != null && _fiUserRpcCalls != null && _nestedTrackerType != null && _fiRpcCalls != null;
                if (!_reflectOk && !_loggedReflectFail)
                {
                    _loggedReflectFail = true;
                    _log?.LogWarning("[Astolfo] LimitMonitor: reflection setup incomplete; limit toasts disabled.");
                }
            }
            catch (Exception ex)
            {
                if (!_loggedReflectFail)
                {
                    _loggedReflectFail = true;
                    _log?.LogWarning($"[Astolfo] LimitMonitor reflection failed: {ex.Message}");
                }
                _reflectOk = false;
            }
        }

        private static int GetMaxRpcCalls(MonkeAgent ma)
        {
            var max = 0;
            try
            {
                var root = _fiUserRpcCalls?.GetValue(ma);
                if (root is not IDictionary outer)
                    return 0;

                foreach (DictionaryEntry e in outer)
                {
                    if (e.Value is not IDictionary inner)
                        continue;
                    foreach (DictionaryEntry e2 in inner)
                    {
                        var tracker = e2.Value;
                        if (tracker == null || _fiRpcCalls == null)
                            continue;
                        var v = _fiRpcCalls.GetValue(tracker);
                        if (v is int n && n > max)
                            max = n;
                    }
                }
            }
            catch
            {
                return max;
            }

            return max;
        }

        private static void UpdateLogPhase(int count, int limit, int warnThreshold)
        {
            var next = 0;
            if (count >= limit)
                next = 2;
            else if (count >= warnThreshold)
                next = 1;

            if (next > _phaseLog)
            {
                if (next == 1)
                {
                    OnLimitWarning?.Invoke(LimitKind.LogErrors, count, limit);
                    NotificationManager.Push($"Log errors high ({count}/{limit})", 4f, NotificationKind.Warning);
                }
                else if (next == 2)
                {
                    OnLimitExceeded?.Invoke(LimitKind.LogErrors, count, limit);
                    NotificationManager.Push($"Log error cap reached ({count}/{limit})", 5f, NotificationKind.Warning);
                }
            }

            _phaseLog = next;
        }

        private static void UpdateRpcPhase(int maxCalls, int limit)
        {
            var warnThreshold = (int)Mathf.Ceil(limit * WarnRatio);
            var next = 0;
            if (maxCalls > limit)
                next = 2;
            else if (maxCalls >= warnThreshold)
                next = 1;

            if (next > _phaseRpc)
            {
                if (next == 1)
                {
                    OnLimitWarning?.Invoke(LimitKind.RpcChannel, maxCalls, limit);
                    NotificationManager.Push($"RPC load high ({maxCalls}/{limit})", 4f, NotificationKind.Warning);
                }
                else if (next == 2)
                {
                    OnLimitExceeded?.Invoke(LimitKind.RpcChannel, maxCalls, limit);
                    NotificationManager.Push($"RPC limit exceeded ({maxCalls}/{limit})", 5f, NotificationKind.Warning);
                }
            }

            _phaseRpc = next;
        }
    }
}
