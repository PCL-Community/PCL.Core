using System.IO;
using System.Text;

namespace PCL.Core.Helper.Hash;

public interface IHashProvider
{
    string ComputeHash(Stream input);
    string ComputeHash(byte[] input);
    string ComputeHash(string input, Encoding? en = null);
}