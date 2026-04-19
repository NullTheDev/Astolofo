using UnityEngine;

namespace AstolfoGorillaTagMenu.UI
{
    internal static class AstolfoTheme
    {
        public static readonly Color WindowBg = Hex("2A1824");
        public static readonly Color PanelBg = Hex("3D2436");
        public static readonly Color AccentPink = Hex("FF8FB8");
        public static readonly Color AccentDeep = Hex("E85D9A");
        public static readonly Color TextPrimary = Hex("FFF5FA");
        public static readonly Color TextMuted = Hex("D9AFC4");
        public static readonly Color TabInactive = Hex("5C3D52");
        public static readonly Color TabActive = Hex("FF9EC9");
        public static readonly Color Border = Hex("FFB7D5");

        public static Color Hex(string rrggbb)
        {
            if (ColorUtility.TryParseHtmlString("#" + rrggbb, out var c))
                return c;
            return Color.white;
        }
    }
}
