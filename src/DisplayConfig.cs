using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace spiw
{
    internal class DisplayConfigInfo
    {
        public int DisplayId { get; private set; }
        public string FriendlyName { get; private set; }
        public int LeftPosition { get; private set; }
        public int TopPosition { get; private set; }
        public uint HorizontalResolution { get; private set; }
        public uint VerticalResolution { get; private set; }

        public DisplayConfigInfo(int displayId, string friendlyName, int leftPosition, int topPosition, uint horizontalResolution, uint verticalResolution)
        {
            DisplayId = displayId;
            FriendlyName = friendlyName;
            LeftPosition = leftPosition;
            TopPosition = topPosition;
            HorizontalResolution = horizontalResolution;
            VerticalResolution = verticalResolution;
        }
    }

    internal static class DisplayConfig
    {
        public static IReadOnlyList<DisplayConfigInfo> GetDisplayConfigInfo()
        {
            // Get the active display's config data.
            var displayConfigDataArray = GetActiveDisplayConfigData();

            var displayConfigInfoList = new List<DisplayConfigInfo>(displayConfigDataArray.Count);
            foreach (var configData in displayConfigDataArray)
            {
                var friendlyName = GetDisplayFriendlyName(configData.AdapterId, configData.TargetId);

                displayConfigInfoList.Add(new DisplayConfigInfo(
                    configData.ConfigDataId, 
                    friendlyName, 
                    configData.LeftPosition, 
                    configData.TopPosition, 
                    configData.HorizontalResolution, 
                    configData.VerticalResolution));
            }

            return displayConfigInfoList;
        }

        private class DisplayConfigData
        {
            public int ConfigDataId { get; private set; }
            public NativeMethods.LUID AdapterId { get; private set; }
            public uint TargetId { get; private set; }
            public uint SourceId { get; private set; }
            public int LeftPosition { get; private set; }
            public int TopPosition { get; private set; }
            public uint HorizontalResolution { get; private set; }
            public uint VerticalResolution { get; private set; }

            public DisplayConfigData(int configDataId, NativeMethods.LUID adapterId, uint targetId, uint sourceId, int leftPosition, int topPosition, uint horizontalResolution, uint verticalResolution)
            {
                ConfigDataId = configDataId;
                AdapterId = adapterId;
                TargetId = targetId;
                SourceId = sourceId;
                LeftPosition = leftPosition;
                TopPosition = topPosition;
                HorizontalResolution = horizontalResolution;
                VerticalResolution = verticalResolution;
            }
        }

        private static IReadOnlyList<DisplayConfigData> GetActiveDisplayConfigData()
        {
            // Get the needed number of elements for the display config information array.
            var result = NativeMethods.GetDisplayConfigBufferSizes(NativeMethods.QDC_ONLY_ACTIVE_PATHS, out uint numPathArrayElements, out uint numModeInfoArrayElements);
            if (result != NativeMethods.ERROR_SUCCESS)
            {
                throw new Win32Exception(result, "Failed GetDisplayConfigBufferSizes()");
            }

            // Create the display config information arrays using the got number of elements.
            NativeMethods.DISPLAYCONFIG_PATH_INFO[] pathArray = new NativeMethods.DISPLAYCONFIG_PATH_INFO[numPathArrayElements];
            NativeMethods.DISPLAYCONFIG_MODE_INFO[] modeInfoArray = new NativeMethods.DISPLAYCONFIG_MODE_INFO[numModeInfoArrayElements];

            // Get the active display's config information.
            result = NativeMethods.QueryDisplayConfig(NativeMethods.QDC_ONLY_ACTIVE_PATHS, ref numPathArrayElements, pathArray, ref numModeInfoArrayElements, modeInfoArray, IntPtr.Zero);
            if (result != NativeMethods.ERROR_SUCCESS)
            {
                throw new Win32Exception(result, "Failed QueryDisplayConfig()");
            }

            var displayConfigDataList = new List<DisplayConfigData>();
            for (int i = 0; i < pathArray.Length; i++)
            {
                // Find the corresponding source mode information.
                var modeInfo = FindSourceModeInfo(modeInfoArray, pathArray[i].targetInfo.adapterId, pathArray[i].sourceInfo.id);

                displayConfigDataList.Add(new DisplayConfigData(
                    i,
                    pathArray[i].targetInfo.adapterId,
                    pathArray[i].targetInfo.id,
                    pathArray[i].sourceInfo.id,
                    modeInfo.sourceMode.position.x,
                    modeInfo.sourceMode.position.y,
                    modeInfo.sourceMode.width,
                    modeInfo.sourceMode.height));
            }

            return displayConfigDataList;
        }

        private static NativeMethods.DISPLAYCONFIG_MODE_INFO FindSourceModeInfo(IEnumerable<NativeMethods.DISPLAYCONFIG_MODE_INFO> modeInfoArray, NativeMethods.LUID adapterId, uint sourceId)
        {
            foreach (var modeInfo in modeInfoArray)
            {
                if (modeInfo.infoType == NativeMethods.DISPLAYCONFIG_MODE_INFO_TYPE.Source &&
                    modeInfo.adapterId.HighPart == adapterId.HighPart && modeInfo.adapterId.LowPart == adapterId.LowPart &&
                    modeInfo.id == sourceId)
                {
                    return modeInfo;
                }
            }

            throw new InvalidOperationException("Couldn't find the source mode info.");
        }

        private static string GetDisplayFriendlyName(NativeMethods.LUID adapterId, uint targetId)
        {
            var targetDeviceName = new NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME()
            {
                header = new NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER()
                {
                    type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_TYPE.GetTargetName,
                    size = (uint)Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                    adapterId = adapterId,
                    id = targetId,
                }
            };
            var result = NativeMethods.DisplayConfigGetDeviceInfo(ref targetDeviceName);
            if (result != NativeMethods.ERROR_SUCCESS)
            {
                throw new Win32Exception(result, "Failed DisplayConfigGetDeviceInfo()");
            }

            if (targetDeviceName.outputTechnology == NativeMethods.DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.Internal)
            {
                // The internal display has no friendly name. (It has the empty string as a friendly name)
                return @"(Internal)";
            }
            else
            {
                return targetDeviceName.monitorFriendlyDeviceName;
            }
        }

        private static class NativeMethods
        {
            //
            // Common
            //

            // winerror.h
            public const int ERROR_SUCCESS = 0;
            public const int ERROR_ACCESS_DENIED = 5;
            public const int ERROR_GEN_FAILURE = 31;
            public const int ERROR_NOT_SUPPORTED = 50;
            public const int ERROR_INVALID_PARAMETER = 87;
            public const int ERROR_INSUFFICIENT_BUFFER = 122;

            // Definition from ntdef.h
            [StructLayout(LayoutKind.Sequential, Size = 8)]
            public struct LUID
            {
                public uint LowPart;
                public int HighPart;
            };

            //
            // For QueryDisplayConfig function.
            //

            // Definition from ntdef.h
            [StructLayout(LayoutKind.Sequential, Size = 8)]
            public struct POINTL
            {
                public int x;
                public int y;
            };

            // Definition from ntdef.h
            [StructLayout(LayoutKind.Sequential, Size = 16)]
            public struct RECTL
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Explicit, Size = 20)]
            public struct DISPLAYCONFIG_PATH_SOURCE_INFO
            {
                [FieldOffset(0)]
                public LUID adapterId;

                [FieldOffset(8)]
                public uint id;

                [FieldOffset(12)]
                public uint modeInfoIdx;

                [FieldOffset(12)]
                public ushort cloneGroupId;

                [FieldOffset(14)]
                public ushort sourceModeInfoIdx;

                [FieldOffset(16)]
                public uint statusFlags;
            };

            // Definition from wingdi.h
            public enum DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY : uint
            {
                //Other = -1,
                Hd15 = 0,
                Svideo = 1,
                CompositeVideo = 2,
                ComponentVideo = 3,
                Dvi = 4,
                Hdmi = 5,
                Lvds = 6,
                DJpn = 8,
                Sdi = 9,
                DisplayportExternal = 10,
                DisplayportEmbedded = 11,
                UdiExternal = 12,
                UdiEmbedded = 13,
                Sdtvdongle = 14,
                Miracast = 15,
                IndirectWired = 16,
                IndirectVirtual = 17,
                Internal = 0x80000000,
            };

            // Definition from wingdi.h
            public enum DISPLAYCONFIG_ROTATION : uint
            {
                Identity = 1,
                Rotate90 = 2,
                Rotate180 = 3,
                Rotate270 = 4,
            };

            // Definition from wingdi.h
            public enum DISPLAYCONFIG_SCALING : uint
            {
                Identity = 1,
                Centered = 2,
                Stretched = 3,
                Aspectratiocenteredmax = 4,
                Custom = 5,
                Preferred = 128,
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Sequential, Size = 8)]
            public struct DISPLAYCONFIG_RATIONAL
            {
                public uint Numerator;
                public uint Denominator;
            };

            // Definition from wingdi.h
            public enum DISPLAYCONFIG_SCANLINE_ORDERING : uint
            {
                Unspecified = 0,
                Ppogressive = 1,
                Interlaced = 2,
                InterlacedUpperfieldfirst = Interlaced,
                InterlacedLowerfieldfirst = 3,
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Explicit, Size = 48)]
            public struct DISPLAYCONFIG_PATH_TARGET_INFO
            {
                [FieldOffset(0)]
                public LUID adapterId;

                [FieldOffset(8)]
                public uint id;

                [FieldOffset(12)]
                public uint modeInfoIdx;

                [FieldOffset(12)]
                public ushort desktopModeInfoIdx;

                [FieldOffset(14)]
                public ushort targetModeInfoIdx;

                [FieldOffset(16)]
                public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;

                [FieldOffset(20)]
                public DISPLAYCONFIG_ROTATION rotation;

                [FieldOffset(24)]
                public DISPLAYCONFIG_SCALING scaling;

                [FieldOffset(28)]
                public DISPLAYCONFIG_RATIONAL refreshRate;

                [FieldOffset(36)]
                public DISPLAYCONFIG_SCANLINE_ORDERING scanLineOrdering;

                [FieldOffset(40)]
                public bool targetAvailable;

                [FieldOffset(44)]
                public uint statusFlags;
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Sequential, Size = 72)]
            public struct DISPLAYCONFIG_PATH_INFO
            {
                public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
                public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
                public uint flags;
            };

            // Definition from wingdi.h
            public enum DISPLAYCONFIG_MODE_INFO_TYPE : uint
            {
                Source = 1,
                Target = 2,
                DesktopImage = 3,
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Sequential, Size = 8)]
            public struct DISPLAYCONFIG_2DREGION
            {
                uint cx;
                uint cy;
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Sequential, Size = 48)]
            public struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
            {
                public ulong pixelRate;
                public DISPLAYCONFIG_RATIONAL hSyncFreq;
                public DISPLAYCONFIG_RATIONAL vSyncFreq;
                public DISPLAYCONFIG_2DREGION activeSize;
                public DISPLAYCONFIG_2DREGION totalSize;
                public uint videoStandard;
                public DISPLAYCONFIG_SCANLINE_ORDERING scanLineOrdering;
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Sequential, Size = 48)]
            public struct DISPLAYCONFIG_TARGET_MODE
            {
                public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
            };

            // Definition from wingdi.h
            public enum DISPLAYCONFIG_PIXELFORMAT : uint
            {
                PixelFormat8bpp = 1,
                PixelFormat16bpp = 2,
                PixelFormat24bpp = 3,
                PixelFormat32bpp = 4,
                PixelFormatNonGdi = 5,
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Sequential, Size = 20)]
            public struct DISPLAYCONFIG_SOURCE_MODE
            {
                public uint width;
                public uint height;
                public DISPLAYCONFIG_PIXELFORMAT pixelFormat;
                public POINTL position;
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Sequential, Size = 40)]
            public struct DISPLAYCONFIG_DESKTOP_IMAGE_INFO
            {
                public POINTL PathSourceSize;
                public RECTL DesktopImageRegion;
                public RECTL DesktopImageClip;
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Explicit, Size = 64)]
            public struct DISPLAYCONFIG_MODE_INFO
            {
                [FieldOffset(0)]
                public DISPLAYCONFIG_MODE_INFO_TYPE infoType;

                [FieldOffset(4)]
                public uint id;

                [FieldOffset(8)]
                public LUID adapterId;

                [FieldOffset(16)]
                public DISPLAYCONFIG_TARGET_MODE targetMode;

                [FieldOffset(16)]
                public DISPLAYCONFIG_SOURCE_MODE sourceMode;

                [FieldOffset(16)]
                public DISPLAYCONFIG_DESKTOP_IMAGE_INFO desktopImageInfo;
            };

            // Definitions from wingdi.h
            public const uint QDC_ALL_PATHS = 0x00000001;
            public const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
            public const uint QDC_DATABASE_CURRENT = 0x00000004;

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
            public static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
            public static extern int QueryDisplayConfig(uint flags, ref uint numPathArrayElements, [Out] DISPLAYCONFIG_PATH_INFO[] pathArray, ref uint numModeInfoArrayElements, [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray, IntPtr currentTopologyId);

            //
            // For DisplayConfigGetDeviceInfo function.
            //

            // Definitions from wingdi.h
            public enum DISPLAYCONFIG_DEVICE_INFO_TYPE : int
            {
                GetSourceName = 1,
                GetTargetName = 2,
                GetTargetPreferredMode = 3,
                GetAdapterName = 4,
                SetTargetPersistence = 5,
                GetTargetBaseType = 6,
                GetSupportVirtualResolution = 7,
                SetSupportVirtalResolution = 8,
                GetAdvancedColorInfo = 9,
                SetAdvancedColorState = 10,
                GetSdrWhiteLevel = 11,
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Sequential, Size = 20)]
            public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                public DISPLAYCONFIG_DEVICE_INFO_TYPE type;
                public uint size;
                public LUID adapterId;
                public uint id;
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Sequential, Size = 4)]
            public struct DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS
            {
                public uint value;
            };

            // Definition from wingdi.h
            [StructLayout(LayoutKind.Sequential, Size = 420, CharSet = CharSet.Unicode)]
            public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
            {
                public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
                public DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS flags;
                public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
                public ushort edidManufactureId;
                public ushort edidProductCodeId;
                public uint connectorInstance;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
                public string monitorFriendlyDeviceName;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string monitorDevicePath;
            };

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
            public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName);
        }
    }
}
