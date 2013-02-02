using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace FolderLANSync
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            // 設定読み込み
            if (!loadConfiguration())
                return;

            // 
        }

        private List<SyncFolderMaster> mSyncMasterList = new List<SyncFolderMaster>();

        // 設定ファイルを読み込む
        private bool loadConfiguration()
        {
            String filename = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "config.xml");
            if (!File.Exists(filename))
            {
                MessageBox.Show(filename + "が見つかりません");
                Application.Exit();
                return false;
            }

            EXMLPlace place = EXMLPlace.None;
            int nPort = 23133;
            SyncFolderMaster syncMaster = new SyncFolderMaster(this);

            XmlTextReader reader = new XmlTextReader(filename);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "config")
                        place = EXMLPlace.Config;
                    else if (reader.Name == "sync")
                    {
                        place = EXMLPlace.Sync;
                        syncMaster.setPort(nPort);
                    }
                    else
                    {
                        if (place == EXMLPlace.Sync)
                        {
                            if (reader.Name == "name")
                            {

                            }
                            else if (reader.Name == "port")
                            {
                                reader.Read();
                                int.TryParse(reader.Value, out nPort);
                                syncMaster.setPort(nPort);
                            }
                            else if (reader.Name == "host")
                            {
                                reader.Read();
                                String strHostName = reader.Value;
                                syncMaster.setHostName(strHostName);
                            }
                            else if (reader.Name == "sync_file_ext")
                            {
                                reader.Read();
                                String strExt = reader.Value;
                                syncMaster.addSyncExt(strExt);
                            }
                            else if (reader.Name == "sync_folder")
                            {
                                reader.Read();
                                String strSyncFolder = reader.Value;
                                syncMaster.setSyncFolder(strSyncFolder);    // エラーを出すかも
                            }
                            else if (reader.Name == "max_file_size")
                            {
                                reader.Read();
                                int nMaxFileSize = 10 * 1024 * 1024;
                                int.TryParse(reader.Value, out nMaxFileSize);
                                syncMaster.setMaxLimit(nMaxFileSize);
                            }
                        }
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement ) {

                    if ( reader.Name == "config" ) {
                        place = EXMLPlace.None;
                    }
                    else if ( reader.Name == "sync" ) {
                    
                        place = EXMLPlace.None;
                        this.mSyncMasterList.Add(syncMaster);
                        syncMaster = new SyncFolderMaster(this);
                    }
                }
            }

            // エラーチェック
            foreach (var syncmaster in this.mSyncMasterList)
            {
                if (syncmaster.isError())
                {
                    Application.Exit();
                    return false;
                }
            }

            // 監視を起動する
            foreach (var syncmaster in this.mSyncMasterList)
            {
                Thread thread = new Thread(new ThreadStart(syncmaster.run));
                thread.IsBackground = true;
                thread.Start();
            }

            return true;
        }

        private List<String> listLog3 = new List<String>();

        public void addLog(String strLog)
        {
            this.listLog3.Add(strLog);

            if (this.listLog3.Count > 100)
                this.listLog3.RemoveAt(0);

            StringBuilder str = new StringBuilder();
            for (int it = this.listLog3.Count - 1; it >= 0; --it )
            {
                str.AppendLine(this.listLog3[it]);
            }

            this.txtLog3.SetPropertyThreadSafe(() => txtLog3.Text, str.ToString());
        }

        public void setLog(List<String> listStr, int nTarget)
        {
            StringBuilder str = new StringBuilder();
            for (int it = listStr.Count - 1; it >= 0; --it)
            {
                str.AppendLine(listStr[it]);
            }

            if (nTarget == 0)
            {
                this.txtLog1.SetPropertyThreadSafe(() => txtLog1.Text, str.ToString());
            }
            else
            {
                this.txtLog2.SetPropertyThreadSafe(() => txtLog2.Text, str.ToString());
            }
        }


        private enum EXMLPlace
        {
            None,
            Config,
            Sync
        }
    }
    // 拡張メソッド用のクラス
    static class Extention
    {
        // Webより
        private delegate void SetPropertyThreadSafeDelegate<TResult>(Control @this, Expression<Func<TResult>> property, TResult value);

        // 拡張メソッド
        public static void SetPropertyThreadSafe<TResult>(this Control @this, Expression<Func<TResult>> property, TResult value)
        {
            var propertyInfo = (property.Body as MemberExpression).Member as PropertyInfo;

            if (propertyInfo == null ||
                !@this.GetType().IsSubclassOf(propertyInfo.ReflectedType) ||
                @this.GetType().GetProperty(propertyInfo.Name, propertyInfo.PropertyType) == null)
            {
                throw new ArgumentException("The lambda expression 'property' must reference a valid property on this Control.");
            }

            if (@this.InvokeRequired)
            {
                @this.Invoke(new SetPropertyThreadSafeDelegate<TResult>(SetPropertyThreadSafe), new object[] { @this, property, value });
            }
            else
            {
                @this.GetType().InvokeMember(propertyInfo.Name, BindingFlags.SetProperty, null, @this, new object[] { value });
            }
        }

    }
    // XML読み込みのときの位置記録用

}
