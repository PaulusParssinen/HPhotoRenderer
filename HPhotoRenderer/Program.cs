using System.Buffers.Binary;

if (args is not [string filePath])
{
    Console.WriteLine("You must provide path to MUS binary as an argument.");
    return;
}

Rgba32[] grayscalePalette = ReadPalette("grayscale.pal");

// TODO: Eventually use Shockky for all of this.
Span<byte> musBytes = File.ReadAllBytes(filePath);

// BitmapCastProperties
ushort stride = BinaryPrimitives.ReadUInt16BigEndian(musBytes[28..]);

ushort top = BinaryPrimitives.ReadUInt16BigEndian(musBytes[30..]);
ushort left = BinaryPrimitives.ReadUInt16BigEndian(musBytes[32..]);
ushort bottom = BinaryPrimitives.ReadUInt16BigEndian(musBytes[34..]);
ushort right = BinaryPrimitives.ReadUInt16BigEndian(musBytes[36..]);

// Skip other properties we don't need for photo rendering.

byte bitDepth = 1;
(short LibNum, short MemberNum) paletteRef = default;
if ((stride & 0x8000u) != 0)
{
    stride &= 0x3FFF;

    bitDepth = musBytes[51];
    paletteRef = (BinaryPrimitives.ReadInt16BigEndian(musBytes[52..]), BinaryPrimitives.ReadInt16BigEndian(musBytes[54..]));
}

if (bitDepth != 8)
{
    Console.WriteLine("BitmapCastProperties.BitDepth != 8");
    return;
}

if (paletteRef.MemberNum - 1 != -3)
{
    Console.WriteLine("BitmapCastProperties.PaletteRef != Grayscale");
    return;
}

// Now comes the BitmapData (BITD)
int width = stride;
int height = bottom - top;

int byteLength = stride * width;
byte[] decompressedBitmapBytes = new byte[byteLength];

int compressedBytesLength = BinaryPrimitives.ReadInt32LittleEndian(musBytes[64..]);
if (!TryDecompress(musBytes.Slice(68, compressedBytesLength), decompressedBitmapBytes, out int bytesWritten))
{
    Console.WriteLine("Failed to decompress the RLE bitmap data.");
    return;
}

using var image = new Image<Rgba32>(width, height);

// Photos only use 8bpp
image.ProcessPixelRows(pa =>
{
    for (int y = 0; y < pa.Height; y++)
    {
        Span<byte> paletteIndexRow = decompressedBitmapBytes.AsSpan(y * pa.Width, pa.Width);

        Span<Rgba32> imageRow = pa.GetRowSpan(y);
        for (int x = 0; x < imageRow.Length; x++)
        {
            imageRow[x] = grayscalePalette[paletteIndexRow[x]];
        }
    }
});

//TODO: Ink 41(-8?=AddPin), #681F10

image.SaveAsPng($"{Path.GetFileNameWithoutExtension(filePath)}.png");

static Rgba32[] ReadPalette(string fileName)
{
    using var fs = new FileStream(fileName, FileMode.Open);
    using var input = new BinaryReader(fs);
    input.BaseStream.Position = 22;

    Rgba32[] colors = new Rgba32[input.ReadInt16()];
    for (int i = 0; i < colors.Length; i++)
    {
        colors[i] = new Rgba32(input.ReadByte(), input.ReadByte(), input.ReadByte());
        input.ReadByte();
    }
    return colors;
}

// RLE decompresison.
static bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
{
    bytesWritten = 0;

    int position = 0;
    while (position < source.Length)
    {
        byte marker = source[position++];
        if ((marker & 0x80) != 0)
        {
            int length = 257 - marker;
            destination.Slice(bytesWritten, length).Fill(source[position++]);
            bytesWritten += length;
        }
        else
        {
            int length = marker + 1;
            source.Slice(position, length).CopyTo(destination.Slice(bytesWritten));

            bytesWritten += length;
            position += length;
        }
    }
    return true;
}