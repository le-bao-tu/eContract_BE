using System;
using System.Collections.Generic;
using System.Text;

namespace NetCore.Business
{
    public class LayerModel
    {
        public string Logo { get; set; }
        public string Image { get; set; }
        public string Text { get; set; }
        public Dictionary<string, string> Styles { get; set; }
    }

    public class DictionaryStyleKey
    {
        public const string Font = "Font";
        public const string Color = "Color";
        public const string Height = "Height";
        public const string Width = "Width";
    }
}
