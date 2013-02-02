using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace GodaiLibrary
{
    /*
     * network転送用ラッパークラス
     */
    public class Network
    {
        private NetworkStream mStream;
        private bool mClosed = false;
        private ImageConverter mConv = new ImageConverter();

        public Network(NetworkStream ns)
        {
            this.mStream = ns;
        }

        public int receiveByte()
        {
            //            int nByte = this.mStream.ReadByte();
            byte [] data = new byte[1];
            while (true)
            {
                int nRead = this.mStream.Read(data, 0, 1);
                if (nRead == 0)
                    continue;
                if (nRead < 0)
                {
                    this.mClosed = true;
                }
                break;
            }
            return data[0];
        }

        public bool isClosed()
        {
            return this.mClosed;
        }

        public void sendByte(byte data)
        {
            this.mStream.WriteByte(data);
        }

        public int receiveWORD()
        {
            int nb1 = receiveByte();
            int nb2 = receiveByte();
            if (isClosed())
                return 0;
            else
                return (nb1 << 8) + nb2;
        }

        public void sendWORD(int nData)
        {
            this.mStream.WriteByte((byte)(nData >> 8));
            this.mStream.WriteByte((byte)(nData & 0xff));
        }

        public int receiveDWORD()
        {
            int nb1 = receiveByte();
            int nb2 = receiveByte();
            int nb3 = receiveByte();
            int nb4 = receiveByte();
            if (isClosed())
                return 0;
            else
                return (nb1 << 24) + (nb2 << 16) + (nb3 << 8) + nb4;
        }

        public void sendDWORD(int nData)
        {
            this.mStream.WriteByte((byte)(nData >> 24));
            this.mStream.WriteByte((byte)(nData >> 16));
            this.mStream.WriteByte((byte)(nData >> 8));
            this.mStream.WriteByte((byte)(nData & 0xff));
        }

        public double receiveDouble()
        {
            
            long nb1 = receiveByte();
            long nb2 = receiveByte();
            long nb3 = receiveByte();
            long nb4 = receiveByte();
            int nb5 = receiveByte();
            int nb6 = receiveByte();
            int nb7 = receiveByte();
            int nb8 = receiveByte();

            if (isClosed())
                return 0;
            else {
                long value = (nb1 << 56) + (nb2 << 48) + (nb3 << 40) + (nb4 << 32) + (nb5 << 24) + (nb6 << 16) + (nb7 << 8) + nb8;
                return BitConverter.Int64BitsToDouble(value);
            }
        }

        public void sendDouble(double dData)
        {
            long nVal = BitConverter.DoubleToInt64Bits(dData);
            sendDWORD((int)(nVal >> 32));
            sendDWORD((int)(nVal & 0xffff));
        }

        public long receiveLength()
        {
            int nByte = receiveByte();
            if ((nByte & 0xf0) == 0)
            {
                return nByte & 0x0f;
            }
            int nLen = nByte >> 4;
            long nRet = nByte & 0x0f;
            for (int it = 0; it < nLen; ++it)
            {
                nRet = (nRet << 8) + receiveByte();
            }
            return nRet;
        }

        public void sendString(string str)
        {
            byte [] byteArray = System.Text.Encoding.Unicode.GetBytes(str);
            sendLength(byteArray.Length);
            this.mStream.Write(byteArray, 0, byteArray.Length);
        }

        public String receiveString()
        {
            long nLen = receiveLength();
            if (nLen == 0)
                return "";
            byte[] byteArray = new byte[nLen];

            int nPos = 0;
            while (nLen > 0)
            {
                int nReadByte = this.mStream.Read(byteArray, nPos, (int)nLen);
                if (nReadByte <= 0)
                    break;
                nPos += nReadByte;
                nLen -= nReadByte;
            }
            return System.Text.Encoding.Unicode.GetString(byteArray);
        }

        public void sendLength(long nLength)
        {
            if (nLength < 0x10)
            {
                sendByte((byte)nLength);
            }
            else if (nLength < 0xfff)
            {
                sendByte((byte)((nLength >> 8) | 0x10));
                sendByte((byte)(nLength & 0xff));
            }
            else if (nLength < 0xfffff)
            {
                sendByte((byte)((nLength >> 16) | 0x20));
                sendByte((byte)((nLength >> 8) & 0xff));
                sendByte((byte)(nLength & 0xff));
            }
            else if (nLength < 0xfffffff)
            {
                sendByte((byte)((nLength >> 24) | 0x30));
                sendByte((byte)((nLength >> 16) & 0xff));
                sendByte((byte)((nLength >> 8) & 0xff));
                sendByte((byte)((nLength) & 0xff));
            }
            else
                throw new ArgumentException("Too large data. It cannot transfer");
        }

        public Image receiveImage()
        {
            byte[] byteImage = receiveBinary();
            if (byteImage == null)
                return null;
            return (Image)mConv.ConvertFrom(byteImage);
        }

        public byte[] receiveBinary()
        {
            int nLen = (int) this.receiveLength();
            if (nLen == 0)
                return null;
            byte[] ret = new byte[nLen];
            int size = nLen;
            int offset = 0;
            while (size > 0)
            {
                int nReadLen = this.mStream.Read(ret, offset, size);
                if (nReadLen < 0)
                    return null;
                size -= nReadLen;
                offset += nReadLen;
            }
            return ret;
        }

        public void sendImage(Image image)
        {
            byte[] byteImage = (byte[])mConv.ConvertTo(image, typeof(byte[]));
            this.sendBinary(byteImage);
        }

        public void sendBinary(byte[] data)
        {
            this.sendLength(data.Length);
            this.mStream.Write(data, 0, data.Length);
        }

        public void flush()
        {
            this.mStream.Flush();
        }

        public void disconnect()
        {
            this.mStream.Close();
        }

        /// ファイルを受信する
        public static void receiveFiles(Network network, String strDirectory, FolderLANSync.SyncFolderMaster parent)
        {
            if ( strDirectory.Length > 3 && !Directory.Exists(strDirectory))
                Directory.CreateDirectory(strDirectory);

            while (true)
            {
                byte nFlag = (byte)network.receiveByte();
                if (nFlag == 0)
                    break;

                String strFilePath = network.receiveString();

                parent.addLog("「" + strFilePath + "」を受信開始");

                byte[] data = network.receiveBinary();  // 先に受信してしまう

                strFilePath = Path.Combine(strDirectory, strFilePath);
                parent.beginReceiveFile(strFilePath);   //フォルダを変更する前に呼び出す．監視をオフにする。

#if true
                // スレッド同期とってないし封印
                {
                    ReceiveThread receiveThread = new ReceiveThread(data, parent, strFilePath);
                    Thread thread = new Thread(new ThreadStart(receiveThread.receiveFileThread));
                    thread.Start();
                }
#else
                Directory.CreateDirectory(Path.GetDirectoryName(strFilePath));

                System.IO.FileStream wout = null;
                for (int it = 0; it < 3; ++it)
                {
                    try
                    {
                        wout = new System.IO.FileStream(strFilePath, FileMode.Create);
                    }
                    catch (Exception ex)
                    {
                        parent.addLog("「" + strFilePath + "」が上書きできませんでした");
                        System.Threading.Thread.Sleep(500);
                    }
                    if (wout != null)
                    {
                        if (it > 0)
                        {
                            parent.addLog("「" + strFilePath + "」に書き込めました");
                        }
                    }
                }

                if (data != null)
                {
                    System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(new MemoryStream(data), System.IO.Compression.CompressionMode.Decompress);
                    byte[] buffer = new byte[1024 * 1024 * 50];
                    while (true)
                    {
                        int nReadSize = gzip.Read(buffer, 0, buffer.Length);
                        if (nReadSize == 0)
                            break;
                        if ( wout != null )
                            wout.Write(buffer, 0, nReadSize);
                    }
                    gzip.Close();
                }
                parent.addLog("「" + strFilePath + "」を受信終了");
                if (wout != null)
                   wout.Close();

                parent.endReceiveFile(strFilePath);   //フォルダを変更する前に呼び出す．監視をオフにする。
#endif
            }
        }


        /// ファイルを送信する
        public static void sendFiles(Network network, String strDirectoryOrFile)
        {
            try
            {
                if (File.Exists(strDirectoryOrFile))
                {
                    sendFile(network, Path.GetFileName(strDirectoryOrFile), Path.GetFullPath(strDirectoryOrFile), "");
                }
                else
                {
                    sendFilesSub(network, strDirectoryOrFile, "");
                }
            }
            finally
            {
                network.sendByte(0);
            }
        }

        private static void sendFilesSub(Network network, String strDirectory, String strSubPath)
        {
            DirectoryInfo dirinfo = new DirectoryInfo(strDirectory);
            foreach (var file in dirinfo.GetFiles())
            {
                sendFile(network, file.Name, file.FullName, strSubPath);
            }
            foreach (var dir in dirinfo.GetDirectories())
            {
                sendFilesSub(network, dir.FullName, Path.Combine(strSubPath, dir.Name));
            }
        }

        // ファイル転送 strSubPathは付加するフォルダ名
        public static void sendFile(Network network, String strFileName, String strFullPath, String strSubPath)
        {
            try
            {
                MemoryStream memory = new MemoryStream();
                bool bWrite = false;
                using (System.IO.FileStream win = new System.IO.FileStream(strFullPath, FileMode.Open, FileAccess.Read))
                {
                    System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(memory, System.IO.Compression.CompressionMode.Compress);
//                    System.IO.Compression.DeflateStream gzip = new System.IO.Compression.DeflateStream(memory, System.IO.Compression.CompressionMode.Compress);
                    byte[] buffer = new byte[1024 * 1024 * 50];
                    while (true)
                    {
                        int nReadSize = win.Read(buffer, 0, buffer.Length);
                        if (nReadSize == 0)
                            break;
                        bWrite = true;
                        gzip.Write(buffer, 0, nReadSize);
//                        memory.Write(buffer, 0, nReadSize);
                    }
                    gzip.Close();
                }

                if (bWrite)
                {
                    network.sendByte(1);
                    String strTransferPath = Path.Combine(strSubPath, strFileName);
                    network.sendString(strTransferPath);

                    byte[] data = memory.ToArray();
                    network.sendBinary(data);
                }

                memory.Close();
            }
            catch (Exception ex)
            {
                throw ex;
//                MessageBox.Show(ex.Message);
            }
        }
    }

    public class ReceiveThread
    {
        public ReceiveThread(byte[] data, FolderLANSync.SyncFolderMaster parent, String strFilePath)
        {
            this.mData = data;
            this.mParent = parent;
            this.mStrFilePath = strFilePath;
        }

        private byte[] mData;
        private FileStream mWout;
        private FolderLANSync.SyncFolderMaster mParent;
        private String mStrFilePath;

        public void receiveFileThread()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(this.mStrFilePath));

            System.IO.FileStream wout = null;
            for (int it = 0; it < 3; ++it)
            {
                try
                {
                    wout = new System.IO.FileStream(this.mStrFilePath, FileMode.Create);
                }
                catch (Exception ex)
                {
                    this.mParent.addLog("「" + this.mStrFilePath + "」が上書きできませんでした");
                    System.Threading.Thread.Sleep(500);
                }
                if (wout != null)
                {
                    if (it > 0)
                    {
                        this.mParent.addLog("「" + this.mStrFilePath + "」に書き込めました");
                    }
                    break;
                }
            }

            if (this.mData != null)
            {
                System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(new MemoryStream(this.mData), System.IO.Compression.CompressionMode.Decompress);
//                System.IO.Compression.DeflateStream gzip = new System.IO.Compression.DeflateStream(new MemoryStream(this.mData), System.IO.Compression.CompressionMode.Decompress);
//                MemoryStream gzip = new MemoryStream(this.mData);
                byte[] buffer = new byte[1024 * 1024];
                while (true)
                {
                    int nReadSize = gzip.Read(buffer, 0, buffer.Length);
                    if (nReadSize == 0)
                        break;
                    if (wout != null)
                        wout.Write(buffer, 0, nReadSize);
                }
                gzip.Close();
            }
            this.mParent.addLog("「" + this.mStrFilePath + "」を受信終了");

            this.mParent.endReceiveFile(this.mStrFilePath);   
        }
    }
}
