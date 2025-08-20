using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.Build.Framework;

namespace Skinde.Build
{
    public class UpdateDirectoryBuildProps : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string FilePath { get; set; }

        public override bool Execute()
        {
            try
            {
                var branchName = GitHelper.GetCurrentBranchName();
                if (string.IsNullOrEmpty(branchName))
                {
                    Log.LogError("Failed to get the current branch name.");
                    return false;
                }

                Log.LogMessage(MessageImportance.High, $"Current branch: {branchName}");

                if (!branchName.StartsWith("release/", StringComparison.OrdinalIgnoreCase))
                {
                    Log.LogMessage(MessageImportance.High, "Not a release branch. No changes made.");
                    return true;
                }

                // Extract the version from the branch name
                var versionFromBranch = branchName.Substring("release/".Length);
                Log.LogMessage(MessageImportance.High, $"Version from branch: {versionFromBranch}");

                if (!File.Exists(FilePath))
                {
                    Log.LogError($"File not found: {FilePath}");
                    return false;
                }

                var doc = XDocument.Load(FilePath);
                var versionPrefixElement = doc.Root?.Element("PropertyGroup")?.Element("VersionPrefix");
                if (versionPrefixElement == null)
                {
                    Log.LogError("VersionPrefix element not found in the XML file.");
                    return false;
                }

                Log.LogMessage(MessageImportance.High, $"Version from {FilePath}: {versionPrefixElement.Value}");

                if (versionPrefixElement.Value != versionFromBranch)
                {
                    versionPrefixElement.Value = versionFromBranch;
                    doc.Save(FilePath);
                    Log.LogMessage(MessageImportance.High, $"Version updated to {versionFromBranch}");
                }
                else
                {
                    Log.LogMessage(MessageImportance.High, "Version is already up to date.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }
    }
}