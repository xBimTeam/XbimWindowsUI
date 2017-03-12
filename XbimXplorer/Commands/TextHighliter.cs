using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace XbimXplorer.Commands
{
    internal class TextHighliter
    {
        internal abstract class ReportComponent
        {
            internal abstract Block ToBlock();
        }

        internal class MultiPartBit : ReportComponent
        {
            Block _b = null;
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
                _b = new Paragraph(s);
                
            }
            internal override Block ToBlock()
            {
                return _b;
            }
        }

        internal class ReportBit : ReportComponent
        {
            string _textContent;
            Brush _textBrush;

            public ReportBit(string txt, Brush brsh = null)
            {
                _textContent = txt;
                _textBrush = brsh;
            }

            internal override Block ToBlock()
            {
                Regex re = new Regex(@"\[#\d+\]");
                Paragraph p = new Paragraph();

                var tmp = _textContent;

                while (!string.IsNullOrEmpty(tmp))
                {
                    var m = re.Match(tmp);
                    if (m.Success)
                    {
                        var pre = tmp.Substring(0, m.Index);
                        var link = m.Value;
                        tmp = tmp.Substring(m.Index + m.Length);
                        p.Inlines.Add(new Run(pre));
                        var h = new Hyperlink();
                        h.Inlines.Add(link);
                        h.IsEnabled = true;
                        // h.RequestNavigate += HOnRequestNavigate;
                        h.NavigateUri = new Uri("xbim://EntityLabel/" + link.Substring(2,link.Length-3));
                        p.Inlines.Add(h);
                    }
                    else
                    {
                        var run = new Run(tmp);
                        p.Inlines.Add(run);
                        tmp = "";
                    }
                }

                
                if (_textBrush != null)
                    p.Foreground = _textBrush;
                return p;
            }
        }

        List<ReportComponent> _bits = new List<ReportComponent>();

        internal void Append(string text, Brush color)
        {
            _bits.Add(new ReportBit(text, color));
        }

        public Brush DefaultBrush = null;

        internal void AppendFormat(string format, params object[] args)
        {
            _bits.Add(new ReportBit(
                string.Format(null, format, args),
                DefaultBrush
                ));
        }

        internal void Append(TextHighliter other)
        {
            _bits.AddRange(other._bits);
        }

        internal void Clear()
        {
            _bits = new List<ReportComponent>();
        }

        internal void DropInto(FlowDocument flowDocument)
        {
            flowDocument.Blocks.AddRange(ToBlocks());
        }

        private IEnumerable<Block> ToBlocks()
        {
            foreach (var reportComponent in _bits)
            {
                yield return reportComponent.ToBlock();
            }
        }

        internal void AppendSpans(string[] strings, Brush[] brushes)
        {
            MultiPartBit b = new MultiPartBit(strings, brushes);
            _bits.Add(b);
        }
    }
}
