using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class Area : Control
    {
        private bool drawBorder;
        private int hue;

        public Area(bool _drawBorder = true, int _borderHue = 0)
        {
            AcceptMouseInput = true;
            AcceptKeyboardInput = true;
            drawBorder = _drawBorder;
            hue = _borderHue;
        }

        public void ForceSizeUpdate(bool onlyIfLarger = true)
        {
            int maxW = Width;
            int maxH = Height;
            foreach (Control c in Children)
            {
                int right = c.X + c.Width;
                int bottom = c.Y + c.Height;
                if (right > maxW || !onlyIfLarger)
                    maxW = System.Math.Max(maxW, right);
                if (bottom > maxH || !onlyIfLarger)
                    maxH = System.Math.Max(maxH, bottom);
            }
            Width = maxW;
            Height = maxH;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);

            if (drawBorder)
            {
                float layerDepth = layerDepthRef;
                renderLists.AddGumpNoAtlas(batcher =>
                {
                    batcher.DrawRectangle(
                        SolidColorTextureCache.GetTexture(Color.Gray),
                        x, y,
                        Width - 1,
                        Height - 1,
                        ShaderHueTranslator.GetHueVector(hue),
                        layerDepth
                    );
                    return true;
                });
            }
            return true;
        }
    }
}
