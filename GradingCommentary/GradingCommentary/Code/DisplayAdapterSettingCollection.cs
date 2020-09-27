using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GradingCommentary.Code
{
    [Serializable]
    [XmlRoot("Display")]
    public class DisplayAdapterSettingCollection : ICollection<DisplaySetting>
    {
        [XmlArrayItem(typeof(DisplaySetting))]
        private readonly ICollection<DisplaySetting> _settings = new List<DisplaySetting>();

        public IEnumerator<DisplaySetting> GetEnumerator()
        {
            return _settings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(DisplaySetting item)
        {
            _settings.Add(item);
        }

        public void Clear()
        {
            _settings.Clear();
        }

        public bool Contains(DisplaySetting item)
        {
            return _settings.Contains(item);
        }

        public void CopyTo(DisplaySetting[] array, int arrayIndex)
        {
            _settings.CopyTo(array, arrayIndex);
        }

        public bool Remove(DisplaySetting item)
        {
            return _settings.Remove(item);
        }

        public int Count { get { return _settings.Count; } }
        public bool IsReadOnly { get { return _settings.IsReadOnly; } }

        public DisplaySetting this[int i]
        {
            get { return ((List<DisplaySetting>) _settings)[i]; }
        }

        public DisplaySetting this[string name]
        {
            get { return _settings.FirstOrDefault(x => x.Name == name); }
        }
    }
}
