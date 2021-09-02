using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using Zen.Barcode;

namespace barApp.Models
{
    public class Printer
    {
        private PrintDocument Document;
        private Font TitleFont;
        private Font SubtitleFont;
        private Font BodyFont;
        private Font BodyFontBold;
        private Brush Brush;
        private float Width;
        private int Padding;
        private int ySeparation;
        private float yCurrent;

        public readonly KeyValuePair<string, string> EmptyListElement = new KeyValuePair<string, string>(string.Empty, string.Empty);

        public Printer(string printerRoute = "")
        {
            Document = new PrintDocument();
            Document.PrinterSettings.PrinterName = string.IsNullOrWhiteSpace(printerRoute) ? Document.PrinterSettings.PrinterName : printerRoute;
            TitleFont = new Font("Calibri", 18, FontStyle.Bold);
            SubtitleFont = new Font("Calibri", 12, FontStyle.Bold);
            BodyFont = new Font("Calibri", 10);
            BodyFontBold = new Font("Calibri", 10, FontStyle.Bold);
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
                _event.Graphics.DrawString(title.ToUpper(), TitleFont, Brush, Width / 2, yCurrent, new StringFormat() { Alignment = StringAlignment.Center });
                yCurrent += _event.Graphics.MeasureString(title.ToUpper(), TitleFont).Height;
            };
        }

        public void AddSubtitle(string title)
        {
            AddLine();
            AddSpace(0.5f);

            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                _event.Graphics.DrawString(title.ToUpper(), SubtitleFont, Brush, Width / 2, yCurrent, new StringFormat() { Alignment = StringAlignment.Center });
                yCurrent += _event.Graphics.MeasureString(title.ToUpper(), SubtitleFont).Height;
            };
        }

        public void AddString(string text, bool bold = false, StringAlignment alignment = StringAlignment.Near)
        {
            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                StringFormat stringFormat = new StringFormat() { Alignment = alignment, Trimming = StringTrimming.Character };
                _event.Graphics.DrawString(text, (bold ? BodyFontBold : BodyFont), Brush, (alignment == StringAlignment.Center ? Width / 2 : Padding), yCurrent, stringFormat);

                yCurrent += _event.Graphics.MeasureString(text, (bold ? BodyFontBold : BodyFont)).Height;
            };
        }

        public void AddTable(string[] columns, string[][] data, bool hasHeader = false)
        {
            AddLine();

            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                float xGridSize = (Width - (Padding * 2)) / columns.Length;
                StringFormat stringFormat = new StringFormat { Trimming = StringTrimming.Character };

                for (int index = 0; index < columns.Length; index++)
                {
                    _event.Graphics.DrawString(columns[index], SubtitleFont, Brush, (Padding + (xGridSize * index)), yCurrent, stringFormat);
                }

                yCurrent += _event.Graphics.MeasureString(columns[0], BodyFont).Height;
                _event.Graphics.DrawLine(new Pen(Brush), Padding / 2, yCurrent, Width - (Padding / 2), yCurrent);
                yCurrent += 10;

                for (int row = 0; row < data.Length; row++)
                {
                    if (hasHeader)
                    {
                        _event.Graphics.DrawString(data[row][0], BodyFont, Brush, Padding, yCurrent, stringFormat);
                        yCurrent += _event.Graphics.MeasureString(data[row][0], BodyFont).Height - 5;
                    }

                    for (int valueIndex = (hasHeader ? 1 : 0); valueIndex < data[row].Length; valueIndex++)
                    {
                        _event.Graphics.DrawString(data[row][valueIndex], BodyFont, Brush, (Padding + (xGridSize * (valueIndex - (hasHeader ? 1 : 0)))), yCurrent, stringFormat);
                    }

                    yCurrent += _event.Graphics.MeasureString(columns.First(), BodyFont).Height + 5;
                }
            };

            AddLine();
        }

        public void AddTableDetails(IDictionary<string, string> data, int tableColumns)
        {
            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                StringFormat stringFormat = new StringFormat() { Trimming = StringTrimming.Character };
                float xGridSize = (Width - (Padding * 2)) / tableColumns;

                for (int index = 0; index < data.Count; index++)
                {
                    _event.Graphics.DrawString(data.ElementAt(index).Key + ":", BodyFontBold, Brush, (Padding + (xGridSize * (tableColumns - 2))), yCurrent, stringFormat);
                    _event.Graphics.DrawString(data.ElementAt(index).Value, BodyFont, Brush, (Padding + (xGridSize * (tableColumns - 1))), yCurrent, stringFormat);

                    yCurrent += _event.Graphics.MeasureString(data.ElementAt(index).Key, BodyFontBold).Height;
                }
            };
        }

        public void AddDescriptionList(IDictionary<string, string> data, int columns = 1)
        {
            float xGridSize = (Width - (Padding * 2)) / columns;

            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                for (int index = 0; index < data.Count; index++)
                {
                    float yBase = 1;

                    if (columns == 2) yBase = index % columns;
                    if (columns == 3)
                    {
                        int divisor = columns;
                        int modulus = index % divisor;
                        int fase = (index + 1) % divisor;

                        yBase = (modulus - fase) / (columns - 1);
                    }

                    float xBase = Padding + (xGridSize * (index % columns));
                    SizeF keySize = _event.Graphics.MeasureString(data.ElementAt(index).Key + ": ", BodyFontBold);

                    if (!string.IsNullOrEmpty(data.ElementAt(index).Key))
                    {
                        _event.Graphics.DrawString(data.ElementAt(index).Key + ": ", BodyFontBold, Brush, xBase, yCurrent);
                        _event.Graphics.DrawString(data.ElementAt(index).Value, BodyFont, Brush, (xBase + keySize.Width), yCurrent);
                    }

                    yCurrent += yBase * keySize.Height;
                }
            };
        }

        public void AddBarCode(string text)
        {
            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                Image barcode = BarcodeDrawFactory.GetSymbology(BarcodeSymbology.Code128).Draw(text, 36);
                _event.Graphics.DrawImage(barcode, (Width - barcode.Width) / 2, yCurrent);

                yCurrent += barcode.Height;
            };

            AddString(text, false, StringAlignment.Center);
        }

        public void AddLine()
        {
            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                _event.Graphics.DrawLine(new Pen(Brush), Padding / 2, yCurrent, Width - (Padding / 2), yCurrent);
            };
        }

        public void AddSpace(float modifier = 1)
        {
            Document.PrintPage += delegate (object sender, PrintPageEventArgs _event)
            {
                yCurrent += ySeparation * modifier;
            };
        }

        public void Print()
        {
            Document.Print();
        }
    }
}