using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Encryption
{
    public class AesCbc : IEncryptionProvider
    {
        public byte[] Decrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
        {
            throw new NotImplementedException();
        }

        public byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
        {
            throw new NotImplementedException();
        }
    }
}
