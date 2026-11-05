using System;
using System.Collections.Generic;
using AutoUSBToolkit.Models;

namespace AutoUSBToolkit.Core
{
    public static class CarDatabase
    {
        private static readonly List<CarProfile> _profiles = new List<CarProfile>();

        static CarDatabase()
        {
            InitializeProfiles();
        }

        public static List<CarProfile> GetAllProfiles()
        {
            return new List<CarProfile>(_profiles);
        }

        private static void InitializeProfiles()
        {
            // ===================================================================
            // 1. UNIVERSAL AUTO PROFILE (HIGHEST COMPATIBILITY)
            // ===================================================================
            _profiles.Add(new CarProfile
            {
                Id = "universal",
                Make = "Universal Automotive",
                SystemName = "High-Compatibility (All Cars)",
                Category = "Universal",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768, // 32KB
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 8,
                MaxTotalFiles = 5000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps, 44.1 kHz, CBR/VBR), WMA",
                SupportedVideo = "MP4 (H.264/AAC, 720p, <=30fps)",
                TagStandard = "ID3v2.3 (UTF-16 or ISO-8859-1)",
                MaxArtworkResolution = 500,
                Notes = "Guaranteed to work with 99.9% of car stereos, factory head units, and aftermarket decks."
            });

            // ===================================================================
            // 2. INDIA CAR BRANDS (COMPREHENSIVE ALL INDIAN MANUFACTURERS)
            // ===================================================================

            // Maruti Suzuki - SmartPlay Pro+ (9-inch HD)
            _profiles.Add(new CarProfile
            {
                Id = "maruti_smartplay_pro_plus",
                Make = "Maruti Suzuki",
                SystemName = "SmartPlay Pro+ 9\" (Grand Vitara, Baleno, Brezza, Fronx, Jimny, XL6, Invicto, New Dzire)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC, FLAC (Up to 24-bit/96kHz), WAV",
                SupportedVideo = "MP4, AVI, MKV (720p max, vehicle parked)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Strict FAT32 requirement. Arkamys Surround Sense DSP plays FLAC natively on 9-inch Pro+ units. Max 255 tracks per folder; larger folders cause indexing halts."
            });

            // Maruti Suzuki - SmartPlay Pro (7-inch)
            _profiles.Add(new CarProfile
            {
                Id = "maruti_smartplay_pro",
                Make = "Maruti Suzuki",
                SystemName = "SmartPlay Pro 7\" (New Swift 2024+, Brezza, Fronx, Ertiga)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 8,
                MaxTotalFiles = 8000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps, 44.1 kHz), WMA, AAC",
                SupportedVideo = "MP4 (720p, 30fps)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Strictly FAT32 with 32 KB clusters. Folders with >255 files will truncate playback. ID3v2.3 tags ensure track titles and album art display without freezing."
            });

            // Maruti Suzuki - SmartPlay Studio (7-inch)
            _profiles.Add(new CarProfile
            {
                Id = "maruti_smartplay_studio",
                Make = "Maruti Suzuki",
                SystemName = "SmartPlay Studio (Swift, Dzire, WagonR, Ertiga, Celerio, Ignis, S-Presso)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 6,
                MaxTotalFiles = 5000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC",
                SupportedVideo = "MP4 (480p/720p H.264 Baseline, vehicle stationary)",
                TagStandard = "ID3v2.3 (ISO-8859-1)",
                MaxArtworkResolution = 400,
                Notes = "Harman-developed SmartPlay Studio. Sensitive to hidden macOS ._ AppleDouble files. Clean FAT32 format with 32KB clusters prevents 'No Song' errors."
            });

            // Maruti Suzuki - SmartPlay Dock / 2-DIN OEM Audio
            _profiles.Add(new CarProfile
            {
                Id = "maruti_smartplay_dock",
                Make = "Maruti Suzuki",
                SystemName = "SmartPlay Dock / OEM 2-DIN Audio (Alto K10, WagonR LXi, Eeco, Tour S/M/V)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 5,
                MaxTotalFiles = 2500,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (CBR 128-320 kbps), WMA",
                SupportedVideo = "None",
                TagStandard = "ID3v2.3 (ISO-8859-1 / ASCII)",
                MaxArtworkResolution = 0,
                Notes = "Basic microcontroller architecture. Drives above 32GB fail to mount. Requires physical FAT directory sorting for albums to play in correct order."
            });

            // Tata Motors - Cinematic & Harman Touchscreen (10.25-inch / 12.3-inch)
            _profiles.Add(new CarProfile
            {
                Id = "tata_cinematic_harman",
                Make = "Tata Motors",
                SystemName = "Harman Cinematic 10.25\" & 12.3\" (Harrier, Safari, Nexon Facelift, Curvv, Punch EV, Nexon EV)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 500,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3, AAC, FLAC (16/24-bit up to 96kHz), WAV, WMA",
                SupportedVideo = "MP4, MKV (1080p, H.264/AAC, vehicle in park)",
                TagStandard = "ID3v2.3 (UTF-8 / UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "Tata's latest Harman / JBL multi-speaker acoustic architecture. Supports Hi-Res FLAC playback. Ensure MBR partition to prevent 'Unsupported USB Storage' prompt."
            });

            // Tata Motors - Harman 7-inch Touchscreen
            _profiles.Add(new CarProfile
            {
                Id = "tata_harman_7",
                Make = "Tata Motors",
                SystemName = "Harman Touchscreen 7\" (Punch, Altroz, Tiago, Tigor, Nexon Pre-facelift)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 6,
                MaxTotalFiles = 6000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC, FLAC (16-bit/44.1kHz)",
                SupportedVideo = "MP4 (720p max, vehicle stopped with handbrake on)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Harman 4-speaker + 4-tweeter sound system. FAT32 32KB cluster eliminates playback stutter. Square album artwork (500x500 JPEG) renders sharply."
            });

            // Tata Motors - ConnectNext Audio (5-inch / 2-DIN)
            _profiles.Add(new CarProfile
            {
                Id = "tata_connectnext",
                Make = "Tata Motors",
                SystemName = "ConnectNext Audio (Tiago, Tigor XE/XT, Zest, Bolt)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 4,
                MaxTotalFiles = 3000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (CBR/VBR 32-320 kbps), WMA",
                SupportedVideo = "None",
                TagStandard = "ID3v2.3 (ISO-8859-1)",
                MaxArtworkResolution = 0,
                Notes = "Harman ConnectNext display. Drives must be MBR FAT32. Non-ASCII characters in song titles can freeze the unit."
            });

            // Mahindra - AdrenoX AI Infotainment (Dual 10.25-inch)
            _profiles.Add(new CarProfile
            {
                Id = "mahindra_adrenox",
                Make = "Mahindra",
                SystemName = "AdrenoX AI Dual 10.25\" (XUV700, Scorpio-N, Thar Roxx, XUV 3XO, XUV400 EV, BE.05)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 500,
                MaxFolderDepth = 8,
                MaxTotalFiles = 15000,
                RecommendedMaxDriveSizeGb = 128,
                SupportedAudio = "MP3 (32-320 kbps), AAC, FLAC (Sony 3D Immersive), WAV, WMA",
                SupportedVideo = "MP4 (H.264/AAC, 1080p, parked only)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "Sony 3D 12-speaker sound system. Supports up to 128GB formatted as FAT32 with MBR partition table. High-bitrate FLAC supported."
            });

            // Mahindra - Touchscreen Infotainment (7-inch / 9-inch)
            _profiles.Add(new CarProfile
            {
                Id = "mahindra_thar_scorpio",
                Make = "Mahindra",
                SystemName = "Touchscreen Infotainment 7\"-9\" (Thar 3-Door, Scorpio Classic, Bolero Neo, Bolero Neo+)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 6,
                MaxTotalFiles = 5000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC",
                SupportedVideo = "MP4 (720p, 30fps)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Visteon/Nippon sourced head unit. 32GB maximum drive size recommended. Format with 32KB clusters to avoid track stutter during off-road vibrations."
            });

            // Mahindra - Bolero Commercial 2-DIN Audio
            _profiles.Add(new CarProfile
            {
                Id = "mahindra_bolero_audio",
                Make = "Mahindra",
                SystemName = "OEM 2-DIN Audio (Bolero, Bolero Camper, Bolero Maxx, Thar AX)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 4,
                MaxTotalFiles = 2000,
                RecommendedMaxDriveSizeGb = 16,
                SupportedAudio = "MP3 (CBR 128-320 kbps), WMA",
                SupportedVideo = "None",
                TagStandard = "ID3v2.3 (ASCII)",
                MaxArtworkResolution = 0,
                Notes = "Legacy Kenwood/Nippon 2-DIN unit. Drives >16GB or formatted as exFAT/GPT fail completely. Requires FAT sorting for sequential track order."
            });

            // Hyundai Motor India - AVN & Connected Display Audio
            _profiles.Add(new CarProfile
            {
                Id = "hyundai_india",
                Make = "Hyundai India",
                SystemName = "AVN & Display Audio 10.25\" & 8\" (Creta, Venue, Verna, i20, Alcazar, Tucson, Exter, Aura, Grand i10, Ioniq 5)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 500,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC, FLAC (16/24-bit), WAV, OGG",
                SupportedVideo = "MP4, AVI, WMV (720p / 1080p, parked)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "Hyundai Mobis AVN platform with Bose Premium Sound. FAT32 with MBR partition table is strictly required for seamless media indexing."
            });

            // Kia India - Dual Panoramic Display AVNT
            _profiles.Add(new CarProfile
            {
                Id = "kia_india",
                Make = "Kia India",
                SystemName = "Dual Panoramic Display AVNT 10.25\" & 12.3\" (Seltos, Sonet, Carens, Carnival, EV6, EV9)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 500,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC, FLAC (Bose 8-Speaker DSP), WAV",
                SupportedVideo = "MP4, AVI (1080p max, vehicle in park)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "Kia Connect platform with Bose sound system. FAT32 32KB cluster formatting ensures immediate recognition without USB read lag."
            });

            // Toyota Kirloskar Motor (India)
            _profiles.Add(new CarProfile
            {
                Id = "toyota_india",
                Make = "Toyota Kirloskar",
                SystemName = "Audio Multimedia 10.1\" & 8\" (Innova Hycross, Crysta, Fortuner, Legender, Hyryder, Glanza, Rumion, Taisor, Hilux, Camry, Vellfire)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 8,
                MaxTotalFiles = 8000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC, WAV, FLAC (Hycross / Fortuner JBL)",
                SupportedVideo = "MP4 (720p, vehicle stationary)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Toyota Kirloskar head units (including Maruti joint venture platforms). Drives must use MBR partition table and FAT32 format."
            });

            // Honda Cars India
            _profiles.Add(new CarProfile
            {
                Id = "honda_india",
                Make = "Honda Cars India",
                SystemName = "Advanced Display Audio 8\" & 7\" (Elevate, City 5th Gen & e:HEV, Amaze 2nd/3rd Gen, WR-V)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 8,
                MaxTotalFiles = 5000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps, 44.1/48 kHz), WMA, AAC, WAV",
                SupportedVideo = "MP4 (720p, handbrake engaged)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Honda infotainment is strictly FAT32. Limit to <=255 tracks per folder to prevent audio indexing cut-offs. MBR partition table is mandatory."
            });

            // Volkswagen India (SAVWIPL) - VW Play
            _profiles.Add(new CarProfile
            {
                Id = "vw_india_play",
                Make = "Volkswagen India",
                SystemName = "VW Play 10.1\" Touchscreen (Taigun, Virtus, Tiguan)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 500,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3 (32-320 kbps), AAC, FLAC (16/24-bit), WAV, WMA",
                SupportedVideo = "MP4 (1080p, parked only)",
                TagStandard = "ID3v2.3 (UTF-8 / UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "India 2.0 MQB-A0-IN platform VW Play. FAT32 with 32KB clusters provides instantaneous track skipping without audio buffer delay."
            });

            // Volkswagen India - Composition Media / MIB (Legacy)
            _profiles.Add(new CarProfile
            {
                Id = "vw_india_composition",
                Make = "Volkswagen India",
                SystemName = "Composition Media & RCD (Polo, Vento, Ameo, Jetta, Passat)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 8,
                MaxTotalFiles = 4000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC",
                SupportedVideo = "None",
                TagStandard = "ID3v2.3 (ISO-8859-1)",
                MaxArtworkResolution = 400,
                Notes = "Classic VAG MIB 1/2 systems in India. Requires MBR and FAT32. Max 400x400 JPEG album art."
            });

            // Skoda Auto India - Skoda Play
            _profiles.Add(new CarProfile
            {
                Id = "skoda_india_play",
                Make = "Skoda India",
                SystemName = "Skoda Play 10\" & 8\" (Kushaq, Slavia, Kodiaq, Superb, Octavia, Kylaq)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 500,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3 (32-320 kbps), AAC, FLAC, WAV, WMA",
                SupportedVideo = "MP4 (1080p max, vehicle in park)",
                TagStandard = "ID3v2.3 (UTF-8 / UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "Skoda Sound System with subwoofers. Strictly requires FAT32 with MBR partition. 32KB clusters eliminate track buffer delays."
            });

            // MG Motor India (JSW MG Motor) - i-SMART
            _profiles.Add(new CarProfile
            {
                Id = "mg_india_ismart",
                Make = "MG Motor India",
                SystemName = "i-SMART 14\" Portrait & 10.1\" (Hector, Hector Plus, Astor, ZS EV, Windsor EV, Comet EV, Gloster)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 500,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3, AAC, FLAC (Infinity / Bose Audio DSP), WAV, WMA",
                SupportedVideo = "MP4, MKV (1080p, vehicle parked)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "JSW MG i-SMART platform. Supports up to 64GB formatted as FAT32. Embedded album art (600x600 JPEG) looks crisp on 14-inch screen."
            });

            // Renault India - MediaNAV & EasyLink
            _profiles.Add(new CarProfile
            {
                Id = "renault_india",
                Make = "Renault India",
                SystemName = "MediaNAV Evolution & EasyLink 8\" (Kiger, Triber, Kwid, Duster)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 5,
                MaxTotalFiles = 4000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps, 44.1 kHz), WMA",
                SupportedVideo = "MP4 (480p/720p H.264, parked only)",
                TagStandard = "ID3v2.3 (ISO-8859-1 / ASCII)",
                MaxArtworkResolution = 300,
                Notes = "LG/Visteon MediaNAV firmware is strictly limited to 32GB FAT32 drives. ExFAT or GPT partitions produce 'Media Not Recognized' error."
            });

            // Nissan Motor India - NissanConnect
            _profiles.Add(new CarProfile
            {
                Id = "nissan_india",
                Make = "Nissan India",
                SystemName = "NissanConnect 8\" (Magnite, X-Trail, Kicks)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 8,
                MaxTotalFiles = 5000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC",
                SupportedVideo = "MP4 (720p max, vehicle stopped)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "JBL sound system on Magnite. Requires FAT32, MBR partition table, and strict alphanumeric sorting for tracks to play in sequence."
            });

            // Citroen India (Stellantis)
            _profiles.Add(new CarProfile
            {
                Id = "citroen_india",
                Make = "Citroen India",
                SystemName = "My Citroen Play 10.2\" & 7\" (Basalt, C3, C3 Aircross, e-C3, C5 Aircross)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 8,
                MaxTotalFiles = 5000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC, WAV",
                SupportedVideo = "MP4 (720p, stationary)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Stellantis Smart Car platform. Requires FAT32 with MBR partition table. Purge all macOS ._ AppleDouble metadata files."
            });

            // Force Motors India
            _profiles.Add(new CarProfile
            {
                Id = "force_india",
                Make = "Force Motors",
                SystemName = "Touchscreen Infotainment (Gurkha 3-Door, Gurkha 5-Door, Trax Cruiser, Urbania)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 5,
                MaxTotalFiles = 3000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps), WMA",
                SupportedVideo = "MP4 (720p)",
                TagStandard = "ID3v2.3 (ISO-8859-1)",
                MaxArtworkResolution = 400,
                Notes = "Aftermarket-style factory touchscreen in Force Gurkha. Strictly FAT32, 32GB maximum, MBR partition scheme."
            });

            // Jeep India (Stellantis)
            _profiles.Add(new CarProfile
            {
                Id = "jeep_india",
                Make = "Jeep India",
                SystemName = "Uconnect 5 10.1\" (Compass, Meridian, Wrangler, Grand Cherokee)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 500,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3, AAC, FLAC (Alpine 9-Speaker Audio), WAV, WMA",
                SupportedVideo = "MP4 (1080p, parked only)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "Jeep Uconnect 5 platform with Alpine audio. FAT32 with 32KB clusters ensures fast album loading and zero playback hesitation."
            });

            // BYD India
            _profiles.Add(new CarProfile
            {
                Id = "byd_india",
                Make = "BYD India",
                SystemName = "DiLink Rotating Screen 12.8\" & 15.6\" (Atto 3, Seal, eMAX 7, e6)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 1000,
                MaxFolderDepth = 10,
                MaxTotalFiles = 20000,
                RecommendedMaxDriveSizeGb = 128,
                SupportedAudio = "MP3, AAC, FLAC (Dynaudio 12-Speaker Sound), WAV, APE",
                SupportedVideo = "MP4, MKV (1080p / 4K, vehicle in park)",
                TagStandard = "ID3v2.3 (UTF-8 / UTF-16)",
                MaxArtworkResolution = 800,
                Notes = "Android-based DiLink platform with Dynaudio sound. FAT32 provides greatest compatibility across firmware builds."
            });

            // Isuzu Motors India
            _profiles.Add(new CarProfile
            {
                Id = "isuzu_india",
                Make = "Isuzu India",
                SystemName = "Touchscreen Infotainment (D-Max V-Cross, MU-X, D-Max Hi-Lander)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 6,
                MaxTotalFiles = 4000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC",
                SupportedVideo = "MP4 (720p, handbrake engaged)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Clarion/Alpine based Isuzu head unit. Strictly requires FAT32 and MBR partition table. Max 32GB drive recommended."
            });

            // BMW India
            _profiles.Add(new CarProfile
            {
                Id = "bmw_india",
                Make = "BMW India",
                SystemName = "iDrive 7 / 8 / 8.5 / 9 (3 Series GL, 5 Series LWB, 7 Series, X1, X3, X5, X7, iX1, i4, i7)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 1000,
                MaxFolderDepth = 8,
                MaxTotalFiles = 25000,
                RecommendedMaxDriveSizeGb = 128,
                SupportedAudio = "MP3, AAC, FLAC (Harman Kardon / Bowers & Wilkins), WAV, ALAC",
                SupportedVideo = "MP4, MKV (1080p, parked)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 800,
                Notes = "Supports High-Res FLAC up to 24-bit/192kHz. Artwork up to 800x800 JPEG displays cleanly on Curved Display. FAT32 32KB is standard."
            });

            // Mercedes-Benz India
            _profiles.Add(new CarProfile
            {
                Id = "mercedes_india",
                Make = "Mercedes-Benz India",
                SystemName = "MBUX NTG 6 / NTG 7 Superscreen (A-Class, C-Class, E-Class LWB, S-Class, GLA, GLC, GLE, GLS, EQS)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 1000,
                MaxFolderDepth = 8,
                MaxTotalFiles = 25000,
                RecommendedMaxDriveSizeGb = 128,
                SupportedAudio = "MP3, AAC, FLAC (Burmester 3D Surround), WAV",
                SupportedVideo = "MP4, MKV (1080p, parked only)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 800,
                Notes = "Burmester 4D / 3D audio platforms. Requires MBR partition table. FAT32 formatting ensures instant indexing of large libraries."
            });

            // Audi India
            _profiles.Add(new CarProfile
            {
                Id = "audi_india",
                Make = "Audi India",
                SystemName = "Dual MMI Touch Response & MIB 3 (A4, A6, A8L, Q3, Q5, Q7, Q8, Q8 e-tron)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 1000,
                MaxFolderDepth = 8,
                MaxTotalFiles = 20000,
                RecommendedMaxDriveSizeGb = 128,
                SupportedAudio = "MP3, AAC, FLAC (Bang & Olufsen 3D Premium), WAV, WMA",
                SupportedVideo = "MP4, AVI (1080p, vehicle parked)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 800,
                Notes = "Bang & Olufsen 3D sound. MMI requires MBR partition. FAT32 32KB clusters prevent audio glitches during media track loading."
            });

            // Volvo India
            _profiles.Add(new CarProfile
            {
                Id = "volvo_india",
                Make = "Volvo India",
                SystemName = "Google Built-in Android Automotive (XC40 Recharge, EX40, XC60, XC90, C40 Recharge)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 1000,
                MaxFolderDepth = 8,
                MaxTotalFiles = 20000,
                RecommendedMaxDriveSizeGb = 128,
                SupportedAudio = "MP3, AAC, FLAC (Bowers & Wilkins / Harman Kardon), WAV",
                SupportedVideo = "MP4 (parked only)",
                TagStandard = "ID3v2.3 (UTF-8 / UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "Android Automotive OS head unit. FAT32 format with MBR partition is essential for external media apps to access USB files."
            });

            // Land Rover & Jaguar India
            _profiles.Add(new CarProfile
            {
                Id = "landrover_india",
                Make = "Land Rover India",
                SystemName = "Pivi Pro 11.4\" Curved Touch (Defender, Range Rover, Range Rover Sport, Velar, Evoque, Discovery)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 1000,
                MaxFolderDepth = 8,
                MaxTotalFiles = 20000,
                RecommendedMaxDriveSizeGb = 128,
                SupportedAudio = "MP3, AAC, FLAC (Meridian 3D Surround Audio), WAV",
                SupportedVideo = "MP4 (1080p, vehicle parked)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 800,
                Notes = "Meridian Surround audio platform. Requires MBR partition table and FAT32 format for rapid flash recognition."
            });

            // Lexus India
            _profiles.Add(new CarProfile
            {
                Id = "lexus_india",
                Make = "Lexus India",
                SystemName = "Lexus Interface 12.3\" & 14\" (ES 300h, NX 350h, RX 350h/500h, LX 600, LM 350h)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 500,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3, AAC, FLAC (Mark Levinson PurePlay Sound), WAV, WMA",
                SupportedVideo = "MP4 (720p / 1080p, stationary)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "Mark Levinson PurePlay 17-speaker acoustic system. FAT32 32KB is mandatory for stutter-free audio."
            });

            // Porsche India
            _profiles.Add(new CarProfile
            {
                Id = "porsche_india",
                Make = "Porsche India",
                SystemName = "Porsche Communication Management PCM 6.0 (Macan, Cayenne, Panamera, 911, Taycan)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 1000,
                MaxFolderDepth = 8,
                MaxTotalFiles = 25000,
                RecommendedMaxDriveSizeGb = 128,
                SupportedAudio = "MP3, AAC, FLAC (Burmester High-End 3D / Bose Sound), WAV",
                SupportedVideo = "MP4 (1080p, vehicle stopped)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 800,
                Notes = "PCM 6.0 platform. Requires MBR partition scheme. FAT32 ensures instant media library caching."
            });

            // Mini India
            _profiles.Add(new CarProfile
            {
                Id = "mini_india",
                Make = "Mini India",
                SystemName = "Mini Connected & OLED Display (Cooper S, Countryman, Electric)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 500,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3, AAC, FLAC (Harman Kardon Sound), WAV",
                SupportedVideo = "MP4 (parked only)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "Mini OS platform with circular OLED screen. FAT32 format with 32KB clusters provides optimal playback performance."
            });

            // Blaupunkt India (Popular Indian Aftermarket Brand)
            _profiles.Add(new CarProfile
            {
                Id = "blaupunkt_india",
                Make = "Blaupunkt India",
                SystemName = "Multimedia & Android Series (Key Largo, San Diego, Santa Rosa, Kingston, Monte Carlo, Costa Mesa)",
                Category = "Aftermarket",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 500,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC, FLAC, WAV",
                SupportedVideo = "MP4, AVI, MKV (1080p)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Widely installed across India. Strict FAT32 requirement. Physical FAT directory sorting ensures tracks play in numeric order."
            });

            // Nippon Audiotronics (OEM Tier-1 India Supplier)
            _profiles.Add(new CarProfile
            {
                Id = "nippon_audiotronics",
                Make = "Nippon Audiotronics",
                SystemName = "Tier-1 OEM Infotainment (Factory Supplier for Maruti, Toyota, Renault, Mahindra)",
                Category = "Indian Market",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 6,
                MaxTotalFiles = 4000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps), WMA",
                SupportedVideo = "MP4 (720p max)",
                TagStandard = "ID3v2.3 (ISO-8859-1 / ASCII)",
                MaxArtworkResolution = 400,
                Notes = "Standard Tier-1 factory unit installed in hundreds of thousands of Indian cars. Strictly limited to 32GB FAT32 with MBR partition."
            });

            // Woodman India (Smart Android Decks)
            _profiles.Add(new CarProfile
            {
                Id = "woodman_india",
                Make = "Woodman India",
                SystemName = "Android Smart Infotainment (Woodman X9, Extreme, Android 11/12/13/14)",
                Category = "Aftermarket",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 1000,
                MaxFolderDepth = 10,
                MaxTotalFiles = 20000,
                RecommendedMaxDriveSizeGb = 128,
                SupportedAudio = "MP3, AAC, FLAC, WAV, APE, WMA (DSP Equalizer)",
                SupportedVideo = "MP4, MKV, AVI (1080p / 2K)",
                TagStandard = "ID3v2.3 (UTF-8 / UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "Popular Indian aftermarket Android platform with DSP. FAT32 32KB formatting avoids Android media scanner indexing loops."
            });

            // Indian Android Aftermarket Head Units (Hypersonic / DHC / Unbox / PX6 / UIS7862)
            _profiles.Add(new CarProfile
            {
                Id = "indian_android_decks",
                Make = "Indian Android Decks",
                SystemName = "Universal Android 10/11/12/13/14 (Hypersonic, DHC, TS10, UIS7862, Rockchip PX5/PX6)",
                Category = "Aftermarket",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 1000,
                MaxFolderDepth = 10,
                MaxTotalFiles = 25000,
                RecommendedMaxDriveSizeGb = 128,
                SupportedAudio = "MP3, AAC, FLAC, WAV, APE, OGG, WMA",
                SupportedVideo = "MP4, MKV, AVI, MOV (1080p / 2K)",
                TagStandard = "ID3v2.3 (UTF-8)",
                MaxArtworkResolution = 600,
                Notes = "Found across Indian car accessory markets. FAT32 with 32KB clusters ensures immediate drive detection without Android mount crashes."
            });

            // Pioneer India
            _profiles.Add(new CarProfile
            {
                Id = "pioneer_india",
                Make = "Pioneer India",
                SystemName = "Smart Sync & Touchscreen (DMH, AVH, SPH, DEH Series)",
                Category = "Aftermarket",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 8,
                MaxTotalFiles = 5000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC, WAV, FLAC (DMH series)",
                SupportedVideo = "MP4, AVI, MKV (720p/1080p, handbrake engaged)",
                TagStandard = "ID3v2.3 (ISO-8859-1 or UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Strictly FAT32 with MBR partition table. Pioneer decks stop reading folders after 255 tracks and require physical FAT sorting for correct playback order."
            });

            // Sony India
            _profiles.Add(new CarProfile
            {
                Id = "sony_india",
                Make = "Sony India",
                SystemName = "WebLink Car Audio (XAV-AX5500, XAV-AX3200, XAV-AX1000 Series)",
                Category = "Aftermarket",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 8,
                MaxTotalFiles = 5000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC, FLAC, WAV",
                SupportedVideo = "MP4 (720p / 1080p H.264)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Sony EXTRA BASS head units. Strictly requires FAT32. Album artwork must be baseline JPEG <=500x500."
            });

            // Alpine India
            _profiles.Add(new CarProfile
            {
                Id = "alpine_india",
                Make = "Alpine India",
                SystemName = "Halo9, Halo11 & iLX Series (iLX-F509, iLX-W650, iLX-702D)",
                Category = "Aftermarket",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3, WMA, AAC, FLAC (24-bit/96kHz), WAV",
                SupportedVideo = "MP4, AVI (720p/1080p)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Hi-Res Audio certified. Sensitive to file system fragmentation. FAT32 32KB ensures jitter-free high-bitrate playback."
            });

            // ===================================================================
            // 3. GLOBAL OEM PLATFORMS
            // ===================================================================

            // Ford SYNC 1 & 2
            _profiles.Add(new CarProfile
            {
                Id = "ford_sync12",
                Make = "Ford / Lincoln",
                SystemName = "SYNC 1 & SYNC 2 (MyFord Touch)",
                Category = "Global OEM",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 5,
                MaxTotalFiles = 4000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3 (32-320 kbps), WMA",
                SupportedVideo = "None",
                TagStandard = "ID3v2.3 (ISO-8859-1)",
                MaxArtworkResolution = 300,
                Notes = "Legacy Windows Embedded Automotive. Extremely vulnerable to ID3v2.4 and AppleDouble metadata bugs. Strictly requires FAT32, MBR, and <=32GB."
            });

            // Ford SYNC 3 & 4
            _profiles.Add(new CarProfile
            {
                Id = "ford_sync34",
                Make = "Ford / Lincoln",
                SystemName = "SYNC 3 & SYNC 4",
                Category = "Global OEM",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 1000,
                MaxFolderDepth = 8,
                MaxTotalFiles = 25000,
                RecommendedMaxDriveSizeGb = 128,
                SupportedAudio = "MP3, AAC, FLAC (SYNC 3 v3.0+), WAV, WMA",
                SupportedVideo = "MP4 (SYNC 4 in park)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 600,
                Notes = "QNX-based modern SYNC platform. Handles up to 128GB formatted as FAT32 or exFAT with MBR partition table."
            });

            // Chevrolet / GMC / Cadillac MyLink
            _profiles.Add(new CarProfile
            {
                Id = "gm_mylink",
                Make = "Chevrolet / GMC / Cadillac",
                SystemName = "MyLink / CUE / Infotainment 3",
                Category = "Global OEM",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 500,
                MaxFolderDepth = 8,
                MaxTotalFiles = 10000,
                RecommendedMaxDriveSizeGb = 64,
                SupportedAudio = "MP3 (32-320 kbps), WMA, AAC, FLAC (Infotainment 3)",
                SupportedVideo = "MP4 (720p, in park)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "FAT32 format required. Drives larger than 32GB must be formatted with 32KB clusters to avoid 'Indexing Paused' lockups."
            });

            // Tesla Dashcam & Audio
            _profiles.Add(new CarProfile
            {
                Id = "tesla_dashcam_music",
                Make = "Tesla",
                SystemName = "Tesla MCU 2 & 3 (Model 3 / Y / S / X)",
                Category = "Global OEM",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 1000,
                MaxFolderDepth = 10,
                MaxTotalFiles = 30000,
                RecommendedMaxDriveSizeGb = 128,
                SupportedAudio = "MP3, AAC, FLAC (Up to 24-bit/96kHz), WAV",
                SupportedVideo = "MP4 (H.264, Tesla Dashcam / Sentry / Theater)",
                TagStandard = "ID3v2.3 (UTF-8)",
                MaxArtworkResolution = 800,
                Notes = "For combined TeslaCam + Music, a FAT32 drive with 'TeslaCam' folder in root and 'Music' folder is recommended."
            });

            // Kenwood / JVC
            _profiles.Add(new CarProfile
            {
                Id = "aftermarket_kenwood",
                Make = "Kenwood / JVC",
                SystemName = "eXcelon / DMX / KW Series",
                Category = "Aftermarket",
                RecommendedFileSystem = "FAT32",
                RecommendedClusterSize = 32768,
                RequiresMbr = true,
                MaxFilesPerFolder = 255,
                MaxFolderDepth = 8,
                MaxTotalFiles = 5000,
                RecommendedMaxDriveSizeGb = 32,
                SupportedAudio = "MP3, WMA, AAC, FLAC (192kHz/24-bit on eXcelon), WAV",
                SupportedVideo = "MP4, MKV (720p, parking brake wire grounded)",
                TagStandard = "ID3v2.3 (UTF-16)",
                MaxArtworkResolution = 500,
                Notes = "Requires FAT32 and MBR partition table. Kenwood indexing engine benefits greatly from 32 KB clusters and physical FAT directory sorting."
            });
        }
    }
}
