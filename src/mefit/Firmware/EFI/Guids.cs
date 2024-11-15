﻿// Mac EFI Toolkit
// https://github.com/MuertoGB/MacEfiToolkit

// Guids.cs
// Released under the GNU GLP v3.0

namespace Mac_EFI_Toolkit.Firmware.EFI
{
    class Guids
    {
        internal static readonly byte[] DXE_CORE =
        {
            0xD9, 0x54, 0x93, 0x7A,
            0x68, 0x04,
            0x4A, 0x44,
            0x81, 0xCE, 0x0B, 0xF6, 0x17, 0xD8, 0x90, 0xDF
        };

        internal static readonly byte[] LZMA_DXE_VOLUME_IMAGE_0_GUID =
        {
            0xDB, 0x7F, 0xAD, 0x77,
            0x2A, 0xDF,
            0x02, 0x43,
            0x88, 0x98, 0xC7, 0x2E, 0x4C, 0xDB, 0xD0, 0xF4
        };

        internal static readonly byte[] LZMA_DXE_VOLUME_IMAGE_1_GUID =
        {
            0x79, 0xDE, 0xD3, 0x2A,
            0xE9, 0x63,
            0x4F, 0x9B,
            0xB6, 0x4F, 0xE7, 0xC6, 0x18, 0x1B, 0x0C, 0xEC
        };

        internal static readonly byte[] APFS_DXE_GUID =
        {
            0xF4, 0x32, 0xFB, 0xCF,
            0xA8, 0xC2,
            0xBB, 0x48,
            0xA0, 0xEB, 0x6C, 0x3C, 0xCA, 0x3F, 0xE8, 0x47
        };

        internal static readonly byte[] APPLE_ROM_INFO_GUID =
        {
            0xF6, 0xAB, 0x35, 0xB5,
            0x7D, 0x96,
            0xF2, 0x43,
            0xB4, 0x94, 0xA1, 0xEB, 0x8E, 0x21, 0xA2, 0x8E
        };

        internal static readonly byte[] EFI_BIOS_ID_GUID =
        {
            0x09, 0x6D, 0xE3, 0xC3,
            0x94, 0x82,
            0x97, 0x4B,
            0xA8, 0x57, 0xD5, 0x28, 0x8F, 0xE3, 0x3E, 0x28
        };

        internal static readonly byte[] NVRAM_SECTION_GUID =
        {
            0x8D, 0x2B, 0xF1, 0xFF,
            0x96, 0x76,
            0x8B, 0x4C,
            0xA9, 0x85, 0x27, 0x47, 0x07, 0x5B, 0x4F, 0x50
        };

        internal static readonly byte[] PDR_SECTION_GUID =
        {
            0x4A, 0x25, 0x1F, 0x78,
            0x57, 0xC4,
            0x13, 0x5D,
            0x92, 0x75, 0x1B, 0xF5, 0xD5, 0x6E, 0x07, 0x24
        };

        internal static readonly byte[] DXE_CORE_GUID =
        {
            0xD9, 0x54, 0x93, 0x7A,
            0x68, 0x04,
            0x4A, 0x44,
            0x81, 0xCE, 0x0B, 0xF6, 0x17, 0xD8, 0x90, 0xDF
        };

        internal static readonly byte[] APPLE_IMMUTABLE_FV_GUID =
        {
            0xAD, 0xEE, 0xAD, 0x04,
            0xFF, 0x61,
            0x31, 0x4D,
            0xB6, 0xBA, 0x64, 0xF8, 0xBF, 0x90, 0x1F, 0x5A
        };

        internal static readonly byte[] APPLE_AUTH_FV_GUID =
        {
            0x8C, 0x1B, 0x00, 0xBD,
            0x71, 0x6A,
            0x7B, 0x48,
            0xA1, 0x4F, 0x0C, 0x2A, 0x2D, 0xCF, 0x7A, 0x5D
        };

        internal static readonly byte[] APPLE_IMC_GUID =
        {
            0x97, 0x21, 0x3D, 0x15,
            0xBD, 0x29,
            0xDC, 0x44,
            0xAC, 0x59, 0x88, 0x7F, 0x70, 0xE4, 0x1A, 0x6B
        };

    }
}