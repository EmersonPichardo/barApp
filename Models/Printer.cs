using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;

namespace barApp.Models
{
    public class Printer
    {
        private PrintDocument Document;
        private Font TitleFont;
        private Font BodyTitleFont;
        private Font BodyFont;
        private Font BodyFontBold;
        private Brush Brush;
        private float Width;
        private int Padding;
        private int ySeparation;
        private float yCurrent;

        public Printer()
        {
            Document = new PrintDocument();
            TitleFont = new Font("Calibri", 22, FontStyle.Bold);
            BodyTitleFont = new Font("Calibri", 14, FontStyle.Bold);
            BodyFont = new Font("Calibri", 12);
            BodyFontBold = new Font("Calibri", 12, FontStyle.Bold);
            Brush = new SolidBrush(Color.Black);
            Width = Document.DefaultPageSettings.PrintableArea.Width;
            Padding = 30;
            ySeparation = 10;
            yCurrent = Padding;
        }

        public void AddTitle(string title)
        {
            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                _event.Graphics.DrawString(title, TitleFont, Brush, Width / 2, yCurrent, new StringFormat() { Alignment = StringAlignment.Center });
                yCurrent += _event.Graphics.MeasureString(title, TitleFont).Height + (ySeparation * 4);
            };
        }

        public void AddString(string paragraph, bool bold = false, StringAlignment alignment = StringAlignment.Near)
        {
            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                _event.Graphics.DrawString(paragraph, (bold ? BodyFontBold : BodyFont), Brush, Padding, yCurrent, new StringFormat() { Alignment = alignment });
            };
        }

        public void AddLine()
        {
            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                _event.Graphics.DrawLine(new Pen(Brush), Padding / 2, yCurrent, Width - (Padding / 2), yCurrent);
            };
        }

        public void AddSpace()
        {
            yCurrent += Padding;
        }

        public void AddTable(string[] columns, string[][] data)
        {
            AddLine();

            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                float xGridSize = (Width - (Padding * 2)) / columns.Length;

                for (int index = 0; index < columns.Length; index++)
                {
                    _event.Graphics.DrawString(columns[index].ToUpper(), BodyTitleFont, Brush, (Padding + (xGridSize * index)), yCurrent);
                }

                yCurrent += _event.Graphics.MeasureString(columns[0], BodyFont).Height;
                _event.Graphics.DrawLine(new Pen(Brush), Padding / 2, yCurrent, Width - (Padding / 2), yCurrent);
                yCurrent += ySeparation;

                for (int row = 0; row < data.Length; row++)
                {
                    for (int valueIndex = 0; valueIndex < data[row].Length; valueIndex++)
                    {
                        _event.Graphics.DrawString(data[row][valueIndex], BodyFont, Brush, (Padding + (xGridSize * valueIndex)), yCurrent);
                    }

                    yCurrent += row < data.Length - 1 ? _event.Graphics.MeasureString(columns[0], BodyFont).Height + ySeparation : 0;
                }

                yCurrent += _event.Graphics.MeasureString(columns[0], BodyFont).Height + (ySeparation * 4);
            };
        }

        public void AddDescriptionList(Dictionary<string, string> data)
        {
            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                for (int index = 0; index < data.Count; index++)
                {
                    _event.Graphics.DrawString(data.ElementAt(index).Key + ": ", BodyFontBold, Brush, Padding, yCurrent);

                    float x = _event.Graphics.MeasureString(data.ElementAt(index).Key + ": ", BodyFontBold).Width;
                    _event.Graphics.DrawString(data.ElementAt(index).Value, BodyFont, Brush, x + Padding, yCurrent);

                    yCurrent += _event.Graphics.MeasureString(data.ElementAt(index).Key, BodyFontBold).Height;
                }

                yCurrent += _event.Graphics.MeasureString(data.ElementAt(0).Key, BodyFontBold).Height + ySeparation;
            };
        }

        public void Print()
        {
            Document.Print();
        }
    }
}