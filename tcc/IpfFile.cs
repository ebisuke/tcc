using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tcc
{
    /// <summary>
    /// ebiカスタマイズ IPF
    /// </summary>
    public class IpfFile
    {
        public IpfFile(FileStream fs)
        {

        }

		private int CheckIpf(FileStream fs, StreamWriter sw)
		{
			if (fs.Length < 44)
			{
                throw new FormatException("IPFサイズ不足　(最低44バイト)");
				return -1;
			}
			if (fs.Length > Int32.MaxValue)
			{
                throw new FormatException("IPFサイズ超過　(最大" + Int32.MaxValue + "バイト)");
				return -2;
			}
			ipfLen = (int)fs.Length; // ファイルのサイズ


			byte[] tmpBuf = new byte[24];
			ReadFile(fs, tmpBuf, ipfLen - 24, 24);
			if ((tmpBuf[12] != 0x50) || (tmpBuf[13] != 0x4B) || (tmpBuf[14] != 0x05) || (tmpBuf[15] != 0x06))
			{
				throw new FormatException("IPFフッタ不正");
				return -3;
			}
			ipfFileCnt = tmpBuf[0] + (tmpBuf[1] * 0x100);
			ipfFileTblPos = tmpBuf[2] + (tmpBuf[3] * 0x100) + (tmpBuf[4] * 0x10000) + (tmpBuf[5] * 0x1000000);
			ipfFileFtrPos = tmpBuf[8] + (tmpBuf[9] * 0x100) + (tmpBuf[10] * 0x10000) + (tmpBuf[11] * 0x1000000);
			ipfTgtVer = tmpBuf[16] + (tmpBuf[17] * 0x100U) + (tmpBuf[18] * 0x10000U) + (tmpBuf[19] * 0x1000000U);
			ipfPkgVer = tmpBuf[20] + (tmpBuf[21] * 0x100U) + (tmpBuf[22] * 0x10000U) + (tmpBuf[23] * 0x1000000U);

			//ExPrint("  ファイル数:" + ipfFileCnt + " [0x" + ipfFileCnt.ToString("X4") + "]", sw);
			//ExPrint("  TargetVer:[" + ipfTgtVer + "]  PackageVer:[" + ipfPkgVer + "]", sw);
			if (ipfFileTblPos > ipfLen - 24 || ipfFileTblPos < 0)
			{
                throw new FormatException("IPFフッタ不正　IPF内テーブル始");
				return -4;
			}
			if (ipfFileFtrPos > ipfLen - 24 || ipfFileFtrPos < 0)
			{
                throw new FormatException("IPFフッタ不正　IPF内フッタ位置");
				return -5;
			}

			int tmpPos = ipfFileTblPos;
			for (int i = 0; i < ipfFileCnt; i++)
			{
				//Print("IPFテーブル解析 "+(i+1));
				if (tmpPos < 0)
				{
					throw new FormatException("IPFテーブル不正　開始位置");
					
				}
				FileTableInf fti = new FileTableInf();

				tmpBuf = new byte[24];
				ReadFile(fs, tmpBuf, tmpPos, 20);
				int archNmLen = tmpBuf[18] + (tmpBuf[19] * 0x100);
				int fileNmLen = tmpBuf[0] + (tmpBuf[1] * 0x100);
				fti.fileCrc = (uint)(tmpBuf[2] + (tmpBuf[3] * 0x100) + (tmpBuf[4] * 0x10000)) + ((uint)tmpBuf[5] * 0x1000000U);
				fti.compLen = tmpBuf[6] + (tmpBuf[7] * 0x100) + (tmpBuf[8] * 0x10000) + (tmpBuf[9] * 0x1000000);
				fti.deplLen = tmpBuf[10] + (tmpBuf[11] * 0x100) + (tmpBuf[12] * 0x10000) + (tmpBuf[13] * 0x1000000);
				fti.dataPos = tmpBuf[14] + (tmpBuf[15] * 0x100) + (tmpBuf[16] * 0x10000) + (tmpBuf[17] * 0x1000000);
				tmpBuf = new byte[archNmLen];
				ReadFile(fs, tmpBuf, tmpPos + 20, archNmLen);
				fti.archNm = Encoding.UTF8.GetString(tmpBuf);
				tmpBuf = new byte[fileNmLen];
				ReadFile(fs, tmpBuf, tmpPos + 20 + archNmLen, fileNmLen);
				fti.fileNm = Encoding.UTF8.GetString(tmpBuf);
				//Print("  ファイル名:"+fti.archNm+" | "+fti.fileNm+"  ファイル位置:"+fti.dataPos+" [0x"+fti.dataPos.ToString("X8")+"]  圧縮サイズ　:"+fti.compLen+" [0x"+fti.compLen.ToString("X8")+"]");
				//Print("  CRC:"+fti.fileCrc.ToString("X8"));

				lstFileTab.Add(fti);
				tmpPos += 20 + fileNmLen + archNmLen;
			}

			return 0;
		}
        private static void ReadFile(FileStream fs, byte[] buf, int seek, int size)
        {
            fs.Seek(seek, SeekOrigin.Begin);
            int readSize = 0;
            while (size > readSize)
            {
                readSize += fs.Read(buf, readSize, size - readSize);
            }
        }
		class FileTableInf
		{
			public string archNm;
			public string fileNm;
			public uint fileCrc;
			public int compLen;
			public int deplLen;
			public int dataPos;

			//public string filePath;//解凍時は使わない
		}
        HashSet<string> nonCompExtnt = new HashSet<string>();

        List<FileTableInf> lstFileTab = new List<FileTableInf>();
        int ipfLen;
        int ipfFileCnt;
        int ipfFileTblPos;
        int ipfFileFtrPos;
        uint ipfTgtVer;
        uint ipfPkgVer;
	}
}
