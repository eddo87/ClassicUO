using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI
{
    internal enum PositionerDirection
    {
        Vertical,
        Horizontal
    }

    internal class Positioner
    {
        public int TopPadding;
        public int LeftPadding;
        public int BlankLineHeight;
        public int IndentWidth;
        public PositionerDirection Direction;

        public int X, Y, LastY, LastHeight, LastX, LastWidth;

        private bool _tableMode = false;
        private int _tableColumns = 0;
        private int _currentColumn = 0;
        private int _tableStartX;
        private int _tableStartY;
        private int _columnWidth;
        private int _columnPadding;
        private int _maxRowHeight = 0;

        public Positioner(int leftPadding = 2, int topPadding = 5, int blankLineHeight = 20, int indentation = 40, PositionerDirection direction = PositionerDirection.Vertical)
        {
            Direction = direction;
            LeftPadding = leftPadding;
            TopPadding = topPadding;
            BlankLineHeight = blankLineHeight;
            IndentWidth = indentation;

            Y = LastY = TopPadding;
            X = LastX = LeftPadding;
        }

        public void BlankLine()
        {
            if (_tableMode)
                EndTable();

            if (Direction == PositionerDirection.Vertical)
            {
                LastY = Y;
                Y += BlankLineHeight;
            }
            else
            {
                LastX = X;
                X += BlankLineHeight;
            }
        }

        public void StartTable(int columns, int columnWidth = 0, int columnPadding = 10)
        {
            if (_tableMode)
                EndTable();

            _tableMode = true;
            _tableColumns = columns;
            _currentColumn = 0;
            _tableStartX = X;
            _tableStartY = Y;
            _columnWidth = columnWidth;
            _columnPadding = columnPadding;
            _maxRowHeight = 0;
        }

        public void EndTable()
        {
            if (!_tableMode) return;

            _tableMode = false;

            if (_maxRowHeight > 0)
                Y = _tableStartY + _maxRowHeight + TopPadding;

            LastY = Y;
            X = _tableStartX;

            _tableColumns = 0;
            _currentColumn = 0;
            _maxRowHeight = 0;
        }

        public void NextTableRow()
        {
            if (!_tableMode) return;

            _tableStartY += _maxRowHeight + TopPadding;
            Y = _tableStartY;
            _currentColumn = 0;
            _maxRowHeight = 0;
        }

        public Control Position(Control c)
        {
            if (_tableMode)
                return PositionInTable(c);

            c.X = X;
            c.Y = Y;

            if (Direction == PositionerDirection.Vertical)
            {
                LastY = Y;
                Y += c.Height + TopPadding;
                LastHeight = c.Height;
            }
            else
            {
                LastX = X;
                X += c.Width + LeftPadding;
                LastWidth = c.Width;
            }

            return c;
        }

        private Control PositionInTable(Control c)
        {
            int alignedX = _tableStartX + (_currentColumn * (_columnWidth + _columnPadding));

            c.X = alignedX;
            c.Y = _tableStartY;

            _maxRowHeight = Math.Max(_maxRowHeight, c.Height);

            _currentColumn++;

            if (_currentColumn >= _tableColumns)
                NextTableRow();

            return c;
        }

        public Control PositionRightOf(Control c, Control other, int padding = 5)
        {
            c.Y = other.Y;
            c.X = other.X + other.Width + padding;
            return c;
        }

        public Control PositionLeftOf(Control c, Control other, int padding = 5)
        {
            c.Y = other.Y;
            c.X = other.X - c.Width - padding;
            return c;
        }

        public Control PositionExact(Control c, int x, int y)
        {
            c.X = x;
            c.Y = y;
            return c;
        }

        public void Reset()
        {
            if (_tableMode)
                EndTable();

            X = LastX = LeftPadding;
            Y = LastY = TopPadding;
        }
    }
}
