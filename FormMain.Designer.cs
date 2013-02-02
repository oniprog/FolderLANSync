namespace FolderLANSync
{
    partial class FormMain
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.txtLog3 = new System.Windows.Forms.TextBox();
            this.txtLog1 = new System.Windows.Forms.TextBox();
            this.txtLog2 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtLog3
            // 
            this.txtLog3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog3.Location = new System.Drawing.Point(11, 134);
            this.txtLog3.Multiline = true;
            this.txtLog3.Name = "txtLog3";
            this.txtLog3.ReadOnly = true;
            this.txtLog3.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog3.Size = new System.Drawing.Size(830, 146);
            this.txtLog3.TabIndex = 2;
            // 
            // txtLog1
            // 
            this.txtLog1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog1.Location = new System.Drawing.Point(11, 12);
            this.txtLog1.Multiline = true;
            this.txtLog1.Name = "txtLog1";
            this.txtLog1.ReadOnly = true;
            this.txtLog1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog1.Size = new System.Drawing.Size(830, 116);
            this.txtLog1.TabIndex = 0;
            // 
            // txtLog2
            // 
            this.txtLog2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog2.Location = new System.Drawing.Point(205, 12);
            this.txtLog2.Multiline = true;
            this.txtLog2.Name = "txtLog2";
            this.txtLog2.ReadOnly = true;
            this.txtLog2.Size = new System.Drawing.Size(637, 116);
            this.txtLog2.TabIndex = 1;
            this.txtLog2.Visible = false;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(854, 292);
            this.Controls.Add(this.txtLog3);
            this.Controls.Add(this.txtLog2);
            this.Controls.Add(this.txtLog1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "FormMain";
            this.Text = "フォルダ同期プログラム";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtLog3;
        private System.Windows.Forms.TextBox txtLog1;
        private System.Windows.Forms.TextBox txtLog2;

    }
}

