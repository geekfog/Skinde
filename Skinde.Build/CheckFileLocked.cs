using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class CheckFileLocked : Task
{
    [Required]
    public string FilePath { get; set; }

    [Output]
    public bool IsLocked { get; set; }

    public override bool Execute()
    {
        try
        {
            using (FileStream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Close();
            }
            IsLocked = false;
        }
        catch (IOException)
        {
            IsLocked = true;
        }
        return true;
    }
}