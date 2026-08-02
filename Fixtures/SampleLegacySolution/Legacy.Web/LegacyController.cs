using System.Web;
using System.Web.Security;

namespace Legacy.Web;

public class LegacyController
{
    public bool IsAdmin()
    {
        return HttpContext.Current.User.IsInRole("Admin");
    }

    public void SignOut()
    {
        FormsAuthentication.SignOut();
    }
}