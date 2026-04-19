using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;

namespace AstolfoGorillaTagMenu.UI
{
    public enum NotificationKind
    {
        Info,
        Success,
        Warning,
        Muted
    }

    public sealed class NotificationManager : MonoBehaviour
    {
        public static NotificationManager? Instance { get; private set; }

        private const int MaxVisible = 8;
        private const float ToastWidth = 360f;
        private const float ToastHeight = 64f;
        private const float StackGap = 10f;
        private const float MarginX = 28f;
        private const float MarginY = 28f;

        private readonly List<ToastView> _toasts = new();
        private Font? _font;
        private RectTransform? _stackRoot;

        public static void EnsureCreated()
        {
            if (Instance != null)
                return;

            var go = new GameObject("Astolfo_Notifications");
            Object.DontDestroyOnLoad(go);

            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 32000;

            var sc = go.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            var cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var stack = new GameObject("Stack");
            var rt = stack.AddComponent<RectTransform>();
            rt.SetParent(go.transform, false);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-MarginX, MarginY);
            rt.sizeDelta = new Vector2(ToastWidth + 40f, 1080f);

            var mgr = go.AddComponent<NotificationManager>();
            mgr._stackRoot = rt;
            mgr._font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Instance = mgr;
        }

        public static void Push(string message, float duration = 3.2f, NotificationKind kind = NotificationKind.Info)
        {
            EnsureCreated();
            Instance?.Enqueue(message, duration, kind);
        }

        private void Enqueue(string message, float duration, NotificationKind kind)
        {
            if (_stackRoot == null)
                return;

            while (_toasts.Count >= MaxVisible)
                _toasts[0].DismissImmediate();

            var go = new GameObject("Toast");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(_stackRoot, false);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(ToastWidth, ToastHeight);

            var bg = go.AddComponent<Image>();
            bg.color = KindToBg(kind);
            bg.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = AstolfoTheme.Border;
            outline.effectDistance = new Vector2(2f, -2f);

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var textGo = new GameObject("Label");
            var tr = textGo.AddComponent<RectTransform>();
            tr.SetParent(rt, false);
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(14f, 8f);
            tr.offsetMax = new Vector2(-14f, -8f);

            var txt = textGo.AddComponent<Text>();
            txt.font = _font;
            txt.text = message;
            txt.fontSize = 17;
            txt.color = AstolfoTheme.TextPrimary;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.raycastTarget = false;

            var view = go.AddComponent<ToastView>();
            view.Setup(this, rt, cg, duration);

            _toasts.Add(view);
            LayoutStack();
            view.PlayEnterHoldExit();
        }

        private static Color KindToBg(NotificationKind kind)
        {
            return kind switch
            {
                NotificationKind.Success => AstolfoTheme.PanelBg,
                NotificationKind.Warning => AstolfoTheme.AccentDeep * new Color(1f, 1f, 1f, 0.92f),
                NotificationKind.Muted => AstolfoTheme.WindowBg,
                _ => AstolfoTheme.PanelBg
            };
        }

        private void OnToastClosed(ToastView t)
        {
            if (_toasts.Remove(t))
                LayoutStack();
        }

        private void LayoutStack()
        {
            for (int i = 0; i < _toasts.Count; i++)
                _toasts[i].SetStackY(i * (ToastHeight + StackGap));
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // =========================
        // ToastView (FIXED ONCE)
        // =========================
        private sealed class ToastView : MonoBehaviour
        {
            private NotificationManager? _owner;
            private RectTransform? _rt;
            private CanvasGroup? _cg;
            private float _hold;

            public void Setup(NotificationManager owner, RectTransform rt, CanvasGroup cg, float hold)
            {
                _owner = owner;
                _rt = rt;
                _cg = cg;
                _hold = hold;
            }

            public void SetStackY(float y)
            {
                if (_rt == null) return;

                var p = _rt.anchoredPosition;
                _rt.anchoredPosition = new Vector2(p.x, y);
            }

            public void PlayEnterHoldExit()
            {
                StartCoroutine(Run());
            }

            private IEnumerator Run()
            {
                if (_rt == null || _cg == null)
                    yield break;

                const float enter = 0.22f;
                const float exit = 0.2f;

                float startX = 96f;
                float y = _rt.anchoredPosition.y;

                _rt.anchoredPosition = new Vector2(startX, y);

                float t = 0f;

                while (t < enter)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / enter);
                    float e = 1f - Mathf.Pow(1f - k, 3f);

                    _cg.alpha = e;
                    _rt.anchoredPosition = new Vector2(Mathf.Lerp(startX, 0f, e), y);

                    yield return null;
                }

                _cg.alpha = 1f;
                _rt.anchoredPosition = new Vector2(0f, y);

                yield return new WaitForSecondsRealtime(_hold);

                t = 0f;
                var from = _rt.anchoredPosition;

                while (t < exit)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / exit);
                    float e = 1f - Mathf.Pow(1f - k, 2f);

                    _cg.alpha = 1f - e;
                    _rt.anchoredPosition =
                        new Vector2(Mathf.Lerp(from.x, from.x + 120f, e), from.y);

                    yield return null;
                }

                DismissImmediate();
            }

            public void DismissImmediate()
            {
                if (_owner != null)
                    _owner.OnToastClosed(this);

                Destroy(gameObject);
            }
        }
    }
}