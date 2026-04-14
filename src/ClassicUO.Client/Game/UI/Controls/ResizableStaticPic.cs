using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ResizableStaticPic : Control
    {
        private uint _graphic;
        private ushort _hue;
        private Vector3 _hueVector = ShaderHueTranslator.GetHueVector(0, false, 1);

        public ushort Hue
        {
            get => _hue;
            set
            {
                _hue = value;
                _hueVector = ShaderHueTranslator.GetHueVector(_hue, false, 1);
            }
        }

        public uint Graphic
        {
            get => _graphic;
            set => _graphic = value;
        }

        public bool DrawBorder { get; set; }

        public ResizableStaticPic(uint graphic, int width, int height)
        {
            _graphic = graphic;
            Width = width;
            Height = height;
            WantUpdateSize = false;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;

            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(_graphic);
            Rectangle artBounds = Client.Game.UO.Arts.GetRealArtBounds(_graphic);

            var texture = artInfo.Texture;
            if (texture == null)
            {
                return false;
            }

            var drawSize = new Point(Width, Height);
            var drawOffset = new Point(0, 0);

            if (artBounds.Width < Width)
            {
                drawSize.X = artBounds.Width;
                drawOffset.X = (Width >> 1) - (drawSize.X >> 1);
            }

            if (artBounds.Height < Height)
            {
                drawSize.Y = artBounds.Height;
                drawOffset.Y = (Height >> 1) - (drawSize.Y >> 1);
            }

            var sourceRect = artInfo.UV;
            Vector3 hueVector = _hueVector;
            bool drawBorder = DrawBorder;
            int w = Width;
            int h = Height;

            renderLists.AddGumpWithAtlas
            (
                (batcher) =>
                {
                    batcher.Draw
                    (
                        texture,
                        new Rectangle
                        (
                            x + drawOffset.X,
                            y + drawOffset.Y,
                            drawSize.X,
                            drawSize.Y
                        ),
                        new Rectangle
                        (
                            sourceRect.X + artBounds.X,
                            sourceRect.Y + artBounds.Y,
                            artBounds.Width,
                            artBounds.Height
                        ),
                        hueVector,
                        layerDepth
                    );

                    if (drawBorder)
                    {
                        batcher.DrawRectangle
                        (
                            SolidColorTextureCache.GetTexture(Color.Gray),
                            x,
                            y,
                            w - 1,
                            h - 1,
                            hueVector,
                            layerDepth
                        );
                    }

                    return true;
                }
            );

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }
    }
}
