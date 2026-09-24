using D365.Community.Ps.Automation.Contract.Content;
using System.Text;
using D365.Community.Ps.Automation.Service;
using System;
using System.IO;
using System.Security.Policy;

namespace D365.Community.Ps.Automation.Export
{
    internal static class FileAttachmentExport
    {
        internal static bool Execute(string directory, string prefix, string entityname, string entityid, string attribute)
        {
            var initializeBody = new StringBuilder()
                .OpenJson()
                .AppendObject("Target", $"{{ \"{entityname}id\": \"{entityid}\", \"@odata.type\": \"Microsoft.Dynamics.CRM.{entityname}\" }}")
                .AppendValue("FileAttributeName", attribute)
                .CloseJson();
            var result = Client.Post<InitializeFileBlocksDownload>($"{PsAutomation.ApiUrl}/InitializeFileBlocksDownload()", initializeBody.ToString(), out var initializeResponse);
            if (initializeResponse.StatusCode != 200) return result;
            var initializeContent = (InitializeFileBlocksDownload)initializeResponse.Content;

            var fileName = initializeContent.FileName;
            var fileContinuationToken = initializeContent.FileContinuationToken;
            var fileSizeInBytes = initializeContent.FileSizeInBytes;
            var fileBytes = new byte[fileSizeInBytes];

            long offset = 0;
            long blockSizeDownload = 4 * 1024 * 1024; // 4 MB

            // File size may be smaller than defined block size
            if (fileSizeInBytes < blockSizeDownload)
            {
                blockSizeDownload = fileSizeInBytes;
            }

            while (fileSizeInBytes > 0)
            {
                var downloadBody = new StringBuilder()
                    .OpenJson()
                    .AppendValue("Offset", offset)
                    .AppendValue("BlockLength", blockSizeDownload)
                    .AppendValue("FileContinuationToken", fileContinuationToken)
                    .CloseJson();
                result = Client.Post<DownloadBlock>($"{PsAutomation.ApiUrl}/DownloadBlock()", downloadBody.ToString(), out var downloadResponse) && result;
                if (downloadResponse.StatusCode != 200) return result;
                var downloadContent = (DownloadBlock)downloadResponse.Content;
                var bytes = downloadContent.GetBytes();
                Array.Copy(bytes, 0, fileBytes, offset, bytes.Length);
                fileSizeInBytes -= (int)blockSizeDownload;
                offset += blockSizeDownload;
            }

            var path = string.IsNullOrWhiteSpace(prefix)
                ? Path.Combine(directory, fileName)
                : Path.Combine(directory, $"{prefix}{fileName}");

            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERROR: {e.GetBaseException().Message}");
                    return false;
                }
            }
            try
            {
                Console.WriteLine($"INFO: File:{path}");
                File.WriteAllBytes(path, fileBytes);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"ERROR: {e.GetBaseException().Message}");
                return false;
            }

            return result;
        }
    }
}
