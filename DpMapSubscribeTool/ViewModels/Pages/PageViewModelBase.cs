using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.ViewModels.Pages
{
    public abstract class PageViewModelBase : ViewModelBase
    {
        public abstract string Title { get; }
    }
}
