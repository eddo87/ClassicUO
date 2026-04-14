using System;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps
{
    internal enum FileSelectorType
    {
        File,
        Directory
    }

    /// <summary>
    /// Minimal file selector stub. Uses a simple approach since ClassicUO
    /// does not have TazUO's full file browser implementation.
    /// The callback receives the path from an internal StbTextBox input.
    /// </summary>
    internal class FileSelector : Gump
    {
        private const int WIDTH = 400, HEIGHT = 120;

        public FileSelector(World world, FileSelectorType type, string initialPath, string[] fileExtensions, Action<string> onFileSelected, string title)
            : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            X = 200;
            Y = 200;
            Width = WIDTH;
            Height = HEIGHT;

            Add(new Controls.AlphaBlendControl(0.85f) { Width = WIDTH, Height = HEIGHT });
            Add(new Controls.Label(title ?? "File Browser", true, 0xFFFF) { X = 10, Y = 5 });

            var textbox = new Controls.StbTextBox(0xFF, -1, WIDTH - 20, true, FontStyle.BlackBorder, 0xFFFF)
            {
                X = 10, Y = 30, Width = WIDTH - 20, Height = 25
            };
            if (!string.IsNullOrEmpty(initialPath))
                textbox.SetText(initialPath);
            Add(textbox);

            var okButton = new Controls.NiceButton(10, 65, 80, 25, ButtonAction.Activate, "OK") { IsSelectable = false };
            okButton.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left && !string.IsNullOrWhiteSpace(textbox.Text))
                {
                    onFileSelected?.Invoke(textbox.Text);
                    Dispose();
                }
            };
            Add(okButton);

            var cancelButton = new Controls.NiceButton(100, 65, 80, 25, ButtonAction.Activate, "Cancel") { IsSelectable = false };
            cancelButton.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                    Dispose();
            };
            Add(cancelButton);
        }

        public static void ShowFileBrowser(World world, FileSelectorType type, string initialPath, string[] fileExtensions, Action<string> onFileSelected, string title = "File Browser")
        {
            var gump = new FileSelector(world, type, initialPath, fileExtensions, onFileSelected, title);
            Managers.UIManager.Add(gump);
        }
    }
}
