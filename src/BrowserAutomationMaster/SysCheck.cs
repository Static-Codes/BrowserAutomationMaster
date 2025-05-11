using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserAutomationMaster
{
    public partial class SysCheck
    {

        SysCheck(string[] args) {
            Verify64BitWindows();
            VerifyRootDrive(args);
        }

        static void Verify64BitWindows()
        {
            if (!Environment.Is64BitOperatingSystem)
            {
                Errors.WriteErrorAndExit("BAM Manager (BAMM) was developed for 64 bit windows operating systems.\n\nIf you're reading this message, your current system is incompatible with this application.\n\nFor more information please visit the link below:\n\nhttps://answers.microsoft.com/en-us/windows/forum/all/what-is-the-advantage-of-going-to-64-biti-have-32/dea5fbb6-4b53-4c66-a0e6-70e76f934d79", 1);
            }
        }

        static void VerifyRootDrive(string[] args)
        {

            if (args.Contains("--ignore-drive-root")) { return; }
            string? rootDrive = Path.GetPathRoot(AppContext.BaseDirectory);

            if (rootDrive == null || !rootDrive.StartsWith("C:"))
            {
                Errors.WriteErrorAndExit("BAMM Manager (BAMM) was developed to be ran on the C: drive.\n\nRunning this application on a different drive caused too many unforseeable bugs, so i've decided to prevent it from happening all together.\n\nIf you are contributing to development, you can bypass this restriction by passing the argument '--ignore-drive-root'.", 1);
            }
        }

    }
}
