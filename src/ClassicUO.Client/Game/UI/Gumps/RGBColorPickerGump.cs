using System;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Minimal RGB color picker gump stub. Provides basic color selection functionality
    /// needed by the GridHighLight system. Can be expanded later with a full visual picker.
    /// </summary>
    internal class RGBColorPickerGump : Gump
    {
        private const int WIDTH = 250, HEIGHT = 150;
        private readonly Action<Color> _onColorSelected;
        private readonly StbTextBox _hexInput;

        private RGBColorPickerGump(Color initialColor, Action<Color> onColorSelected)
            : base(Client.Game.UO.World, 0, 0)
        {
            _onColorSelected = onColorSelected;
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            X = 200;
            Y = 200;
            Width = WIDTH;
            Height = HEIGHT;

            Add(new AlphaBlendControl(0.85f) { Width = WIDTH, Height = HEIGHT });
            Add(new Label("Enter hex color (#RRGGBB):", true, 0xFFFF) { X = 10, Y = 10 });

            _hexInput = new StbTextBox(0xFF, 9, WIDTH - 20, true, FontStyle.BlackBorder, 0xFFFF)
            {
                X = 10,
                Y = 35,
                Width = WIDTH - 20,
                Height = 25
            };
            string hex = $"#{initialColor.R:X2}{initialColor.G:X2}{initialColor.B:X2}";
            _hexInput.SetText(hex);
            Add(_hexInput);

            var okButton = new NiceButton(10, 70, 80, 25, ButtonAction.Activate, "OK") { IsSelectable = false };
            okButton.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    Color color = ParseHexColor(_hexInput.Text, initialColor);
                    _onColorSelected?.Invoke(color);
                    Dispose();
                }
            };
            Add(okButton);

            var cancelButton = new NiceButton(100, 70, 80, 25, ButtonAction.Activate, "Cancel") { IsSelectable = false };
            cancelButton.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                    Dispose();
            };
            Add(cancelButton);
        }

        private static Color ParseHexColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            hex = hex.TrimStart('#');
            if (hex.Length != 6) return fallback;

            try
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return new Color(r, g, b);
            }
            catch
            {
                return fallback;
            }
        }

        public static void Open(Color initialColor, Action<Color> onColorSelected)
        {
            UIManager.GetGump<RGBColorPickerGump>()?.Dispose();
            UIManager.Add(new RGBColorPickerGump(initialColor, onColorSelected));
        }
    }
}
