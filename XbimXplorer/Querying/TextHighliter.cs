using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

namespace XbimXplorer.Querying
{
    internal class TextHighliter
    {
        internal abstract class ReportComponent
        {
            internal abstract Block ToBlock();
        }

        internal class MultiPartBit : ReportComponent
        {
            Block b = null;
            public MultiPartBit(string[] strings, Brush[] brushes)
            {
                int iC = Math.Min(strings.Length, brushes.Length);
                Span s = new Span();
                for (int i = 0; i < iC; i++)
                {
                    Span s2 = new Span(new Run(strings[i]));
                    s2.Foreground = brushes[i];
                    s.Inlines.Add(s2);
                }
                b = new Paragraph(s);
                
            }
            internal override Block ToBlock()
            {
                return b;
            }
        }

        internal class ReportBit : ReportComponent
        {
            string TextContent;
            Brush textBrush;

            public ReportBit(string txt, Brush brsh = null)
            {
                TextContent = txt;
                textBrush = brsh;
            }

            internal override Block ToBlock()
            {
                Paragraph p = new Paragraph(new Run(TextContent));
                if (textBrush != null)
                    p.Foreground = textBrush;
                return p;
            }
        }

        List<ReportComponent> Bits = new List<ReportComponent>();

        internal void Append(string text, Brush color)
        {
            Bits.Add(new ReportBit(text, color));
        }

        public Brush DefaultBrush = null;

        internal void AppendFormat(string format, params object[] args)
        {
            Bits.Add(new ReportBit(
                string.Format(null, format, args),
                DefaultBrush
                ));
        }

        internal void Append(TextHighliter other)
        {
            Bits.AddRange(other.Bits);
        }

        internal void Clear()
        {
            Bits = new List<ReportComponent>();
        }

        internal void DropInto(System.Windows.Documents.FlowDocument flowDocument)
        {
            flowDocument.Blocks.AddRange(this.ToBlocks());
        }

        private IEnumerable<Block> ToBlocks()
        {
            foreach (var item in Bits)
            {
                yield return item.ToBlock();
            }
        }

        internal void AppendSpans(string[] strings, Brush[] brushes)
        {
            MultiPartBit b = new MultiPartBit(strings, brushes);
            Bits.Add(b);
            
        }
    }
}
