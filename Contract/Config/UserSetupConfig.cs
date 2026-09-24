using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class UserSet : Caller
    {
        [DataMember(Name = "users", IsRequired = true)]
        internal List<UserSetup> Users { get; set; } = new List<UserSetup>();
    }

    [DataContract]
    internal sealed class UserSetup
    {
        [DataMember(Name = "user_name", IsRequired = true)]
        internal string UserName { get; set; }

        [DataMember(Name = "business_unit_id", IsRequired = true)]
        internal Guid BusinessUnitId { get; set; }

        [DataMember(Name = "roles", IsRequired = true)]
        internal List<UserRoles> UserRoles { get; set; } = new List<UserRoles>();

        [DataMember(Name = "teams", IsRequired = true)]
        internal List<UserTeam> UserTeams { get; set; } = new List<UserTeam>();

        public override string ToString()
        {
            return $"User: {UserName}(BusinessUnit({BusinessUnitId:D});Roles[{string.Join(",", UserRoles)}];Teams[{string.Join(",", UserTeams)}])";
        }
    }

    [DataContract]
    internal sealed class UserRoles
    {
        [DataMember(Name = "business_unit_id", IsRequired = true)]
        internal Guid BusinessUnitId { get; set; }

        [DataMember(Name = "security_roles", IsRequired = true)]
        internal List<string> SecurityRoles { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"BusinessUnit({BusinessUnitId:D});SecurityRoles[{string.Join("|", SecurityRoles)}]";
        }
    }

    [DataContract]
    internal sealed class UserTeam
    {
        [DataMember(Name = "team_id", IsRequired = true)]
        internal Guid TeamId { get; set; }

        public override string ToString()
        {
            return $"Team({TeamId:D})";
        }
    }
}
