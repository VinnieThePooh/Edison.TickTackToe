using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Edison.TickTackToe.Web.Startup))]
namespace Edison.TickTackToe.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
