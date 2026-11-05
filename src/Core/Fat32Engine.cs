using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AutoUSBToolkit.Core
{
    /// <summary>
    /// Direct FAT32 Volume Formatter Engine.
    /// Breaks Windows' artificial 32 GB FAT32 limit and formats USB drives up to 2 TB
    /// with 32 KB clusters for automotive head unit compatibility.
    /// </summary>
    public static class Fat32Engine
    {
        #region Native Imports

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;

        private const uint FSCTL_LOCK_VOLUME = 0x00090018;
        private const uint FSCTL_UNLOCK_VOLUME = 0x0009001C;
        private const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;
        private const uint IOCTL_DISK_GET_DRIVE_GEOMETRY = 0x00070000;
        private const uint IOCTL_DISK_GET_PARTITION_INFO_EX = 0x00070048;

        [StructLayout(LayoutKind.Sequential)]
        private struct DISK_GEOMETRY
        {
            public long Cylinders;
            public int MediaType;
            public int TracksPerCylinder;
            public int SectorsPerTrack;
            public int BytesPerSector;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PARTITION_INFORMATION_EX
        {
            public int PartitionStyle;
            public long StartingOffset;
            public long PartitionLength;
            public int PartitionNumber;
            public bool RewritePartition;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 112)]
            public byte[] MbrOrGpt;
        }

        #endregion

        public static bool FormatDrive(
            string driveLetter,
            string volumeLabel,
            uint desiredClusterSize,
            IProgress<int> progress)
        {
            string root = driveLetter.TrimEnd('\\');
            string volumePath = @"\\.\" + root;

            Logger.Info(string.Format("Initiating Direct FAT32 Format on {0}...", root));

            IntPtr hVol = CreateFile(
                volumePath,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (hVol.ToInt64() == -1)
            {
                int err = Marshal.GetLastWin32Error();
                Logger.Error(string.Format("Failed to open volume {0} for exclusive access (Error {1}). Please run as Administrator.", root, err));
                return false;
            }

            try
            {
                uint dummy = 0;

                // 1. Lock Volume
                if (!DeviceIoControl(hVol, FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out dummy, IntPtr.Zero))
                {
                    Logger.Warn("Could not lock volume exclusively, attempting dismount...");
                }

                // 2. Dismount Volume
                if (!DeviceIoControl(hVol, FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out dummy, IntPtr.Zero))
                {
                    Logger.Warn("Dismount volume warning (proceeding).");
                }

                // 3. Determine Sector Size and Partition Size
                uint bytesPerSector = 512;
                long volumeSizeBytes = 0;

                int geomSize = Marshal.SizeOf(typeof(DISK_GEOMETRY));
                IntPtr geomPtr = Marshal.AllocHGlobal(geomSize);
                if (DeviceIoControl(hVol, IOCTL_DISK_GET_DRIVE_GEOMETRY, IntPtr.Zero, 0, geomPtr, (uint)geomSize, out dummy, IntPtr.Zero))
                {
                    var geom = (DISK_GEOMETRY)Marshal.PtrToStructure(geomPtr, typeof(DISK_GEOMETRY));
                    if (geom.BytesPerSector > 0)
                        bytesPerSector = (uint)geom.BytesPerSector;
                }
                Marshal.FreeHGlobal(geomPtr);

                // Get Partition Length
                int partSize = Marshal.SizeOf(typeof(PARTITION_INFORMATION_EX));
                IntPtr partPtr = Marshal.AllocHGlobal(partSize);
                if (DeviceIoControl(hVol, IOCTL_DISK_GET_PARTITION_INFO_EX, IntPtr.Zero, 0, partPtr, (uint)partSize, out dummy, IntPtr.Zero))
                {
                    var part = (PARTITION_INFORMATION_EX)Marshal.PtrToStructure(partPtr, typeof(PARTITION_INFORMATION_EX));
                    volumeSizeBytes = part.PartitionLength;
                }
                Marshal.FreeHGlobal(partPtr);

                if (volumeSizeBytes <= 0)
                {
                    // Fallback to DriveInfo total size
                    var di = new DriveInfo(root);
                    volumeSizeBytes = di.TotalSize;
                }

                long totalSectors = volumeSizeBytes / bytesPerSector;
                Logger.Info(string.Format("Volume size: {0:0.0} GB ({1} sectors of {2} bytes)",
                    (double)volumeSizeBytes / (1024 * 1024 * 1024), totalSectors, bytesPerSector));

                // 4. Calculate Sectors Per Cluster (Default to 32 KB clusters for automotive)
                uint sectorsPerCluster = desiredClusterSize > 0 ? (desiredClusterSize / bytesPerSector) : 64;
                if (sectorsPerCluster == 0) sectorsPerCluster = 64; // 32 KB
                if (sectorsPerCluster > 128) sectorsPerCluster = 128; // 64 KB max

                ushort reservedSectors = 32;
                byte numFats = 2;

                // Estimate FAT Size
                // Total clusters ~ (totalSectors - reservedSectors) / sectorsPerCluster
                // Each cluster entry is 4 bytes in FAT32
                long fatSectors = (((totalSectors / sectorsPerCluster) * 4) + bytesPerSector - 1) / bytesPerSector;
                uint sectorsPerFat = (uint)fatSectors;

                uint rootCluster = 2;
                ushort fsInfoSector = 1;
                ushort backupBootSector = 6;

                if (progress != null) progress.Report(15);

                // Clean label (Max 11 chars uppercase ASCII)
                string safeLabel = string.IsNullOrEmpty(volumeLabel) ? "CAR_USB" : volumeLabel.Trim().ToUpperInvariant();
                if (safeLabel.Length > 11) safeLabel = safeLabel.Substring(0, 11);
                safeLabel = safeLabel.PadRight(11, ' ');

                uint volumeSerial = (uint)(DateTime.Now.Ticks & 0xFFFFFFFF);

                // 5. Build Sector 0: Volume Boot Record (VBR)
                byte[] vbr = new byte[bytesPerSector];
                vbr[0] = 0xEB; vbr[1] = 0x58; vbr[2] = 0x90; // JMP instruction
                Encoding.ASCII.GetBytes("MSWIN4.1").CopyTo(vbr, 3); // OEM Name

                // BPB
                BitConverter.GetBytes((ushort)bytesPerSector).CopyTo(vbr, 11);
                vbr[13] = (byte)sectorsPerCluster;
                BitConverter.GetBytes(reservedSectors).CopyTo(vbr, 14);
                vbr[16] = numFats;
                BitConverter.GetBytes((ushort)0).CopyTo(vbr, 17); // Root entries = 0 for FAT32
                BitConverter.GetBytes((ushort)0).CopyTo(vbr, 19); // Total sectors 16 = 0
                vbr[21] = 0xF8; // Media descriptor (Fixed/Removable hard disk)
                BitConverter.GetBytes((ushort)0).CopyTo(vbr, 22); // Sectors per FAT 16 = 0
                BitConverter.GetBytes((ushort)63).CopyTo(vbr, 24); // Sectors per track
                BitConverter.GetBytes((ushort)255).CopyTo(vbr, 26); // Heads
                BitConverter.GetBytes((uint)0).CopyTo(vbr, 28); // Hidden sectors
                BitConverter.GetBytes((uint)totalSectors).CopyTo(vbr, 32); // Total sectors 32

                // FAT32 Extended BPB
                BitConverter.GetBytes(sectorsPerFat).CopyTo(vbr, 36);
                BitConverter.GetBytes((ushort)0).CopyTo(vbr, 40); // Ext flags
                BitConverter.GetBytes((ushort)0).CopyTo(vbr, 42); // FS Version 0.0
                BitConverter.GetBytes(rootCluster).CopyTo(vbr, 44);
                BitConverter.GetBytes(fsInfoSector).CopyTo(vbr, 48);
                BitConverter.GetBytes(backupBootSector).CopyTo(vbr, 50);

                vbr[64] = 0x80; // Drive number
                vbr[66] = 0x29; // Extended boot signature
                BitConverter.GetBytes(volumeSerial).CopyTo(vbr, 67);
                Encoding.ASCII.GetBytes(safeLabel).CopyTo(vbr, 71);
                Encoding.ASCII.GetBytes("FAT32   ").CopyTo(vbr, 82);

                // Boot signature at end of sector
                vbr[510] = 0x55;
                vbr[511] = 0xAA;

                // 6. Build Sector 1: FSInfo Sector
                byte[] fsInfo = new byte[bytesPerSector];
                BitConverter.GetBytes((uint)0x41615252).CopyTo(fsInfo, 0); // Lead signature "RRaA"
                BitConverter.GetBytes((uint)0x61417272).CopyTo(fsInfo, 484); // Struct signature "rrAa"
                uint freeClusters = (uint)((totalSectors - reservedSectors - (numFats * sectorsPerFat)) / sectorsPerCluster);
                BitConverter.GetBytes(freeClusters - 1).CopyTo(fsInfo, 488); // Free cluster count
                BitConverter.GetBytes((uint)3).CopyTo(fsInfo, 492); // Next free cluster
                fsInfo[510] = 0x55;
                fsInfo[511] = 0xAA;

                if (progress != null) progress.Report(30);

                // Write Sector 0 (VBR), Sector 1 (FSInfo), and Sector 6 (Backup VBR)
                using (var fs = new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle(hVol, false), FileAccess.Write))
                {
                    // Write VBR at 0
                    fs.Position = 0;
                    fs.Write(vbr, 0, (int)bytesPerSector);

                    // Write FSInfo at sector 1
                    fs.Position = 1 * bytesPerSector;
                    fs.Write(fsInfo, 0, (int)bytesPerSector);

                    // Write Backup VBR at sector 6
                    fs.Position = backupBootSector * bytesPerSector;
                    fs.Write(vbr, 0, (int)bytesPerSector);

                    // Write Backup FSInfo at sector 7
                    fs.Position = (backupBootSector + 1) * bytesPerSector;
                    fs.Write(fsInfo, 0, (int)bytesPerSector);

                    if (progress != null) progress.Report(50);

                    // 7. Write FAT 1 and FAT 2 Initial Entries
                    byte[] fatInit = new byte[bytesPerSector];
                    // Cluster 0: Media descriptor + reserved bits: 0x0FFFFFF8
                    BitConverter.GetBytes((uint)0x0FFFFFF8).CopyTo(fatInit, 0);
                    // Cluster 1: Clean volume / EOF: 0x0FFFFFFF
                    BitConverter.GetBytes((uint)0x0FFFFFFF).CopyTo(fatInit, 4);
                    // Cluster 2: Root Directory EOF marker: 0x0FFFFFFF
                    BitConverter.GetBytes((uint)0x0FFFFFFF).CopyTo(fatInit, 8);

                    long fat1Offset = (long)reservedSectors * bytesPerSector;
                    long fat2Offset = fat1Offset + ((long)sectorsPerFat * bytesPerSector);

                    // Write initial FAT1 block
                    fs.Position = fat1Offset;
                    fs.Write(fatInit, 0, (int)bytesPerSector);

                    // Write initial FAT2 block
                    fs.Position = fat2Offset;
                    fs.Write(fatInit, 0, (int)bytesPerSector);

                    if (progress != null) progress.Report(75);

                    // 8. Zero out Root Directory (Cluster 2)
                    long dataStartOffset = fat1Offset + ((long)numFats * sectorsPerFat * bytesPerSector);
                    byte[] rootClusterData = new byte[sectorsPerCluster * bytesPerSector];

                    // Write Volume Label entry into root directory if requested
                    if (!string.IsNullOrEmpty(safeLabel.Trim()))
                    {
                        Encoding.ASCII.GetBytes(safeLabel).CopyTo(rootClusterData, 0);
                        rootClusterData[11] = 0x08; // ATTR_VOLUME_ID
                    }

                    fs.Position = dataStartOffset;
                    fs.Write(rootClusterData, 0, rootClusterData.Length);
                    fs.Flush();
                }

                if (progress != null) progress.Report(95);

                // Unlock Volume
                DeviceIoControl(hVol, FSCTL_UNLOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out dummy, IntPtr.Zero);

                Logger.Success(string.Format("Successfully formatted {0} as FAT32 with {1} KB clusters ({2:0.0} GB)!",
                    root, sectorsPerCluster * bytesPerSector / 1024, (double)volumeSizeBytes / (1024 * 1024 * 1024)));

                if (progress != null) progress.Report(100);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Direct FAT32 formatting failed: " + ex.Message);
                return false;
            }
            finally
            {
                CloseHandle(hVol);
            }
        }
    }
}

