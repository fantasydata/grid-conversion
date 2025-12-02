using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SportsDataIO.GRidConversion
{
    public static class Base62
    {
        /// <summary>
        /// Character set (and order) for Base62 conversion from UUID
        /// </summary>
        public const string CHARACTER_SET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        /// <summary>
        /// Separator used between type identifier & UUID
        /// </summary>
        public const char SEPARATOR = '_';

        /// <summary>
        /// Encodes a UUID with the type identifier prefix into the GRid
        /// For C# Guid implementations of UUID, some endian-ness conversions (to big endian to match RFC 4122 / RFC 9562) are required
        /// other languages may or may not need this depending on the endian-ness of
        /// </summary>
        /// <param name="gruuid"></param>
        /// <param name="typeIdentifier"></param>
        /// <returns></returns>
        public static string Encode(Guid gruuid, string typeIdentifier)
        {
            byte[] bytes = gruuid.ToByteArray();
            // Microsoft stores the first 3 parts of a GUID (Data1, Data2, Data3) in Little Endian 
            // format on Little Endian architectures (like x86/x64). 
            // To ensure the Base62 string is sortable and matches the logical Big Endian 
            // string representation (RFC 4122), we must flip them to Big Endian.
            if (BitConverter.IsLittleEndian) // checks architecture
            {
                Array.Reverse(bytes, 0, 4); // Swap Data1 (int)
                Array.Reverse(bytes, 4, 2); // Swap Data2 (short)
                Array.Reverse(bytes, 6, 2); // Swap Data3 (short)
            }
            // At this point, 'bytes' is in RFC 4122 Big Endian order (Network Byte Order). 
            // Example: The byte at index 0 is the Most Significant Byte. 
            // However, the BigInteger constructor expects a Little Endian byte array 
            // (where index 0 is the Least Significant Byte). 
            // To treat the GUID as a single large number where the start of the GUID is the "top", 
            // we must reverse the entire array.
            Array.Reverse(bytes); 
            // Append a zero byte to ensure the BigInteger is treated as positive
            byte[] positiveBytes = new byte[17];
            Array.Copy(bytes, positiveBytes, 16);
            var number = new BigInteger(positiveBytes);
            // 128 bits fits into 22 Base62 characters
            char[] buffer = new char[22];
            for (int i = 21; i >= 0; i--)
            {
                number = BigInteger.DivRem(number, 62, out BigInteger remainder);
                buffer[i] = CHARACTER_SET[(int)remainder];
            }
            var encodedUUID =  new string(buffer);
            var sb = new StringBuilder();
            sb.Append(typeIdentifier);
            sb.Append(SEPARATOR);
            sb.Append(encodedUUID);
            return sb.ToString();
        }

        public static Guid Decode(string input)
        {
            // Split Type ID if listed
            int separatorIndex = input.LastIndexOf(SEPARATOR);
            string idPart = separatorIndex > -1 ? input.Substring(separatorIndex + 1) : input;

            // Decode Base62 back to BigInteger
            BigInteger number = BigInteger.Zero;
            BigInteger base62 = 62;

            foreach (char c in idPart)
            {
                int val = CHARACTER_SET.IndexOf(c);
                if (val == -1) throw new FormatException($"Invalid character '{c}' in Base62 string.");
                number = number * base62 + val;
            }

            // Get Bytes (BigInteger returns LE)
            byte[] bytes = number.ToByteArray();

            // Pad or Trim to 16 bytes
            // BigInteger might return 17 bytes (sign bit) or fewer than 16 (leading zeros)
            byte[] guidBytes = new byte[16];
            int lengthToCopy = Math.Min(bytes.Length, 16);
            Array.Copy(bytes, guidBytes, lengthToCopy);

            // Reverse from LE (BigInt format) to BE (RFC format) due to bigint LE-ness
            Array.Reverse(guidBytes);

            // Restore Microsoft Endianness where needed
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(guidBytes, 0, 4);
                Array.Reverse(guidBytes, 4, 2);
                Array.Reverse(guidBytes, 6, 2);
            }

            return new Guid(guidBytes);
        }
    }
}
