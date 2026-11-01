using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Orblit.Examples;
using Orblit.Filament;

namespace Orblit.Gallery.Tests;

/// <summary>
/// The bytes the recorded digests were made from, written the same way, for the one example whose
/// fixture case lives here. A message value is its type letter, its length and its raw bytes. A digest
/// is FNV-1a over 64 bits. The engine's tests keep the same code for the rest, because they cannot be
/// seen from here.
/// </summary>
internal static class SceneDigest
{
    /// <summary>An absent key is a letter no value uses, so absent and empty differ.</summary>
    public static readonly byte[] Absent = Shaped('n', 0, []);

    /// <summary>What an example sends the renderer at one moment, each value in its byte form.</summary>
    public static Dictionary<string, byte[]> Message(Example example, double seconds) =>
        example.Scene(example.ViewPoint.ToRenderCamera(), seconds).ToMessage(seconds)
            .ToDictionary(entry => entry.Key, entry => Form(entry.Value));

    /// <summary>A digest of one message, key by key in ordinal order.</summary>
    public static string Of(Dictionary<string, byte[]> message)
    {
        var digest = new Digest();
        foreach (var key in message.Keys.Order(StringComparer.Ordinal))
        {
            digest.Add(Text(key));
            digest.Add(message[key]);
        }

        return digest.Hex;
    }

    /// <summary>A digest of one key across <paramref name="frames"/>, with the key absent where a frame lacks it.</summary>
    public static string OfKey(string key, IEnumerable<Dictionary<string, byte[]>> frames)
    {
        var digest = new Digest();
        foreach (var frame in frames)
        {
            digest.Add(frame.GetValueOrDefault(key) ?? Absent);
        }

        return digest.Hex;
    }

    private static byte[] Form(object value) => value switch
    {
        float[] list => Shaped('f', list.Length, MemoryMarshal.AsBytes<float>(list).ToArray()),
        int[] list => Shaped('i', list.Length, MemoryMarshal.AsBytes<int>(list).ToArray()),
        long[] list => Shaped('l', list.Length, MemoryMarshal.AsBytes<long>(list).ToArray()),
        byte[] list => Shaped('b', list.Length, list),
        string[] list => Shaped('s', list.Length, [.. list.SelectMany(Text)]),
        bool flag => Shaped('z', 1, [flag ? (byte)1 : (byte)0]),
        double number => Shaped('d', 1, BitConverter.GetBytes(number)),
        string text => Shaped('t', 1, Text(text)),
        _ => throw new InvalidOperationException($"No form for {value.GetType()}."),
    };

    private static byte[] Shaped(char tag, int length, byte[] bytes) =>
        [(byte)tag, .. BitConverter.GetBytes(length), .. bytes];

    /// <summary>A string as its UTF-8 length and then its UTF-8 bytes.</summary>
    private static byte[] Text(string value)
    {
        var encoded = Encoding.UTF8.GetBytes(value);
        return [.. BitConverter.GetBytes(encoded.Length), .. encoded];
    }

    private sealed class Digest
    {
        private ulong _hash = 0xcbf29ce484222325;

        public string Hex => _hash.ToString("x16", CultureInfo.InvariantCulture);

        public void Add(byte[] data)
        {
            foreach (var b in data)
            {
                _hash = unchecked((_hash ^ b) * 0x100000001b3);
            }
        }
    }
}
