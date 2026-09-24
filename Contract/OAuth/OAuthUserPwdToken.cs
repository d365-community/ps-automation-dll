using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.OAuth
{
    [DataContract(Name = "OAuthUserPwdToken")]
    internal class OAuthUserPwdToken
    {
        [DataMember(Name = "expires_in")]
        internal int ExpiresIn { get; set; }

        [DataMember(Name = "access_token")]
        internal string AccessToken { get; set; }
    }
}
