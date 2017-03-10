using System.Web;
using System.Web.Mvc;
using Edison.TickTackToe.Web.Infrastructure.Attributes;

namespace Edison.TickTackToe.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new InternationalizationAttribute());
        }
    }
}
