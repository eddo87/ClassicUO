using System;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NineSliceGump : Gump
    {
        private Texture2D _customTexture;
        private Rectangle[] _slices = new Rectangle[9];
        private bool _resizable;
        private readonly int _minWidth;
        private readonly int _minHeight;
        private int _borderSize;
        private bool _isDragging;
        private Point _dragStartMousePos;
        private Point _dragStartPosition;
        private Point _dragStartSize;
        private ResizeCorner _dragCorner;
        private ResizeCorner _hoveredCorner;
        private int _cornerSize = 10;
        private ushort _hue;

        public ushort Hue
        {
            get => _hue;
            set => _hue = value;
        }

        public bool Resizable
        {
            get => _resizable;
            set => _resizable = value;
        }

        public int BorderSize
        {
            get => _borderSize;
            set
            {
                _borderSize = value;
                CalculateSlices();
            }
        }

        private enum ResizeCorner
        {
            None,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        public NineSliceGump(World world, int x, int y, int width, int height, Texture2D texture, int borderSize, bool resizable = true, int minWidth = 50, int minHeight = 50) : base(world, 0, 0)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _customTexture = texture;
            _borderSize = borderSize;
            _resizable = resizable;
            _minWidth = minWidth;
            _minHeight = minHeight;

            CalculateSlices();
            AcceptMouseInput = true;
            CanMove = true;
            WantUpdateSize = false;
        }

        private void CalculateSlices()
        {
            if (_customTexture == null) return;

            int texWidth = _customTexture.Width;
            int texHeight = _customTexture.Height;

            _slices[0] = new Rectangle(0, 0, _borderSize, _borderSize);
            _slices[1] = new Rectangle(_borderSize, 0, texWidth - _borderSize * 2, _borderSize);
            _slices[2] = new Rectangle(texWidth - _borderSize, 0, _borderSize, _borderSize);

            _slices[3] = new Rectangle(0, _borderSize, _borderSize, texHeight - _borderSize * 2);
            _slices[4] = new Rectangle(_borderSize, _borderSize, texWidth - _borderSize * 2, texHeight - _borderSize * 2);
            _slices[5] = new Rectangle(texWidth - _borderSize, _borderSize, _borderSize, texHeight - _borderSize * 2);

            _slices[6] = new Rectangle(0, texHeight - _borderSize, _borderSize, _borderSize);
            _slices[7] = new Rectangle(_borderSize, texHeight - _borderSize, texWidth - _borderSize * 2, _borderSize);
            _slices[8] = new Rectangle(texWidth - _borderSize, texHeight - _borderSize, _borderSize, _borderSize);
        }

        protected virtual void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
        }

        private ResizeCorner GetCornerAtPosition(int localX, int localY)
        {
            if (!_resizable) return ResizeCorner.None;

            if (localX <= _cornerSize && localY <= _cornerSize)
                return ResizeCorner.TopLeft;
            if (localX >= Width - _cornerSize && localY <= _cornerSize)
                return ResizeCorner.TopRight;
            if (localX <= _cornerSize && localY >= Height - _cornerSize)
                return ResizeCorner.BottomLeft;
            if (localX >= Width - _cornerSize && localY >= Height - _cornerSize)
                return ResizeCorner.BottomRight;

            return ResizeCorner.None;
        }

        protected override void OnMouseEnter(int x, int y)
        {
            base.OnMouseEnter(x, y);
            if (!_resizable) return;
            _hoveredCorner = GetCornerAtPosition(x, y);
        }

        protected override void OnMouseOver(int x, int y)
        {
            base.OnMouseOver(x, y);
            if (!_resizable) return;
            _hoveredCorner = GetCornerAtPosition(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            _hoveredCorner = ResizeCorner.None;
            base.OnMouseExit(x, y);
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            base.OnMouseDown(x, y, button);

            if (!_resizable || button != MouseButtonType.Left) return;

            _dragCorner = GetCornerAtPosition(x, y);

            if (_dragCorner != ResizeCorner.None && Mouse.LButtonPressed && MouseIsOver)
            {
                _isDragging = true;
                _dragStartMousePos = Mouse.Position;
                _dragStartSize = new Point(Width, Height);
                _dragStartPosition = new Point(X, Y);
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (button == MouseButtonType.Left)
            {
                _isDragging = false;
                _dragCorner = ResizeCorner.None;
            }
        }

        public override void Update()
        {
            base.Update();

            if (_isDragging && _dragCorner != ResizeCorner.None && Mouse.LButtonPressed)
            {
                Point currentMousePos = Mouse.Position;

                int deltaX = currentMousePos.X - _dragStartMousePos.X;
                int deltaY = currentMousePos.Y - _dragStartMousePos.Y;

                int oldWidth = Width;
                int oldHeight = Height;
                int newWidth = Width;
                int newHeight = Height;
                int newX = X;
                int newY = Y;

                switch (_dragCorner)
                {
                    case ResizeCorner.TopLeft:
                        newWidth = Math.Max(Math.Max(_borderSize * 2, _minWidth), _dragStartSize.X - deltaX);
                        newHeight = Math.Max(Math.Max(_borderSize * 2, _minHeight), _dragStartSize.Y - deltaY);
                        newX = _dragStartPosition.X + (_dragStartSize.X - newWidth);
                        newY = _dragStartPosition.Y + (_dragStartSize.Y - newHeight);
                        break;
                    case ResizeCorner.TopRight:
                        newWidth = Math.Max(Math.Max(_borderSize * 2, _minWidth), _dragStartSize.X + deltaX);
                        newHeight = Math.Max(Math.Max(_borderSize * 2, _minHeight), _dragStartSize.Y - deltaY);
                        newX = _dragStartPosition.X;
                        newY = _dragStartPosition.Y + (_dragStartSize.Y - newHeight);
                        break;
                    case ResizeCorner.BottomLeft:
                        newWidth = Math.Max(Math.Max(_borderSize * 2, _minWidth), _dragStartSize.X - deltaX);
                        newHeight = Math.Max(Math.Max(_borderSize * 2, _minHeight), _dragStartSize.Y + deltaY);
                        newX = _dragStartPosition.X + (_dragStartSize.X - newWidth);
                        newY = _dragStartPosition.Y;
                        break;
                    case ResizeCorner.BottomRight:
                        newWidth = Math.Max(Math.Max(_borderSize * 2, _minWidth), _dragStartSize.X + deltaX);
                        newHeight = Math.Max(Math.Max(_borderSize * 2, _minHeight), _dragStartSize.Y + deltaY);
                        newX = _dragStartPosition.X;
                        newY = _dragStartPosition.Y;
                        break;
                }

                if (newWidth != Width || newHeight != Height || newX != X || newY != Y)
                {
                    X = newX;
                    Y = newY;
                    Width = newWidth;
                    Height = newHeight;
                    OnResize(oldWidth, oldHeight, newWidth, newHeight);
                }
            }

            if (_isDragging && !Mouse.LButtonPressed)
            {
                _isDragging = false;
                _dragCorner = ResizeCorner.None;
            }
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (IsDisposed || !IsVisible || _customTexture == null || _customTexture.IsDisposed)
            {
                return false;
            }

            float layerDepth = layerDepthRef;
            Rectangle[] slices = _slices;
            Texture2D texture = _customTexture;
            int width = Width;
            int height = Height;
            int borderSize = _borderSize;
            ushort hue = Hue;
            float alpha = Alpha;

            renderLists.AddGumpNoAtlas(batcher =>
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(hue, false, alpha, true);

                // Top row
                batcher.Draw(texture, new Rectangle(x, y, borderSize, borderSize), slices[0], hueVector, layerDepth);
                batcher.Draw(texture, new Rectangle(x + borderSize, y, width - borderSize * 2, borderSize), slices[1], hueVector, layerDepth);
                batcher.Draw(texture, new Rectangle(x + width - borderSize, y, borderSize, borderSize), slices[2], hueVector, layerDepth);

                // Middle row
                batcher.Draw(texture, new Rectangle(x, y + borderSize, borderSize, height - borderSize * 2), slices[3], hueVector, layerDepth);
                batcher.Draw(texture, new Rectangle(x + borderSize, y + borderSize, width - borderSize * 2, height - borderSize * 2), slices[4], hueVector, layerDepth);
                batcher.Draw(texture, new Rectangle(x + width - borderSize, y + borderSize, borderSize, height - borderSize * 2), slices[5], hueVector, layerDepth);

                // Bottom row
                batcher.Draw(texture, new Rectangle(x, y + height - borderSize, borderSize, borderSize), slices[6], hueVector, layerDepth);
                batcher.Draw(texture, new Rectangle(x + borderSize, y + height - borderSize, width - borderSize * 2, borderSize), slices[7], hueVector, layerDepth);
                batcher.Draw(texture, new Rectangle(x + width - borderSize, y + height - borderSize, borderSize, borderSize), slices[8], hueVector, layerDepth);

                return true;
            });

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }
    }
}
