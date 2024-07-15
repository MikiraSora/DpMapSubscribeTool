using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.Controls
{
    public class ListItemTemplate
    {
        public ListItemTemplate(Type type, string label, string iconSymbol)
        {
            ModelType = type;
            Label = label ?? type.Name.Replace("PageViewModel", "");
            IconSymbol = iconSymbol;
        }

        public string Label { get; }
        public Type ModelType { get; }
        public string IconSymbol { get; }
    }
}
