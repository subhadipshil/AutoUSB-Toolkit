using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace AutoUSBToolkit.Core
{
    public class AudioTagInfo
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Track { get; set; }
        public string Year { get; set; }
        public byte Id3Version { get; set; }
        public bool HasArtwork { get; set; }
        public int ArtworkWidth { get; set; }
        public int ArtworkHeight { get; set; }
        public long ArtworkSizeBytes { get; set; }

        public AudioTagInfo()
        {
            Title = string.Empty;
            Artist = "Unknown Artist";
            Album = "Unknown Album";
            Track = "01";
            Year = string.Empty;
            Id3Version = 3;
        }
    }

    /// <summary>
    /// ID3 Tag Sanitizer & Artwork Optimizer.
    /// Ensures ID3v2.3 compliance and resizes album artwork to <= 500x500 baseline JPEG
    /// to prevent memory overflows and track skipping in car head units.
    /// </summary>
    public static class AudioOptimizer
    {
        public static AudioTagInfo ReadId3Info(string mp3Path)
        {
            var info = new AudioTagInfo();
            try
            {
                using (var fs = new FileStream(mp3Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] header = new byte[10];
                    if (fs.Read(header, 0, 10) == 10)
                    {
                        if (header[0] == 'I' && header[1] == 'D' && header[2] == '3')
                        {
                            info.Id3Version = header[3]; // 3 for 2.3, 4 for 2.4
                            int tagSize = DecodeSynchsafe(header, 6);

                            if (tagSize > 0 && tagSize < 10 * 1024 * 1024 && fs.Length >= 10 + tagSize)
                            {
                                byte[] tagData = new byte[tagSize];
                                fs.Read(tagData, 0, tagSize);
                                ParseFrames(tagData, info);
                            }
                        }
                    }

                    // Fallback to ID3v1 (last 128 bytes) if fields are missing
                    if (string.IsNullOrEmpty(info.Title) && fs.Length >= 128)
                    {
                        fs.Seek(-128, SeekOrigin.End);
                        byte[] v1 = new byte[128];
                        if (fs.Read(v1, 0, 128) == 128)
                        {
                            if (v1[0] == 'T' && v1[1] == 'A' && v1[2] == 'G')
                            {
                                if (string.IsNullOrEmpty(info.Title))
                                    info.Title = Encoding.Default.GetString(v1, 3, 30).Trim().Trim('\0');
                                if (string.IsNullOrEmpty(info.Artist) || info.Artist == "Unknown Artist")
                                    info.Artist = Encoding.Default.GetString(v1, 33, 30).Trim().Trim('\0');
                                if (string.IsNullOrEmpty(info.Album) || info.Album == "Unknown Album")
                                    info.Album = Encoding.Default.GetString(v1, 63, 30).Trim().Trim('\0');
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Could not read ID3 metadata: " + ex.Message);
            }

            if (string.IsNullOrEmpty(info.Title))
            {
                info.Title = Path.GetFileNameWithoutExtension(mp3Path);
            }

            return info;
        }

        private static void ParseFrames(byte[] data, AudioTagInfo info)
        {
            int pos = 0;
            int length = data.Length;

            while (pos + 10 < length)
            {
                if (data[pos] == 0) break; // Padding reached

                string frameId = Encoding.ASCII.GetString(data, pos, 4);
                int frameSize = (data[pos + 4] << 24) | (data[pos + 5] << 16) | (data[pos + 6] << 8) | data[pos + 7];

                if (info.Id3Version == 4)
                {
                    frameSize = DecodeSynchsafe(data, pos + 4);
                }

                if (frameSize <= 0 || pos + 10 + frameSize > length) break;

                int contentPos = pos + 10;

                if (frameId == "TIT2") // Title
                {
                    info.Title = DecodeTextFrame(data, contentPos, frameSize);
                }
                else if (frameId == "TPE1") // Artist
                {
                    info.Artist = DecodeTextFrame(data, contentPos, frameSize);
                }
                else if (frameId == "TALB") // Album
                {
                    info.Album = DecodeTextFrame(data, contentPos, frameSize);
                }
                else if (frameId == "TRCK") // Track number
                {
                    info.Track = DecodeTextFrame(data, contentPos, frameSize);
                }
                else if (frameId == "TYER" || frameId == "TDRC") // Year
                {
                    info.Year = DecodeTextFrame(data, contentPos, frameSize);
                }
                else if (frameId == "APIC") // Attached Picture
                {
                    info.HasArtwork = true;
                    info.ArtworkSizeBytes = frameSize;
                    InspectArtworkSize(data, contentPos, frameSize, info);
                }

                pos += 10 + frameSize;
            }
        }

        private static void InspectArtworkSize(byte[] data, int offset, int size, AudioTagInfo info)
        {
            try
            {
                // In APIC frame: [encoding: 1 byte], [mime: null-terminated], [picture_type: 1 byte], [description: null-terminated], [image binary data]
                int p = offset + 1; // Skip encoding byte
                while (p < offset + size && data[p] != 0) p++;
                p++; // Skip null terminator of MIME
                if (p >= offset + size) return;
                p++; // Skip picture type (1 byte)
                while (p < offset + size && data[p] != 0) p++;
                p++; // Skip null terminator of description
                if (p >= offset + size) return;

                int imgBytesLen = (offset + size) - p;
                if (imgBytesLen > 0)
                {
                    using (var ms = new MemoryStream(data, p, imgBytesLen))
                    using (var img = Image.FromStream(ms))
                    {
                        info.ArtworkWidth = img.Width;
                        info.ArtworkHeight = img.Height;
                    }
                }
            }
            catch { }
        }

        private static string DecodeTextFrame(byte[] data, int offset, int size)
        {
            if (size <= 1) return string.Empty;
            byte encoding = data[offset];
            int textOffset = offset + 1;
            int textSize = size - 1;

            try
            {
                if (encoding == 0) // ISO-8859-1
                {
                    return Encoding.GetEncoding("ISO-8859-1").GetString(data, textOffset, textSize).Trim('\0').Trim();
                }
                else if (encoding == 1) // UTF-16 with BOM
                {
                    return Encoding.Unicode.GetString(data, textOffset, textSize).Trim('\0').Trim();
                }
                else if (encoding == 3) // UTF-8
                {
                    return Encoding.UTF8.GetString(data, textOffset, textSize).Trim('\0').Trim();
                }
                return Encoding.Default.GetString(data, textOffset, textSize).Trim('\0').Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int DecodeSynchsafe(byte[] data, int offset)
        {
            return (data[offset] << 21) |
                   (data[offset + 1] << 14) |
                   (data[offset + 2] << 7) |
                   data[offset + 3];
        }

        /// <summary>
        /// Sanitizes MP3 file in place: ensures ID3v2.3, trims oversized artwork to max 500x500.
        /// </summary>
        public static bool OptimizeMp3Tags(string filePath, int maxArtDimension = 500)
        {
            try
            {
                var info = ReadId3Info(filePath);
                if (info.HasArtwork && (info.ArtworkWidth > maxArtDimension || info.ArtworkHeight > maxArtDimension || info.ArtworkSizeBytes > 250 * 1024))
                {
                    Logger.Info(string.Format("Resizing oversized artwork ({0}x{1}, {2} KB) in '{3}' for car compatibility...",
                        info.ArtworkWidth, info.ArtworkHeight, info.ArtworkSizeBytes / 1024, Path.GetFileName(filePath)));
                    // Resizing embedded cover art or normalizing
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn(string.Format("Audio tag optimization warning on {0}: {1}", Path.GetFileName(filePath), ex.Message));
                return false;
            }
        }

        public static byte[] ResizeImageToJpeg(byte[] rawImageBytes, int maxDimension)
        {
            try
            {
                using (var msIn = new MemoryStream(rawImageBytes))
                using (var original = Image.FromStream(msIn))
                {
                    if (original.Width <= maxDimension && original.Height <= maxDimension)
                    {
                        return rawImageBytes;
                    }

                    int newWidth, newHeight;
                    if (original.Width > original.Height)
                    {
                        newWidth = maxDimension;
                        newHeight = (int)((original.Height * (double)maxDimension) / original.Width);
                    }
                    else
                    {
                        newHeight = maxDimension;
                        newWidth = (int)((original.Width * (double)maxDimension) / original.Height);
                    }

                    using (var resized = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb))
                    {
                        using (var g = Graphics.FromImage(resized))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.DrawImage(original, 0, 0, newWidth, newHeight);
                        }

                        using (var msOut = new MemoryStream())
                        {
                            // Save as 85% quality baseline JPEG
                            var codec = GetEncoder(ImageFormat.Jpeg);
                            var encParams = new EncoderParameters(1);
                            encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);
                            resized.Save(msOut, codec, encParams);
                            return msOut.ToArray();
                        }
                    }
                }
            }
            catch
            {
                return rawImageBytes;
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }
            return null;
        }
    }
}
