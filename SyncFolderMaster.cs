using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;

namespace FolderLANSync
{
    public class SyncFolderMaster
    {
        private FormMain mParent;

        private FileSystemWatcher mWathcer;

        private List<Regex> mExtList = new List<Regex>();
        private String mSyncFolder;
        private String mHostName;
        private int mPort;
        private bool mError = false;
        private bool mStopFlag = false;
        private int mMaxLimit = 1024 * 1024 * 10;

        private ReceiveFileList mReceiveFileInfoList = new ReceiveFileList(); 

        private GodaiLibrary.Network mNetwork;

        public SyncFolderMaster(FormMain parent)
        {
            this.mParent = parent;
        }

        public void setMaxLimit(int nMaxLimit)
        {
            this.mMaxLimit = nMaxLimit;
        }

        public void addSyncExt(String strExt)
        {
            Regex regex = new Regex(strExt);
            this.mExtList.Add(regex);
        }

        public void setHostName(String strHostName)
        {
            this.mHostName = strHostName;
        }

        public void setPort(int nPort)
        {
            this.mPort = nPort;
        }

        public void setSyncFolder(String strFolder)
        {
            if (this.mSyncFolder != null)
            {
                MessageBox.Show("複数の同期フォルダが指定されています");
                this.mError = true;
                return;
            }

            if (strFolder.Length < 4)
            {
                MessageBox.Show("同期フォルダのパスが短すぎます");
                this.mError = true;
                return;
            }

            this.mSyncFolder = strFolder;

            if (!this.mSyncFolder.EndsWith("\\"))
                this.mSyncFolder += "\\";
        }


        public bool isError()
        {
            if (mError)
                return true;

            return checkError();
        }

        private bool checkError() {
            if (this.mSyncFolder == null)
            {
                return true;
            }

            return false;
        }

        public void run()
        {
            while (true)
            {
                try
                {
                    runSub();
                }
                catch (Exception ex)
                {
                    this.mNetwork = null;
//                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void runSub() {
 
            while(this.mNetwork == null ) {
                if (this.mHostName == null) { 
                    // サーバーになる
                    // 接続があるのを待つ
                
                    TcpListener listener = new TcpListener(IPAddress.Any, this.mPort);
                    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 10000);

                    listener.Start();

                    while (!this.mStopFlag)
                    {
                        if (listener.Pending())
                        {
                            // 接続完了
                            TcpClient client = listener.AcceptTcpClient();
                            
                            if (client.GetStream() == null)
                                continue;

                            this.mNetwork = new GodaiLibrary.Network(client.GetStream());
                            break;
                        }
                        else
                        {
                            Thread.Sleep(1000);  // 接続待ち
                        }
                    }
                    listener.Stop();

                    if (this.mStopFlag)
                    {
                        // 中止フラグが立っていたら終了する
                        return;
                    }
                }
                else
                {
                    // クライアントになる
                    // 接続を試みる
                    TcpClient client = new TcpClient();
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 10000);
                    client.Connect(this.mHostName, this.mPort);

                    if ( client.GetStream() == null )
                        continue;

                    this.mNetwork = new GodaiLibrary.Network(client.GetStream());
                }
            }

            // 監視処理開始
            if (this.mWathcer != null)
                this.mWathcer.Dispose();

            this.mWathcer = new FileSystemWatcher();

            if (!Directory.Exists(this.mSyncFolder))
                Directory.CreateDirectory(this.mSyncFolder);

            this.mWathcer.Path = this.mSyncFolder;

            this.mWathcer.NotifyFilter = (NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.FileName);
            this.mWathcer.Filter = "";
            this.mWathcer.IncludeSubdirectories = true;

            this.mWathcer.SynchronizingObject = this.mParent;

            this.mWathcer.Changed += new FileSystemEventHandler(watcher_Changed);
            this.mWathcer.Created += new FileSystemEventHandler(watcher_Changed);
            this.mWathcer.Deleted += new FileSystemEventHandler(watcher_Changed);
            this.mWathcer.Renamed += new RenamedEventHandler(watcher_Renamed);

            this.mWathcer.EnableRaisingEvents = true;

            this.mParent.addLog(this.mSyncFolder + "の監視を始めました");

            // 送信ループを立ち上げる
            Thread threadSend = new Thread(new ThreadStart(this.runSend));
            threadSend.IsBackground = true;
            threadSend.Start();

            // 受信ループ
            while (!this.mStopFlag)
            {
                int nCom = this.mNetwork.receiveByte();
                if (nCom == COM_NO_OP)
                {
                    continue;
                }
                else if (nCom == COM_SENDFILE)
                {
                    this.mCntNoOp = 0;

                    // ファイルを受信する
                    GodaiLibrary.Network.receiveFiles(this.mNetwork, this.mSyncFolder, this);

                    // 再びONにする
//                    this.mWathcer.EnableRaisingEvents = true;
//                    this.mIgnorePath = null;
                }
                else if (nCom == COM_DELETEFILE)
                {
                    // ファイルを削除する
                    String strPath = this.mNetwork.receiveString();
                    if (File.Exists(strPath))
                    {
                        try
                        {
                            File.Delete(strPath);
                            this.mParent.addLog("「" + strPath + "」を削除しました。");
                        }
                        catch (Exception ex) { }
                    }
                }
            }
        }

        public void addLog(String strLog)
        {
            this.mParent.addLog(strLog);
        }

        private String mIgnorePath;

        // 受信前に呼びだされる
        public void beginReceiveFile(String strFilePath)
        {
            this.mParent.addLog("「" + strFilePath + "」を受信しました。");

//            this.mWathcer.EnableRaisingEvents = false;  // 一時的にOFFにする
            // 一人で2台のコンピュータを操作するとき、まさか、同時に操作することは無いだろうとの想定で。

            //this.mIgnorePath = strFilePath;
            ReceiveFileInfo fileinfo = new ReceiveFileInfo(strFilePath);
            lock (this.mReceiveFileInfoList)
            {
                this.mReceiveFileInfoList.addFileInfo(fileinfo);
            }
            if (this.isReceiveFile(strFilePath, false))
                return;
                
            lock (this.mReceiveList)
            {
                this.mReceiveList.Add(strFilePath);
                List<String> listTmp;
                lock (this.mReceiveList)
                {
                    listTmp = this.copyStringList(this.mReceiveList);
                }
                this.mParent.setLog(listTmp, 1);
            }
        }

        // 受信後に呼びだされる
        public void endReceiveFile(String strFilePath)
        {
            lock (this.mReceiveFileInfoList)
            {
                this.mReceiveFileInfoList.setCompleteReceiveFile(strFilePath);
            }
        }

        // コマンド
        private static byte COM_NO_OP = 99;         // 何もしない
        private static byte COM_SENDFILE = 100;     // ファイルを送信
        private static byte COM_DELETEFILE = 101;

        private List<String> mReceiveList = new List<String>();
        private LinkedList<String> mSendList = new LinkedList<String>();
        private List<String> mDeleteList = new List<String>();
        private ManualResetEvent mEventSend = new ManualResetEvent(false);


        private int mCntNoOp = 0;

        /// 送信ループ
        private void runSend()
        {
            try {
                while (!this.mStopFlag)
                {
                    mEventSend.WaitOne(100);

                    this.mNetwork.sendByte(COM_NO_OP);
                    this.mNetwork.flush();

                    /// 削除リストの送信
                    lock (this.mDeleteList)
                    {
                        foreach (var filepath in this.mDeleteList)
                        {
                            this.mNetwork.sendByte(COM_DELETEFILE);
                            this.mNetwork.sendString(filepath);
                        }
                        this.mDeleteList.Clear();
                    }

                    // 
                    bool bNoOp = false;
                    lock (this.mSendList)
                    {
                        if (this.mSendList.Count == 0)
                        {
                            if (++this.mCntNoOp > 6)
                            {
                                bNoOp = true;
                            }
                        }
                    }

                    if (bNoOp)
                    {
                        this.mCntNoOp = 0;

                        // 受信ファイルリストをクリアする
                        lock (this.mReceiveList)
                        {
                            this.mReceiveList.Clear();
                        }
                        List<String> listTmp;
                        lock (this.mReceiveList)
                        {
                            listTmp = this.copyStringList(this.mReceiveList);
                        }
                        this.mParent.setLog(listTmp, 1);

                        lock (this.mSendList)
                        {
                            listTmp = this.copyStringList(this.mSendList);
                        }
                        this.mParent.setLog(listTmp, 0);

                        continue;
                    }

                    this.mCntNoOp = 0;

                    this.mNetwork.sendByte(COM_SENDFILE); // ファイル送信開始

                    while (true)
                    {
                        String strPath;
                        lock (this.mSendList)
                        {
                            if (this.mSendList.Count == 0)
                                break;

                            strPath = this.mSendList.Last.Value;
                            mSendList.RemoveLast();

                            //                        if ( this.isReceiveFile(strPath, false))
                            //                            continue;
                        }

                        if (File.Exists(strPath))
                        {
                            try
                            {
                                long nBefSize = 0;
                                for (int it = 0; it < 10; ++it)
                                {
                                    FileInfo info = new FileInfo(strPath);

                                    if (nBefSize != 0 && nBefSize != info.Length)
                                        throw new Exception();
                                    nBefSize = info.Length;
                                    using (System.IO.FileStream win = new System.IO.FileStream(strPath, FileMode.Open, FileAccess.ReadWrite)) { }

                                    Thread.Sleep(100);
                                }
                            }
                            catch (Exception ex)
                            {
                                lock (this.mSendList)
                                {
                                    Thread.Sleep(1000);

                                    // 再追加
                                    if (File.Exists(strPath))
                                    {
                                        this.mSendList.AddFirst(strPath);

                                        // 再送信しないように設定
                                        lock (this.mReceiveList)
                                        {
                                            this.mReceiveList.Add(strPath);
                                        }
                                    }
                                    continue;
                                }
                            }

                            /// ファイルサイズをチェックする
                            FileInfo info2 = new FileInfo(strPath);
                            if (info2.Length < this.mMaxLimit)
                            {

                                // ファイルを送信する
                                this.addLog("「" + strPath + "」を送信開始");
                                GodaiLibrary.Network.sendFile(this.mNetwork, Path.GetFileName(strPath), strPath, Path.GetDirectoryName(strPath.Substring(this.mSyncFolder.Length)));
                                this.addLog("「" + strPath + "」を送信終了");

                                // 自分で再送信しないように
                                lock (this.mReceiveList)
                                {
                                    this.mReceiveList.Add(strPath);
                                }
                            }
                        }
                        List<String> listTmp;
                        lock (this.mSendList)
                        {
                            listTmp = this.copyStringList(this.mSendList);
                        }
                        this.mParent.setLog(listTmp, 0);
                    }
                    this.mNetwork.sendByte(0);  // 送信終了
                    this.mNetwork.flush();
                }
            }
            catch (Exception ex)
            {
//                MessageBox.Show(ex.Message);
            }
        }

        private List<String> copyStringList(List<String> src)
        {
            List<String> ret = new List<string>();
            foreach (var str in src)
            {
                ret.Add(str);
            }
            return ret;
        }
        private List<String> copyStringList(LinkedList<String> src)
        {
            List<String> ret = new List<string>();
            foreach (var str in src)
            {
                ret.Add(str);
            }
            return ret;
        }

        private bool isReceiveFile(String strPath, bool bDelete)
        {

            lock (this.mReceiveList)
            {
                for (int il = 0; il < this.mReceiveList.Count; ++il)
                {
                    if (this.mReceiveList[il] == strPath)
                    {
                        // 受信ファイルは送信しない
                        if ( bDelete ) this.mReceiveList.RemoveAt(il);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool isSendFile(String strPath)
        {
            lock (this.mSendList)
            {
                foreach (var filepath in this.mSendList) 
                {
                    if (filepath == strPath)
                        return true;
                }
            }
            return false;
        }

        private void changeFile(String strPath) {

            // 受信したファイルは記録しない
//            if (this.isReceiveFile(strPath, false))
//              return;

            //            if ( this.mIgnorePath != null && Path.GetFullPath(strPath) == Path.GetFullPath(this.mIgnorePath) )
            //                return;
            lock (this.mReceiveFileInfoList)
            {
                if (this.mReceiveFileInfoList.isReceivedFile(Path.GetFullPath(strPath)))
                    return;
            }

            if (!File.Exists(strPath))
                return;

            // 拡張子のチェックをする
            // 一致したものが無ければ登録しない
            bool bMatch = false;
            foreach (var regex in this.mExtList)
            {
                if (regex.IsMatch(strPath, 0))
                {
                    bMatch = true;
                    break;
                }
            }

            if (!bMatch)
                return;

            if (this.isSendFile(strPath))
                return;

            // 送信リストに登録する
            lock (this.mSendList) { 

                this.mSendList.AddLast(strPath); 
                this.mEventSend.Set();
            }
            List<String> listTmp;
            lock (this.mSendList)
            {
                listTmp = this.copyStringList(this.mSendList);
            }
            this.mParent.setLog(listTmp, 0);
        }

        private void deleteFile(String strPath)
        {
            if (!File.Exists(strPath))
                return;

            bool bMatch = false;
            foreach (var regex in this.mExtList)
            {
                if (regex.IsMatch(strPath, 0))
                {
                    bMatch = true;
                    break;
                }
            }

            if (!bMatch)
                return;

            lock (this.mDeleteList)
            {
                this.mDeleteList.Add(strPath);
                this.mEventSend.Set();
            }
        }

        private void watcher_Changed(System.Object source, System.IO.FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case System.IO.WatcherChangeTypes.Changed:
//                     this.mParent.addLog(
//                         "「" + e.FullPath + "」"+"が変更されました。");
                     this.changeFile(e.FullPath);
                    break;
                case System.IO.WatcherChangeTypes.Created:
//                     this.mParent.addLog(
//                         "「" + e.FullPath + "」が作成されました。");
                     this.changeFile(e.FullPath);
                    break;
                case System.IO.WatcherChangeTypes.Deleted:
//                     this.mParent.addLog(
//                         "「"+ e.FullPath + "」が削除されました。");
                     this.deleteFile(e.FullPath);
                    break;
            }
        }

        private void watcher_Renamed(System.Object source, System.IO.RenamedEventArgs e)
        {
            //             this.mParent.addLog(
            //                 "「" + e.FullPath + "」に名前が変更されました。");
            if (e.OldFullPath != e.FullPath)
            {
                this.deleteFile(e.OldFullPath);
                this.changeFile(e.FullPath);
            }
        }
    }


}
