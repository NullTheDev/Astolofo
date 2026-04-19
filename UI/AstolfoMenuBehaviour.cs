using System.Collections.Generic;
using AstolfoGorillaTagMenu.Features;
using BepInEx.Logging;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AstolfoGorillaTagMenu.UI
{
    internal sealed class AstolfoMenuBehaviour : MonoBehaviour
    {
        internal ManualLogSource Log = null!;
        private RectTransform? _rootPanel;
        private RectTransform? _playersContent;
        private readonly List<GameObject> _playerRows = new List<GameObject>();
        private GameObject[] _tabPanels = null!;
        private Button[] _tabButtons = null!;
        private Image[] _tabImages = null!;
        private Text[] _tabLabels = null!;
        private Font? _font;
        private bool _visible;
        private float _nextRefresh;
        private float _uiScale = 1f;

        private static Font BuiltinFont =>
            Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

        internal void Initialize(ManualLogSource log)
        {
            Log = log;
            PlayerActions.Log = log;
        }

        private void Start()
        {
            BuildUi();
            _visible = false;
            if (_rootPanel != null)
                _rootPanel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                _visible = !_visible;
                if (_rootPanel != null)
                    _rootPanel.gameObject.SetActive(_visible);
            }

            if (!_visible || _playersContent == null)
                return;

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.75f;
                RefreshPlayerList();
            }
        }

        private void BuildUi()
        {
            _font = BuiltinFont;

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var esGo = new GameObject("Astolfo_EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(esGo);
            }

            var canvasGo = new GameObject("Astolfo_MenuCanvas");
            var cv = canvasGo.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 30000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);

            _rootPanel = CreatePanel(canvasGo.transform, "Root", new Vector2(720, 520), AstolfoTheme.WindowBg);
            _rootPanel.localScale = Vector3.one * _uiScale;
            AddOutline(_rootPanel.gameObject, AstolfoTheme.Border, 3f);

            var header = CreatePanel(_rootPanel, "Header", new Vector2(704, 56), AstolfoTheme.AccentDeep);
            SetAnchoredHeader(header, 0, -28);

            var title = CreateText(header.transform, "Astolfo Lobby Menu", 26, AstolfoTheme.TextPrimary, TextAnchor.MiddleCenter);
            StretchFull(title.rectTransform);

            var tabBar = CreatePanel(_rootPanel, "Tabs", new Vector2(704, 44), AstolfoTheme.PanelBg);
            SetAnchoredHeader(tabBar, 0, -86);

            var tabLayout = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.padding = new RectOffset(12, 12, 6, 6);
            tabLayout.spacing = 8;
            tabLayout.childAlignment = TextAnchor.MiddleCenter;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;

            _tabButtons = new Button[3];
            _tabImages = new Image[3];
            _tabLabels = new Text[3];
            var tabNames = new[] { "Players", "Display", "About" };
            for (var i = 0; i < 3; i++)
            {
                var (btn, img, label) = CreateTabButton(tabBar.transform, tabNames[i], i);
                _tabButtons[i] = btn;
                _tabImages[i] = img;
                _tabLabels[i] = label;
            }

            var body = CreatePanel(_rootPanel, "Body", new Vector2(704, 396), AstolfoTheme.PanelBg);
            SetAnchoredCenter(body, new Vector2(0, -24));

            _tabPanels = new GameObject[3];
            _tabPanels[0] = BuildPlayersTab(body.transform);
            _tabPanels[1] = BuildDisplayTab(body.transform);
            _tabPanels[2] = BuildAboutTab(body.transform);

            SelectTab(0);
        }

        private GameObject BuildPlayersTab(Transform parent)
        {
            var panel = new GameObject("Tab_Players");
            var rt = panel.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            StretchFull(rt);

            var scrollGo = new GameObject("Scroll");
            var srt = scrollGo.AddComponent<RectTransform>();
            srt.SetParent(panel.transform, false);
            StretchFull(srt);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var viewport = new GameObject("Viewport");
            var vrt = viewport.AddComponent<RectTransform>();
            vrt.SetParent(scrollGo.transform, false);
            StretchFull(vrt);
            viewport.AddComponent<Image>().color = AstolfoTheme.PanelBg;
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = new GameObject("Content");
            _playersContent = content.AddComponent<RectTransform>();
            _playersContent.SetParent(viewport.transform, false);
            StretchTopStretchWidth(_playersContent);

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vrt;
            scroll.content = _playersContent;

            scrollGo.AddComponent<LayoutElement>().flexibleHeight = 1f;

            return panel;
        }

        private GameObject BuildDisplayTab(Transform parent)
        {
            var panel = new GameObject("Tab_Display");
            var rt = panel.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            StretchFull(rt);

            var v = new GameObject("VBox");
            var vrt = v.AddComponent<RectTransform>();
            vrt.SetParent(panel.transform, false);
            StretchFull(vrt);
            var vlg = v.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.UpperLeft;

            CreateText(vrt, "Resize the menu (saved for this session).", 18, AstolfoTheme.TextMuted, TextAnchor.UpperLeft)
                .gameObject.AddComponent<LayoutElement>().minHeight = 28;

            var row = new GameObject("ScaleRow");
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.SetParent(vrt, false);
            row.AddComponent<LayoutElement>().minHeight = 44;
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childForceExpandWidth = false;

            CreateSmallButton(rowRt, "−", () => AdjustScale(-0.05f));
            var value = CreateText(rowRt, "100%", 20, AstolfoTheme.TextPrimary, TextAnchor.MiddleCenter);
            value.gameObject.AddComponent<LayoutElement>().minWidth = 72;
            CreateSmallButton(rowRt, "+", () => AdjustScale(0.05f));

            _scaleLabel = value;

            panel.SetActive(false);
            return panel;
        }

        private Text? _scaleLabel;

        private void AdjustScale(float delta)
        {
            _uiScale = Mathf.Clamp(_uiScale + delta, 0.65f, 1.45f);
            if (_rootPanel != null)
                _rootPanel.localScale = Vector3.one * _uiScale;
            if (_scaleLabel != null)
                _scaleLabel.text = $"{Mathf.RoundToInt(_uiScale * 100f)}%";
        }

        private void CreateSmallButton(Transform parent, string label, UnityAction onClick)
        {
            var go = new GameObject("MiniBtn_" + label);
            go.AddComponent<RectTransform>().SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = AstolfoTheme.AccentDeep;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            CreateText(go.transform, label, 22, AstolfoTheme.TextPrimary, TextAnchor.MiddleCenter);
            StretchFull(go.transform.GetChild(0).GetComponent<RectTransform>());
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 44;
            le.minHeight = 40;
        }

        private GameObject BuildAboutTab(Transform parent)
        {
            var panel = new GameObject("Tab_About");
            var rt = panel.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            StretchFull(rt);

            var box = new GameObject("AboutBox");
            var brt = box.AddComponent<RectTransform>();
            brt.SetParent(panel.transform, false);
            StretchFull(brt);
            var vlg = box.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.childAlignment = TextAnchor.UpperLeft;

            var t = CreateText(brt,
                "Astolfo-themed lobby UI for Gorilla Tag.\n\n" +
                "Toggle: Insert\n\n" +
                "Players: teleport to others, copy names, or log info to the BepInEx console.\n" +
                "Toasts: room and player events appear bottom-right (animated stack).\n\n" +
                "Please use mods responsibly and respect other players.",
                18, AstolfoTheme.TextPrimary, TextAnchor.UpperLeft);
            t.verticalOverflow = VerticalWrapMode.Overflow;

            panel.SetActive(false);
            return panel;
        }

        private (Button btn, Image img, Text label) CreateTabButton(Transform parent, string name, int index)
        {
            var go = new GameObject("Tab_" + name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = AstolfoTheme.TabInactive;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = AstolfoTheme.AccentPink;
            colors.pressedColor = AstolfoTheme.AccentDeep;
            btn.colors = colors;

            var i = index;
            btn.onClick.AddListener(() => SelectTab(i));

            var label = CreateText(rt, name, 18, AstolfoTheme.TextPrimary, TextAnchor.MiddleCenter);
            StretchFull(label.rectTransform);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 36;
            le.flexibleWidth = 1f;
            return (btn, img, label);
        }

        private void SelectTab(int index)
        {
            for (var i = 0; i < _tabPanels.Length; i++)
                _tabPanels[i].SetActive(i == index);

            for (var i = 0; i < _tabImages.Length; i++)
            {
                _tabImages[i].color = i == index ? AstolfoTheme.TabActive : AstolfoTheme.TabInactive;
                _tabLabels[i].color = i == index ? AstolfoTheme.WindowBg : AstolfoTheme.TextPrimary;
            }

            if (index == 0)
                RefreshPlayerList();
        }

        private void RefreshPlayerList()
        {
            if (_playersContent == null || NetworkSystem.Instance == null)
                return;

            foreach (var row in _playerRows)
            {
                if (row != null)
                    Destroy(row);
            }
            _playerRows.Clear();

            var players = NetworkSystem.Instance.AllNetPlayers;
            if (players == null || players.Length == 0)
            {
                CreateInfoRow(_playersContent, "No players in cache (join a room first).");
                return;
            }

            foreach (var p in players)
            {
                if (p == null || !p.IsValid)
                    continue;
                CreatePlayerRow(_playersContent, p);
            }
        }

        private void CreateInfoRow(RectTransform parent, string message)
        {
            var row = CreateRowPanel(parent, "Info", 40);
            var t = CreateText(row.transform, message, 16, AstolfoTheme.TextMuted, TextAnchor.MiddleLeft);
            StretchFull(t.rectTransform);
            _playerRows.Add(row.gameObject);
        }

        private void CreatePlayerRow(RectTransform parent, NetPlayer player)
        {
            var row = CreateRowPanel(parent, "PlayerRow", 78);
            AddOutline(row.gameObject, AstolfoTheme.Border, 1.5f);

            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(10, 10, 8, 8);
            h.spacing = 8;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childForceExpandWidth = false;
            h.childControlWidth = false;

            var nameBlock = new GameObject("Names");
            nameBlock.AddComponent<RectTransform>().SetParent(row.transform, false);
            var nle = nameBlock.AddComponent<LayoutElement>();
            nle.flexibleWidth = 1f;
            nle.minWidth = 220;
            nameBlock.AddComponent<VerticalLayoutGroup>().spacing = 2;

            var display = string.IsNullOrEmpty(player.SanitizedNickName) ? player.NickName : player.SanitizedNickName;
            if (string.IsNullOrEmpty(display))
                display = player.DefaultName;

            var line1 = $"{display}  (#{player.ActorNumber})";
            var dist = PlayerActions.TryGetDistanceMeters(player);
            var tags = player.IsLocal ? "YOU" : (player.IsMasterClient ? "HOST" : "");
            var line2 = dist.HasValue ? $"{dist.Value:0.0} m  {tags}" : tags;

            var t1 = CreateText(nameBlock.transform, line1, 19, AstolfoTheme.TextPrimary, TextAnchor.UpperLeft);
            t1.gameObject.AddComponent<LayoutElement>().minHeight = 24;
            var t2 = CreateText(nameBlock.transform, line2, 14, AstolfoTheme.TextMuted, TextAnchor.UpperLeft);
            t2.gameObject.AddComponent<LayoutElement>().minHeight = 20;

            CreatePlayerActionButton(row.transform, "Teleport", () =>
            {
                if (!PlayerActions.TryTeleportTo(player))
                    Log.LogWarning("[Astolfo] Teleport failed (invalid target or missing rig).");
            }, player.IsLocal);

            CreatePlayerActionButton(row.transform, "Copy name", () => PlayerActions.CopyNickname(player), false);
            CreatePlayerActionButton(row.transform, "Log info", () => PlayerActions.LogPlayerInfo(player), false);

            _playerRows.Add(row.gameObject);
        }

        private void CreatePlayerActionButton(Transform parent, string label, UnityAction onClick, bool disabled)
        {
            var go = new GameObject("Btn_" + label);
            go.AddComponent<RectTransform>().SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = disabled ? AstolfoTheme.TabInactive : AstolfoTheme.AccentDeep;
            var btn = go.AddComponent<Button>();
            btn.interactable = !disabled;
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            CreateText(go.transform, label, 15, AstolfoTheme.TextPrimary, TextAnchor.MiddleCenter);
            StretchFull(go.transform.GetChild(0).GetComponent<RectTransform>());

            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 108;
            le.minHeight = 36;
        }

        private RectTransform CreateRowPanel(RectTransform parent, string name, float height)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            StretchTopStretchWidth(rt);
            var img = go.AddComponent<Image>();
            img.color = AstolfoTheme.WindowBg;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.flexibleWidth = 1f;
            return rt;
        }

        private RectTransform CreatePanel(Transform parent, string name, Vector2 size, Color bg)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = bg;
            return rt;
        }

        private Text CreateText(Transform parent, string value, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("Text");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.text = value;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchTopStretchWidth(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y);
        }

        private static void SetAnchoredHeader(RectTransform rt, float x, float y)
        {
            StretchFull(rt);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(704f, rt.sizeDelta.y);
        }

        private static void SetAnchoredCenter(RectTransform rt, Vector2 pos)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
        }

        private static void AddOutline(GameObject go, Color c, float distance)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = c;
            o.effectDistance = new Vector2(distance, -distance);
        }
    }
}
