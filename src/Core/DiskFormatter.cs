using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AutoUSBToolkit.Models;

namespace AutoUSBToolkit.Core
{
    public static class DiskFormatter
    {
        #region fmifs.dll Interop

        public enum CallbackCommand : uint
        {
            Progress = 0,
            DoneWithStructure = 1,
            IncompatibleFileSystem = 3,
            AccessDenied = 4,
            MediaWriteProtected = 5,
            VolumeInUse = 6,
            CantQuickFormat = 7,
            Done = 8,
            BadSectors = 9
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool FmifsCallback(
            CallbackCommand command,
            uint subAction,
            IntPtr actionInfo);

        [DllImport("fmifs.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void FormatEx(
            string driveRoot,
            uint mediaFlag,
            string format,
            string label,
            bool quickFormat,
            uint clusterSize,
            FmifsCallback callback);

        private const uint FMIFS_HARDDISK = 0x0C;

        #endregion

        public static bool IsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public static bool RelaunchAsAdministrator()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Process.GetCurrentProcess().MainModule.FileName,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
                System.Windows.Application.Current.Shutdown();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Could not elevate to Administrator: " + ex.Message);
                return false;
            }
        }

        public static async Task<bool> FormatAsync(
            FormatOptions options,
            UsbDriveInfo driveInfo,
            IProgress<int> progress)
        {
            return await Task.Run(() =>
            {
                return ExecuteFormat(options, driveInfo, progress);
            });
        }

        private static bool ExecuteFormat(
            FormatOptions options,
            UsbDriveInfo driveInfo,
            IProgress<int> progress)
        {
            string root = options.DriveLetter.TrimEnd('\\');
            string rootWithSlash = root + "\\";

            // Safety guard: Protect system Windows drive
            string sysRoot = Path.GetPathRoot(Environment.SystemDirectory).TrimEnd('\\');
            if (string.Equals(root, sysRoot, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Error("Safety lock triggered: System drive cannot be formatted!");
                return false;
            }

            Logger.Info(string.Format("Preparing format for drive {0} ({1}, Cluster: {2} B, MBR: {3})...",
                root, options.FileSystem, options.ClusterSize, options.EnforceMbr));

            if (progress != null) progress.Report(5);

            // Step 1: If EnforceMbr is requested and drive is GPT or multi-partition, convert to MBR first via Diskpart
            if (options.EnforceMbr && (driveInfo != null && driveInfo.PartitionStyle == "GPT"))
            {
                Logger.Warn(string.Format("Drive {0} is currently GPT. Converting to MBR partition style for car compatibility...", root));
                bool mbrOk = ConvertToMbrViaDiskpart(driveInfo, options, progress);
                if (mbrOk)
                {
                    Logger.Success("Drive successfully repartitioned as MBR!");
                    if (progress != null) progress.Report(100);
                    return true;
                }
                else
                {
                    Logger.Warn("Diskpart MBR conversion had warnings, attempting standard format...");
                }
            }

            // Step 2: If FileSystem is FAT32 and drive size > 32 GB, standard Windows FormatEx fails.
            // Use Direct FAT32 Engine!
            bool isDriveLarge = (driveInfo != null && driveInfo.TotalSizeBytes > 32L * 1024 * 1024 * 1024);
            bool isFat32 = string.Equals(options.FileSystem, "FAT32", StringComparison.OrdinalIgnoreCase);

            if (options.UseDirectFat32Engine || (isFat32 && isDriveLarge))
            {
                Logger.Info("Drive capacity exceeds 32 GB. Activating Direct FAT32 Car Engine...");
                bool directOk = Fat32Engine.FormatDrive(root, options.VolumeLabel, options.ClusterSize, progress);
                if (directOk) return true;
                Logger.Warn("Direct FAT32 engine encountered an issue. Falling back to Windows Format API...");
            }

            // Step 3: Standard Windows fmifs.dll FormatEx
            Logger.Info(string.Format("Executing Windows IFS Format on {0}...", rootWithSlash));

            bool formatSuccess = false;
            int lastPercent = 0;

            FmifsCallback callback = (command, subAction, actionInfo) =>
            {
                switch (command)
                {
                    case CallbackCommand.Progress:
                        if (actionInfo != IntPtr.Zero)
                        {
                            int percent = Marshal.ReadInt32(actionInfo);
                            if (percent != lastPercent)
                            {
                                lastPercent = percent;
                                if (progress != null) progress.Report(percent);
                            }
                        }
                        return true;

                    case CallbackCommand.Done:
                        if (actionInfo != IntPtr.Zero)
                        {
                            byte resultByte = Marshal.ReadByte(actionInfo);
                            formatSuccess = (resultByte == 1);
                        }
                        return true;

                    case CallbackCommand.VolumeInUse:
                        // Force dismount volume if in use
                        Logger.Info("Volume in use, forcing dismount...");
                        return true;

                    case CallbackCommand.AccessDenied:
                        Logger.Error("Access denied during format. Ensure application is run as Administrator.");
                        return false;

                    case CallbackCommand.IncompatibleFileSystem:
                        Logger.Error("Incompatible filesystem requested.");
                        return false;

                    default:
                        return true;
                }
            };

            try
            {
                uint cluster = options.ClusterSize; // 0 for default, 32768 for 32KB
                string label = string.IsNullOrEmpty(options.VolumeLabel) ? "CAR_USB" : options.VolumeLabel;

                FormatEx(
                    rootWithSlash,
                    FMIFS_HARDDISK,
                    options.FileSystem,
                    label,
                    options.QuickFormat,
                    cluster,
                    callback);

                if (formatSuccess)
                {
                    Logger.Success(string.Format("Successfully formatted {0} as {1}!", root, options.FileSystem));
                    if (progress != null) progress.Report(100);
                    return true;
                }
                else
                {
                    Logger.Warn("Windows IFS Format did not complete. Attempting Diskpart fallback...");
                    return FormatViaDiskpart(root, options, progress);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("FormatEx API error: " + ex.Message + ". Falling back to Diskpart...");
                return FormatViaDiskpart(root, options, progress);
            }
        }

        private static bool ConvertToMbrViaDiskpart(UsbDriveInfo driveInfo, FormatOptions options, IProgress<int> progress)
        {
            if (driveInfo.PhysicalDriveIndex < 0) return false;

            string diskNum = driveInfo.PhysicalDriveIndex.ToString();
            string letter = options.DriveLetter.TrimEnd('\\').Replace(":", "");
            string clusterStr = options.ClusterSize > 0 ? string.Format("unit={0}", options.ClusterSize) : "";
            string fs = options.FileSystem.ToLowerInvariant();
            string label = string.IsNullOrEmpty(options.VolumeLabel) ? "CAR_USB" : options.VolumeLabel;

            string script = string.Format(
                "select disk {0}\r\nclean\r\nconvert mbr\r\ncreate partition primary\r\nactive\r\nformat fs={1} quick {2} label=\"{3}\"\r\nassign letter={4}\r\n",
                diskNum, fs, clusterStr, label, letter);

            return RunDiskpartScript(script, progress);
        }

        private static bool FormatViaDiskpart(string driveLetter, FormatOptions options, IProgress<int> progress)
        {
            string letter = driveLetter.TrimEnd('\\').Replace(":", "");
            string clusterStr = options.ClusterSize > 0 ? string.Format("unit={0}", options.ClusterSize) : "";
            string fs = options.FileSystem.ToLowerInvariant();
            string label = string.IsNullOrEmpty(options.VolumeLabel) ? "CAR_USB" : options.VolumeLabel;

            string script = string.Format(
                "select volume {0}\r\nformat fs={1} quick {2} label=\"{3}\"\r\n",
                letter, fs, clusterStr, label);

            return RunDiskpartScript(script, progress);
        }

        private static bool RunDiskpartScript(string scriptContent, IProgress<int> progress)
        {
            try
            {
                string tempScript = Path.Combine(Path.GetTempPath(), "autousb_diskpart_" + Guid.NewGuid().ToString("N") + ".txt");
                File.WriteAllText(tempScript, scriptContent, Encoding.ASCII);

                if (progress != null) progress.Report(30);

                var psi = new ProcessStartInfo
                {
                    FileName = "diskpart.exe",
                    Arguments = string.Format("/s \"{0}\"", tempScript),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    string error = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    try { File.Delete(tempScript); } catch { }

                    if (proc.ExitCode == 0)
                    {
                        Logger.Success("Diskpart operation completed successfully.");
                        if (progress != null) progress.Report(90);
                        return true;
                    }
                    else
                    {
                        Logger.Error("Diskpart exited with code " + proc.ExitCode + ": " + output + " " + error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Diskpart execution failed: " + ex.Message);
                return false;
            }
        }
    }
}

