using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using AutoUSBToolkit.Models;

namespace AutoUSBToolkit.Core
{
    public static class DriveDetector
    {
        #region Win32 Native Interop

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

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetDiskFreeSpaceW(
            string lpRootPathName,
            out uint lpSectorsPerCluster,
            out uint lpBytesPerSector,
            out uint lpNumberOfFreeClusters,
            out uint lpTotalNumberOfClusters);

        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;

        private const uint IOCTL_STORAGE_GET_DEVICE_NUMBER = 0x002D1080;
        private const uint IOCTL_STORAGE_QUERY_PROPERTY = 0x002D1400;
        private const uint IOCTL_DISK_GET_DRIVE_LAYOUT_EX = 0x00070050;

        [StructLayout(LayoutKind.Sequential)]
        private struct STORAGE_DEVICE_NUMBER
        {
            public int DeviceType;
            public int DeviceNumber;
            public int PartitionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STORAGE_PROPERTY_QUERY
        {
            public int PropertyId;
            public int QueryType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] AdditionalParameters;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STORAGE_DEVICE_DESCRIPTOR
        {
            public uint Version;
            public uint Size;
            public byte DeviceType;
            public byte DeviceTypeModifier;
            public byte RemovableMedia;
            public byte CommandQueueing;
            public uint VendorIdOffset;
            public uint ProductIdOffset;
            public uint ProductRevisionOffset;
            public uint SerialNumberOffset;
            public int BusType; // 7 = Usb
            public uint RawPropertiesLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] RawDeviceProperties;
        }

        #endregion

        public static List<UsbDriveInfo> GetAvailableDrives(bool showAllDrives = false)
        {
            var result = new List<UsbDriveInfo>();

            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (var d in drives)
                {
                    if (!d.IsReady) continue;

                    string root = d.Name.TrimEnd('\\'); // e.g. "E:"
                    string sysDrive = Path.GetPathRoot(Environment.SystemDirectory).TrimEnd('\\');

                    // System drive (C:) is protected - do not list unless explicitly requested in advanced mode
                    if (!showAllDrives && string.Equals(root, sysDrive, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var info = InspectDrive(d, root);
                    if (info == null) continue;

                    if (showAllDrives || info.IsRemovable || info.IsUsb)
                    {
                        result.Add(info);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to enumerate drives: " + ex.Message);
            }

            return result;
        }

        private static UsbDriveInfo InspectDrive(DriveInfo d, string root)
        {
            try
            {
                var info = new UsbDriveInfo();
                info.DriveLetter = root;
                info.VolumeLabel = d.VolumeLabel;
                info.FileSystem = d.DriveFormat;
                info.TotalSizeBytes = d.TotalSize;
                info.FreeSizeBytes = d.TotalFreeSpace;
                info.IsRemovable = (d.DriveType == DriveType.Removable);
                info.PartitionStyle = "MBR"; // Default assumption for removable
                info.PhysicalDriveIndex = -1;
                info.PartitionNumber = 1;
                info.DeviceModel = "Removable Storage";

                // 1. Get Cluster Size
                uint spc, bps, nfc, tnc;
                if (GetDiskFreeSpaceW(d.Name, out spc, out bps, out nfc, out tnc))
                {
                    info.ClusterSizeBytes = spc * bps;
                }

                // 2. Query Storage Device Properties & Device Number
                string volumeHandlePath = @"\\.\" + root;
                IntPtr hVol = CreateFile(
                    volumeHandlePath,
                    0, // Query access
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    0,
                    IntPtr.Zero);

                if (hVol.ToInt64() != -1)
                {
                    try
                    {
                        // Get Device Number (PhysicalDriveN)
                        int sdnSize = Marshal.SizeOf(typeof(STORAGE_DEVICE_NUMBER));
                        IntPtr sdnPtr = Marshal.AllocHGlobal(sdnSize);
                        uint bytesRet = 0;

                        if (DeviceIoControl(hVol, IOCTL_STORAGE_GET_DEVICE_NUMBER, IntPtr.Zero, 0, sdnPtr, (uint)sdnSize, out bytesRet, IntPtr.Zero))
                        {
                            var sdn = (STORAGE_DEVICE_NUMBER)Marshal.PtrToStructure(sdnPtr, typeof(STORAGE_DEVICE_NUMBER));
                            info.PhysicalDriveIndex = sdn.DeviceNumber;
                            info.PartitionNumber = sdn.PartitionNumber;
                        }
                        Marshal.FreeHGlobal(sdnPtr);

                        // Query Bus Type and Hardware Info
                        var query = new STORAGE_PROPERTY_QUERY { PropertyId = 0, QueryType = 0 };
                        int querySize = Marshal.SizeOf(typeof(STORAGE_PROPERTY_QUERY));
                        IntPtr queryPtr = Marshal.AllocHGlobal(querySize);
                        Marshal.StructureToPtr(query, queryPtr, false);

                        int descSize = Marshal.SizeOf(typeof(STORAGE_DEVICE_DESCRIPTOR));
                        IntPtr descPtr = Marshal.AllocHGlobal(descSize);

                        if (DeviceIoControl(hVol, IOCTL_STORAGE_QUERY_PROPERTY, queryPtr, (uint)querySize, descPtr, (uint)descSize, out bytesRet, IntPtr.Zero))
                        {
                            var desc = (STORAGE_DEVICE_DESCRIPTOR)Marshal.PtrToStructure(descPtr, typeof(STORAGE_DEVICE_DESCRIPTOR));
                            info.IsUsb = (desc.BusType == 7); // 7 = BusTypeUsb
                            if (desc.RemovableMedia == 1)
                            {
                                info.IsRemovable = true;
                            }

                            // Extract Product / Vendor strings
                            string vendor = ExtractAsciiString(descPtr, desc.VendorIdOffset);
                            string product = ExtractAsciiString(descPtr, desc.ProductIdOffset);
                            string fullModel = (vendor + " " + product).Trim();
                            if (!string.IsNullOrEmpty(fullModel))
                            {
                                info.DeviceModel = fullModel;
                            }
                        }

                        Marshal.FreeHGlobal(queryPtr);
                        Marshal.FreeHGlobal(descPtr);
                    }
                    finally
                    {
                        CloseHandle(hVol);
                    }
                }

                // 3. Inspect Physical Drive for Partition Style (MBR vs GPT)
                if (info.PhysicalDriveIndex >= 0)
                {
                    string physPath = string.Format(@"\\.\PhysicalDrive{0}", info.PhysicalDriveIndex);
                    IntPtr hPhys = CreateFile(physPath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                    if (hPhys.ToInt64() != -1)
                    {
                        try
                        {
                            byte[] layoutBuf = new byte[4096];
                            uint ret = 0;
                            GCHandle pin = GCHandle.Alloc(layoutBuf, GCHandleType.Pinned);
                            if (DeviceIoControl(hPhys, IOCTL_DISK_GET_DRIVE_LAYOUT_EX, IntPtr.Zero, 0, pin.AddrOfPinnedObject(), (uint)layoutBuf.Length, out ret, IntPtr.Zero))
                            {
                                int style = BitConverter.ToInt32(layoutBuf, 0);
                                if (style == 0) info.PartitionStyle = "MBR";
                                else if (style == 1) info.PartitionStyle = "GPT";
                                else info.PartitionStyle = "RAW";
                            }
                            pin.Free();
                        }
                        catch { }
                        finally
                        {
                            CloseHandle(hPhys);
                        }
                    }
                }

                return info;
            }
            catch (Exception ex)
            {
                Logger.Warn(string.Format("Could not inspect drive {0}: {1}", root, ex.Message));
                return null;
            }
        }

        private static string ExtractAsciiString(IntPtr basePtr, uint offset)
        {
            if (offset == 0) return string.Empty;
            try
            {
                IntPtr strPtr = new IntPtr(basePtr.ToInt64() + offset);
                return Marshal.PtrToStringAnsi(strPtr) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}

