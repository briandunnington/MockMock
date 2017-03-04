using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    public interface IWidgetService
    {
        int MinimumQuantity { get; set; }
        string GetWidgetDescription(string name, bool truncate);
        void ValidateWidgets(DateTime timestamp);
    }
}
