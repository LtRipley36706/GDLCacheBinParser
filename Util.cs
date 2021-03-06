﻿using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PhatACCacheBinParser
{
	static class Util
	{
		public static readonly Regex IllegalInFileName = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled);


	    /// <summary>
	    /// If the length of the string is 0, null will be returned.
	    /// </summary>
        public static string ReadString(BinaryReader binaryReader, bool alignTo4Bytes)
		{
			var len = binaryReader.ReadUInt16();

			var bytes = binaryReader.ReadBytes(len);

			if (alignTo4Bytes)
			{
				// Make sure our position is a multiple of 4
				if (binaryReader.BaseStream.Position % 4 != 0)
					binaryReader.BaseStream.Position += 4 - (binaryReader.BaseStream.Position % 4);
			}

		    if (len == 0)
		        return null;

            return Encoding.ASCII.GetString(bytes);
		}

		/// <summary>
		/// Each byte of the string has it's upper and lower nibble swapped.<para />
        /// If the length of the string is 0, null will be returned.
		/// </summary>
		public static string ReadEncryptedString1(BinaryReader binaryReader, bool alignTo4Bytes)
		{
			var len = binaryReader.ReadUInt16();

            var bytes = binaryReader.ReadBytes(len);

			for (int i = 0; i < bytes.Length; i++)
			{
				var b = bytes[i];
				bytes[i] = (byte)((b >> 4) | (b << 4));
			}

			if (alignTo4Bytes)
			{
				// Make sure our position is a multiple of 4
				if (binaryReader.BaseStream.Position % 4 != 0)
					binaryReader.BaseStream.Position += 4 - (binaryReader.BaseStream.Position % 4);
			}

		    if (len == 0)
		        return null;

            return Encoding.ASCII.GetString(bytes);
		}

	    public static int ReadPackedKnownType(BinaryReader binaryReader, int knownType)
	    {
	        int result = binaryReader.ReadUInt16();

	        if ((result & 0x8000) == 0x8000)
	        {
	            var lower = binaryReader.ReadUInt16();
                result = ((result & 0x3FFF) << 16) | lower; // Should this be masked with 0x7FFF instead?
	        }

            return knownType + result;
	    }
	}
}
