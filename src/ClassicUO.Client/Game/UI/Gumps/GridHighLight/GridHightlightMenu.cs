using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    internal class GridHighlightMenu : NineSliceGump
    {
        private const int WIDTH = 420, HEIGHT = 500;
        private ScrollArea highlightSectionScroll;

        public GridHighlightMenu(World world, int x = 100, int y = 100) : base(world, x, y, WIDTH, HEIGHT, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BorderSize, true, WIDTH, HEIGHT)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            BuildGump();
        }

        protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            base.OnResize(oldWidth, oldHeight, newWidth, newHeight);
            BuildGump();
        }

        private void BuildGump()
        {
            Clear();
            int y = 0;
            int sectionWidth = Width - (BorderSize * 2);

            // Header section
            {
                Add(new AlphaBlendControl(0.85f) { X = BorderSize, Y = BorderSize, Width = sectionWidth, Height = 70 });

                Add(new Label("Grid highlighting settings", true, 0xffff) { X = BorderSize + 5, Y = BorderSize + 2 });
                Add(new Label("You can add object properties that you would like the grid to be highlighted for here.", true, 0xffff, sectionWidth - 15) { X = BorderSize + 5, Y = BorderSize + 20 });

                int btnY = BorderSize + 48;
                int btnX = BorderSize + 5;

                NiceButton _;
                Add(_ = new NiceButton(btnX, btnY, 60, 20, ButtonAction.Activate, "Add +") { IsSelectable = false });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        ProfileManager.CurrentProfile.GridHighlightSetup.Add(new GridHighlightSetupEntry());
                        GridHighlightData.AllConfigs = null;
                        BuildGump();
                    }
                };
                btnX += 65;

                Add(_ = new NiceButton(btnX, btnY, 60, 20, ButtonAction.Activate, "Export") { IsSelectable = false });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        ExportGridHighlightSettings(World);
                    }
                };
                btnX += 65;

                Add(_ = new NiceButton(btnX, btnY, 60, 20, ButtonAction.Activate, "Import") { IsSelectable = false });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        ImportGridHighlightSettings(World);
                        GridHighlightData.RecheckMatchStatus();
                    }
                };
                btnX += 65;

                Add(_ = new NiceButton(btnX, btnY, 60, 20, ButtonAction.Activate, "Configs") { IsSelectable = false });
                _.MouseUp += (s, e) =>
                {
                    if (e.Button == Input.MouseButtonType.Left)
                    {
                        UIManager.GetGump<GridHighlightConfig>()?.Dispose();
                        UIManager.Add(new GridHighlightConfig(World, 100, 100));
                    }
                };

                y = BorderSize + 75;
            }

            // Highlight entries scroll area
            Add(highlightSectionScroll = new ScrollArea(BorderSize, y, sectionWidth, Height - y - BorderSize, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

            y = 0;
            for (int i = 0; i < ProfileManager.CurrentProfile.GridHighlightSetup.Count; i++)
            {
                highlightSectionScroll.Add(NewAreaSection(i, y));
                y += 21;
            }
        }

        private Area NewAreaSection(int keyLoc, int y)
        {
            var pos = new Positioner(0, 0, 0, 0);
            var data = GridHighlightData.GetGridHighlightData(keyLoc);
            int areaWidth = highlightSectionScroll.Width - 18 - 15;
            var area = new Area() { Y = y, X = BorderSize };
            area.Width = areaWidth;
            area.Height = 150;
            y = 0;

            NiceButton colorButton;
            area.Add(colorButton = new NiceButton(0, y, 60, 20, ButtonAction.Activate, "Color") { IsSelectable = false });
            colorButton.SetTooltip("Select grid highlight color");
            colorButton.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    RGBColorPickerGump.Open(data.HighlightColor, selectedColor =>
                    {
                        data.HighlightColor = selectedColor;
                        data.Hue = (ushort)(selectedColor.R + (selectedColor.G << 8) + (selectedColor.B << 16));
                        GridHighlightData.RecheckMatchStatus();
                    });
                }
            };

            NiceButton _propertiesButton;
            area.Add(_propertiesButton = new NiceButton(0, y, 60, 20, ButtonAction.Activate, "Properties") { IsSelectable = false });
            _propertiesButton.MouseUp += (s, e) =>
           {
               if (e.Button == Input.MouseButtonType.Left)
               {
                   UIManager.GetGump<GridHighlightProperties>()?.Dispose();
                   UIManager.Add(new GridHighlightProperties(World, keyLoc, 100, 100));
               }
           };

            NiceButton _del;
            area.Add(_del = new NiceButton(0, y, 20, 20, ButtonAction.Activate, "X") { IsSelectable = false });
            _del.SetTooltip("Delete this highlight configuration");
            _del.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.Delete();
                    BuildGump();
                    GridHighlightData.RecheckMatchStatus();
                }
            };

            NiceButton _moveUp;
            area.Add(_moveUp = new NiceButton(0, y, 40, 20, ButtonAction.Activate, "Up") { IsSelectable = false });
            _moveUp.SetTooltip("Move this up in the list");
            _moveUp.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.Move(true);
                    GridHighlightData.AllConfigs = null;
                    BuildGump();
                }
            };

            NiceButton _moveDown;
            area.Add(_moveDown = new NiceButton(area.Width - 40, y, 40, 20, ButtonAction.Activate, "Down") { IsSelectable = false });
            _moveDown.SetTooltip("Move this down in the list");
            _moveDown.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.Move(false);
                    GridHighlightData.AllConfigs = null;
                    BuildGump();
                }
            };

            pos.PositionLeftOf(_moveUp, _moveDown);
            pos.PositionLeftOf(_del, _moveUp);
            pos.PositionLeftOf(_propertiesButton, _del);
            pos.PositionLeftOf(colorButton, _propertiesButton);

            InputField _name;
            area.Add(_name = new InputField(0x0BB8, 0xFF, 0xFFFF, true, colorButton.X - 7, 20)
            {
                X = 0,
                Y = y,
                AcceptKeyboardInput = true
            }
            );
            _name.SetText(data.Name);
            _name.TextChanged += (s, e) => data.Name = _name.Text;

            area.ForceSizeUpdate(false);
            return area;
        }

        private static void SaveProfile() => GridHighlightRules.SaveGridHighlightConfiguration();

        public static void Open(World world)
        {
            UIManager.GetGump<GridHighlightMenu>()?.Dispose();
            UIManager.Add(new GridHighlightMenu(world));
        }

        private static void ExportGridHighlightSettings(World world)
        {
            List<GridHighlightSetupEntry> data = ProfileManager.CurrentProfile.GridHighlightSetup;

            RunFileDialog(world, true, "Save grid highlight settings", file =>
            {
                if (Directory.Exists(file))
                {
                    file = Path.Combine(file, "highlights.json");
                }
                else if (!Path.HasExtension(file))
                {
                    file += ".json";
                }

                string json = JsonSerializer.Serialize(data, GridHighlightExportJsonContext.Default.ListGridHighlightSetupEntry);
                File.WriteAllText(file, json);
                GameActions.Print(world, $"Saved highlight export to: {file}");
            });
        }

        private static void ImportGridHighlightSettings(World world) => RunFileDialog(world, false, "Import grid highlight settings", file =>
                                                                                 {
                                                                                     try
                                                                                     {
                                                                                         if (!File.Exists(file))
                                                                                             return;

                                                                                         string json = File.ReadAllText(file);
                                                                                         List<GridHighlightSetupEntry> imported = JsonSerializer.Deserialize(json, GridHighlightExportJsonContext.Default.ListGridHighlightSetupEntry);
                                                                                         if (imported != null)
                                                                                         {
                                                                                             ProfileManager.CurrentProfile.GridHighlightSetup.AddRange(imported);
                                                                                             SaveProfile();
                                                                                             UIManager.GetGump<GridHighlightMenu>()?.Dispose();
                                                                                             UIManager.Add(new GridHighlightMenu(world));
                                                                                             GameActions.Print(world, $"Imported highlight config from: {file}");
                                                                                         }
                                                                                     }
                                                                                     catch (Exception ex)
                                                                                     {
                                                                                         GameActions.Print(world, "Error importing highlight config", Constants.HUE_ERROR);
                                                                                         Log.Error(ex.ToString());
                                                                                     }
                                                                                 });

        private static void RunFileDialog(World world, bool save, string title, Action<string> onResult) => FileSelector.ShowFileBrowser(world, save ? FileSelectorType.Directory : FileSelectorType.File, null, save ? null : new[] { "*.json" }, onResult, title);
    }

    // Source generator context for export/import JSON serialization (avoids reflection)
    [System.Text.Json.Serialization.JsonSerializable(typeof(List<GridHighlightSetupEntry>))]
    [System.Text.Json.Serialization.JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class GridHighlightExportJsonContext : System.Text.Json.Serialization.JsonSerializerContext
    {
    }
}
