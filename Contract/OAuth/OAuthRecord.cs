using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.OAuth
{
    [DataContract(Name = "OAuthTokenType")]
    internal enum OAuthTokenType
    {
        [EnumMember(Value = "UserPwd")]
        UserPwd = 0,
        [EnumMember(Value = "AppSecret")]
        AppSecret = 1,
        [EnumMember(Value = "AppCert")]
        AppCert = 2
    }

    [DataContract(Name = "OAuthRecord")]
    internal class OAuthRecord
    {
        [DataMember(Name = "Active")]
        internal bool Active { get; set; }

        [DataMember(Name = "Directory")]
        internal string Directory { get; set; }

        [DataMember(Name = "AuthName")]
        internal string AuthName { get; set; }

        [DataMember(Name = "TokenType")]
        internal OAuthTokenType TokenType { get; set; }

        [DataMember(Name = "AccessToken")]
        internal string AccessToken { get; set; }

        [DataMember(Name = "ExpiresOn")]
        internal DateTime ExpiresOn { get; set; }

        [DataMember(Name = "LoginUrl")]
        internal string LoginUrl { get; set; }

        [DataMember(Name = "DynamicsUrl")]
        internal string DynamicsUrl { get; set; }

        [DataMember(Name = "Content")]
        internal string Content { get; set; }

        public override string ToString()
        {
            return $"AuthName:{AuthName}, Directory:{Directory}, Active:{Active}, TokenType:{TokenType}, ExpiresOn:{ExpiresOn}, LoginUrl:{LoginUrl}, DynamicsUrl:{DynamicsUrl}, AccessToken:{(AccessToken == null ? null : "****")}, Content:{(Content == null ? null : "****")}";
        }
    }
}
