/*
部分内容参考了 https://github.com/LogosBible/bsdiff.net 的实现

Copyright 2010-2024 Logos Bible Software

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.


Copyright 2003-2005 Colin Percival
All rights reserved

Redistribution and use in source and binary forms, with or without
modification, are permitted providing that the following conditions
are met:
1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.IO;
using System.Threading.Tasks;

namespace PCL.Core.Helper.Diff;


public class BsDiff : IBinaryDiff
{
	private const int HeaderSize = 32; // 32-byte header
	private const int HeaderVersionIndex = 0;
	private const long HeaderVersion = 0x3034464649445342; // "BSDIFF40" in little-endian
	private const int HeaderCtrlIndex = 8;
	private const int HeaderDiffIndex = 16;
	private const int HeaderNewSizeIndex = 24;

	
	public async Task<byte[]> Apply(byte[] originData, byte[] diffData)
	{
		if (diffData.Length < HeaderSize)
			throw new Exception("Diff file size is less than the header size");
		if (BitConverter.ToInt64(diffData, HeaderVersionIndex) != HeaderVersion)
			throw new Exception("Diff file version is wrong");
		
		// 读取 Header 信息
		long ctrlLen = BitConverter.ToInt64(diffData, HeaderCtrlIndex);
		long diffLen = BitConverter.ToInt64(diffData, HeaderDiffIndex);
		long newLen = BitConverter.ToInt64(diffData, HeaderNewSizeIndex);
		long extraLen = diffData.Length - HeaderSize - ctrlLen - diffLen;
		
		if (ctrlLen < 0 || diffLen < 0 || extraLen < 0)
			throw new Exception("Block size is negative");
		if (newLen < 0)
			throw new Exception("Final file size info is negative");
		if (HeaderSize + ctrlLen + diffLen + extraLen > diffData.Length)
			throw new Exception("Diff file size info is not correct");
		
		var ctrlContent = new byte[ctrlLen];
		// 获取 Control 数据
		long curOffset = HeaderSize;
		Array.Copy(diffData, curOffset, ctrlContent, 0, ctrlLen);
		using var ctrlStream = new MemoryStream(ctrlContent);
		using var ctrlReader = new BinaryReader(ctrlStream);
		// 获取 Diff 数据
		curOffset += ctrlLen;
		var diffContent = new byte[diffLen];
		Array.Copy(diffData, curOffset, diffContent, 0, diffLen);
		using var diffStream = new MemoryStream(diffContent);
		using var diffReader = new BinaryReader(diffStream);
		// 获取 Extra 数据
		curOffset += diffLen;
		var extraContent = new byte[newLen];
		Array.Copy(diffData, curOffset, extraContent, 0, extraLen);
		using var extraStream = new MemoryStream(extraContent);
		using var extraReader = new BinaryReader(extraStream);

		var ret = new byte[newLen];

		long newDataPos = 0;
		long oldDataPos = 0;
		while (newDataPos < newLen)
		{
			long addRange = ctrlReader.ReadInt64();
			long copyRange = ctrlReader.ReadInt64();
			long seekPos = ctrlReader.ReadInt64();
			
			// 新加入的
			if (newDataPos + addRange > newLen)
				throw new Exception("Add range overflows");

			for (int i = 0; i < addRange; i++)
			{
				if (oldDataPos + i < originData.Length)
					ret[newDataPos + i] = (byte)(diffReader.ReadByte() + originData[oldDataPos + i]);
				else
					ret[newDataPos + i] = diffReader.ReadByte();
			}
			
			newDataPos += addRange;
			oldDataPos += addRange;
			
			// 原有的
			if (newDataPos + copyRange > newLen)
				throw new Exception("Copy range overflows");

			for (int i = 0; i < copyRange; i++)
			{
				ret[newDataPos + i] = extraReader.ReadByte();
			}
			newDataPos += copyRange;
			
			// 原有的切换到指定位置继续读取
			oldDataPos = oldDataPos + seekPos;
			if (oldDataPos < originData.Length)
				throw new Exception("Old data pos overflows");
		}
		
		return ret;
	}

	public async Task<byte[]> Make(byte[] originData, byte[] newData)
	{
		throw new NotImplementedException();
	}
}