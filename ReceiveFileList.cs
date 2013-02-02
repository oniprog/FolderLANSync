using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FolderLANSync
{
    ///  受信ファイルの情報
    public struct ReceiveFileInfo 
    {
        public DateTime mDateTime;
        public String mFullPath;

        public ReceiveFileInfo(String strFullPath)
        {
            this.mFullPath = strFullPath;
            this.mDateTime = DateTime.Now.AddHours(1);  // わざと未来にしておく。受信完了後に、現在日付に
        }
    }

    ///  受信ファイルの情報
    public class ReceiveFileList
    {

        private LinkedList<ReceiveFileInfo> mFileList = new LinkedList<ReceiveFileInfo>();
        private int mExpiredSecond = 5;

        public void addFileInfo(ReceiveFileInfo info_)
        {
            this.mFileList.AddLast(info_);
        }

        // ファイルを受信完了した
        public void setCompleteReceiveFile(String strFullPath)
        {
            var cur = this.mFileList.First;
            for (; cur != null; cur = cur.Next)
            {
                if (cur.Value.mFullPath == strFullPath)
                {
                    this.mFileList.Remove(cur);

                    var val = cur.Value;
                    val.mDateTime = DateTime.Now;
                    this.mFileList.AddFirst(val);
                }
            }
        }

        // 受信したファイルかの判定
        public bool isReceivedFile(String strFullPath)
        {
            // 古いファイルを消す
            deleteExpiredFile();

            foreach (var fileinfo in this.mFileList) {

                if (fileinfo.mFullPath == strFullPath)
                    return true;
            }

            return false;
        }

        private void deleteExpiredFile()
        {
            DateTime time = DateTime.Now;
            LinkedList<ReceiveFileInfo> listNew = new LinkedList<ReceiveFileInfo>();
            foreach (var fileinfo in this.mFileList)
            {
                var spendtime = time - fileinfo.mDateTime;
                if (spendtime.Seconds < this.mExpiredSecond)
                {
                    // 新しいファイルを残す
                    listNew.AddLast(fileinfo);
                }
            }
            this.mFileList = listNew;
        }
    }
}
