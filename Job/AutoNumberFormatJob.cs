using D365.Community.Ps.Automation.Service;
using System;
using System.Text;

namespace D365.Community.Ps.Automation.Job
{
    internal static class AutoNumberFormatJob
    {
        // /api/data/v9.2/EntityDefinitions(LogicalName='account')/Attributes(LogicalName='msdyn_partynumber')
        internal static bool Execute(Contract.Config.AutoNumberFormats formats)
        {
            var result = true;
            foreach (var format in formats.Formats)
            {
                var attribute = $"{PsAutomation.ApiUrl}/EntityDefinitions(LogicalName='{format.Entity}')/Attributes(LogicalName='{format.Attribute}')";
                result = Client.Get<Contract.Content.EntityAttribute>(attribute, out var response) && result;
                if (response.StatusCode == 200)
                {
                    var content = (Contract.Content.EntityAttribute)response.Content;
                    if (!string.Equals(format.Format, content.AutoNumberFormat, StringComparison.InvariantCulture))
                    {
                        var typeBody = new StringBuilder().OpenJson();
                        typeBody.AppendValue("AutoNumberFormat", format.Format);
                        typeBody.CloseJson();
                        result = Client.Patch(attribute, typeBody.ToString()) && result;
                    }
                }
            }
            return result;
        }

    }
}
