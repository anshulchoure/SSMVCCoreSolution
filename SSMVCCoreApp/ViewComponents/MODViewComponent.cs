using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace SSMVCCoreApp.ViewComponents
{
    public class MODViewComponent : ViewComponent
    {
        public MODViewComponent(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public Task<IViewComponentResult> InvokeAsync()
        {
            var result = Configuration["MOD"];
            return Task.FromResult<IViewComponentResult>(View("Default", result));
        }
    }
}
