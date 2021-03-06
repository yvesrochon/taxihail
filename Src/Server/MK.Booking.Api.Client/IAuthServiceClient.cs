#region

using System.Threading.Tasks;
using apcurium.MK.Booking.Api.Contract.Security;

#endregion

namespace apcurium.MK.Booking.Api.Client
{
    public interface IAuthServiceClient
    {
        Task CheckSession();

		Task<AuthenticationData> Authenticate(string email, string password);
        Task<AuthenticationData> AuthenticateTwitter(string twitterId);
		Task<AuthenticationData> AuthenticateFacebook (string facebookId);
    }
}