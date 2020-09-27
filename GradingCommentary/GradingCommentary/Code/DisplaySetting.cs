using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GradingCommentary.Code
{
    [Serializable]
    public class DisplaySetting : DisplayAdapter
    {
        [XmlAttribute] public string Name;
        [XmlAttribute] public bool Default;
        [XmlElement] public string Header;
        [XmlElement] public string Footer;
        [XmlElement] public string RatioBody;
        [XmlElement] public string Body;
        [XmlElement] public string Note;

        [XmlIgnore]
        protected override string HeaderFormat
        {
            get { return Header ?? string.Empty; }
        }
        [XmlIgnore]
        protected override string FooterFormat
        {
            get { return Footer ?? string.Empty; }
        }
        [XmlIgnore]
        protected override string RatioBodyFormat
        {
            get { return RatioBody ?? string.Empty; }
        }
        [XmlIgnore]
        protected override string BodyFormat
        {
            get { return Body ?? string.Empty; }
        }

        protected override string NoteFormat
        {
            get { return Note ?? string.Empty; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Display Setting[{0}]: ", Name);
            if (!string.IsNullOrWhiteSpace(Header))
            {
                sb.Append("Header ");
            }
            if (!string.IsNullOrWhiteSpace(Body))
            {
                sb.Append("Body ");
            }
            if (!string.IsNullOrWhiteSpace(RatioBody))
            {
                sb.Append("RatioBody ");
            }
            if (!string.IsNullOrWhiteSpace(Footer))
            {
                sb.Append("Footer ");
            }

            if (!string.IsNullOrWhiteSpace(Note))
            {
                sb.Append("Note ");
            }
            return sb.ToString();
        }

        public static DisplayAdapter GetDisplayAdapter(string name)
        {
            return Settings.Default.Display[name];
        }

        public static DisplayAdapter DefaultDisplayAdapter
        {
            get { return Settings.Default.Display.FirstOrDefault(x => x.Default) ?? Settings.Default.Display.First(); }
        }
    }
}
