using ComicPlate.Core.Books;

namespace ComicPlate.Infrastructure.FileSystem;

public static class ImageMetadataReader
{
    public static PageImageInfo Read(Stream stream)
    {
        if (!stream.CanSeek)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return ReadSeekable(memoryStream);
        }

        var position = stream.Position;
        try
        {
            stream.Position = 0;
            return ReadSeekable(stream);
        }
        finally
        {
            stream.Position = position;
        }
    }

    private static PageImageInfo ReadSeekable(Stream stream)
    {
        Span<byte> header = stackalloc byte[32];
        var count = stream.Read(header);
        stream.Position = 0;

        if (count >= 24 && IsPng(header))
        {
            return new PageImageInfo(ReadInt32BigEndian(header[16..20]), ReadInt32BigEndian(header[20..24]));
        }

        if (count >= 10 && IsGif(header))
        {
            return new PageImageInfo(ReadUInt16LittleEndian(header[6..8]), ReadUInt16LittleEndian(header[8..10]));
        }

        if (count >= 26 && IsBmp(header))
        {
            return new PageImageInfo(Math.Abs(ReadInt32LittleEndian(header[18..22])), Math.Abs(ReadInt32LittleEndian(header[22..26])));
        }

        if (count >= 12 && IsJpeg(header))
        {
            return ReadJpeg(stream);
        }

        if (count >= 30 && IsWebp(header))
        {
            return ReadWebp(stream);
        }

        return PageImageInfo.Unknown;
    }

    private static PageImageInfo ReadJpeg(Stream stream)
    {
        stream.Position = 2;
        Span<byte> lengthBytes = stackalloc byte[2];
        Span<byte> sizeBytes = stackalloc byte[5];

        while (stream.Position < stream.Length)
        {
            var markerPrefix = stream.ReadByte();
            if (markerPrefix != 0xFF)
            {
                continue;
            }

            int marker;
            do
            {
                marker = stream.ReadByte();
            }
            while (marker == 0xFF);

            if (marker < 0)
            {
                break;
            }

            if (marker is 0xD8 or 0xD9 or 0x01 || marker is >= 0xD0 and <= 0xD7)
            {
                continue;
            }

            if (stream.Read(lengthBytes) != 2)
            {
                break;
            }

            var segmentLength = ReadUInt16BigEndian(lengthBytes);
            if (segmentLength < 2)
            {
                break;
            }

            if (IsStartOfFrame(marker))
            {
                if (stream.Read(sizeBytes) != 5)
                {
                    break;
                }

                var height = ReadUInt16BigEndian(sizeBytes[1..3]);
                var width = ReadUInt16BigEndian(sizeBytes[3..5]);
                return new PageImageInfo(width, height);
            }

            stream.Position = Math.Min(stream.Position + segmentLength - 2, stream.Length);
        }

        return PageImageInfo.Unknown;
    }

    private static PageImageInfo ReadWebp(Stream stream)
    {
        stream.Position = 12;
        Span<byte> chunk = stackalloc byte[18];
        if (stream.Read(chunk) < 8)
        {
            return PageImageInfo.Unknown;
        }

        if (chunk[0] == (byte)'V' && chunk[1] == (byte)'P' && chunk[2] == (byte)'8' && chunk[3] == (byte)'X' && chunk.Length >= 18)
        {
            var width = 1 + Read24LittleEndian(chunk[12..15]);
            var height = 1 + Read24LittleEndian(chunk[15..18]);
            return new PageImageInfo(width, height);
        }

        if (chunk[0] == (byte)'V' && chunk[1] == (byte)'P' && chunk[2] == (byte)'8' && chunk[3] == (byte)'L' && chunk.Length >= 13)
        {
            var b0 = chunk[9];
            var b1 = chunk[10];
            var b2 = chunk[11];
            var b3 = chunk[12];
            var width = 1 + (((b1 & 0x3F) << 8) | b0);
            var height = 1 + ((b3 << 6) | (b2 >> 2) | ((b1 & 0xC0) << 6));
            return new PageImageInfo(width, height);
        }

        return PageImageInfo.Unknown;
    }

    private static bool IsStartOfFrame(int marker)
    {
        return marker is >= 0xC0 and <= 0xCF && marker is not 0xC4 and not 0xC8 and not 0xCC;
    }

    private static bool IsPng(ReadOnlySpan<byte> bytes)
    {
        return bytes[0] == 0x89 && bytes[1] == (byte)'P' && bytes[2] == (byte)'N' && bytes[3] == (byte)'G';
    }

    private static bool IsGif(ReadOnlySpan<byte> bytes)
    {
        return bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F';
    }

    private static bool IsBmp(ReadOnlySpan<byte> bytes)
    {
        return bytes[0] == (byte)'B' && bytes[1] == (byte)'M';
    }

    private static bool IsJpeg(ReadOnlySpan<byte> bytes)
    {
        return bytes[0] == 0xFF && bytes[1] == 0xD8;
    }

    private static bool IsWebp(ReadOnlySpan<byte> bytes)
    {
        return bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
            && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P';
    }

    private static int ReadInt32BigEndian(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }

    private static int ReadInt32LittleEndian(ReadOnlySpan<byte> bytes)
    {
        return bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
    }

    private static int ReadUInt16BigEndian(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 8) | bytes[1];
    }

    private static int ReadUInt16LittleEndian(ReadOnlySpan<byte> bytes)
    {
        return bytes[0] | (bytes[1] << 8);
    }

    private static int Read24LittleEndian(ReadOnlySpan<byte> bytes)
    {
        return bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);
    }
}
