using System;
using System.Collections.Generic;
using System.Text;

namespace Skinde.Build
{
    public static class GitHelper
    {
        public static string GetCurrentBranchName()
        {
            try
            {
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --abbrev-ref HEAD",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = new System.Diagnostics.Process { StartInfo = processStartInfo })
                {
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd().Trim();
                    var error = process.StandardError.ReadToEnd().Trim();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Git command failed with error: {error}");
                    }
                    return output;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get current Git branch name.", ex);
            }
        }
    }
}
