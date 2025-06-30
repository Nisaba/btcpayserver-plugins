namespace BTCPayServer.Plugins.MtPelerin.Services
{
    using System;
    using System.IO;
    using System.Text;
    using NBitcoin;
    using NBitcoin.Crypto;

    public static class BitcoinMessageSigner
    {
        public static string SignMessageBitcoin(this Key key, string message, Network network)
        {
            byte[] messageBytes = FormatMessageForSigning(message);
            var hash = Hashes.DoubleSHA256(messageBytes);
            var sig = key.SignCompact(hash, key.PubKey.IsCompressed);
            return Convert.ToBase64String(sig.Signature);  
        }

        private static byte[] FormatMessageForSigning(string message)
        {
            var magic = "Bitcoin Signed Message:\n";
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            WriteVarString(writer, magic);
            WriteVarString(writer, message);
            return ms.ToArray();
        }

        private static void WriteVarString(BinaryWriter writer, string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            WriteVarInt(writer, bytes.Length);
            writer.Write(bytes);
        }

        private static void WriteVarInt(BinaryWriter writer, int value)
        {
            if (value < 0xfd)
            {
                writer.Write((byte)value);
            }
            else if (value <= 0xffff)
            {
                writer.Write((byte)0xfd);
                writer.Write((ushort)value);
            }
            else
            {
                writer.Write((byte)0xfe);
                writer.Write(value);
            }
        }
    }
}
