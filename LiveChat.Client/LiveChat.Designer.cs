namespace LiveChat.Client
{
    partial class LiveChat
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LiveChat));
            this.labelSelectFile = new System.Windows.Forms.Label();
            this.buttonSendFile = new System.Windows.Forms.Button();
            this.openFileDialogLiveChat = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // labelSelectFile
            // 
            this.labelSelectFile.AutoSize = true;
            this.labelSelectFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSelectFile.Location = new System.Drawing.Point(12, 9);
            this.labelSelectFile.Name = "labelSelectFile";
            this.labelSelectFile.Size = new System.Drawing.Size(137, 25);
            this.labelSelectFile.TabIndex = 0;
            this.labelSelectFile.Text = "Select a file";
            // 
            // buttonSendFile
            // 
            this.buttonSendFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSendFile.Location = new System.Drawing.Point(12, 49);
            this.buttonSendFile.Name = "buttonSendFile";
            this.buttonSendFile.Size = new System.Drawing.Size(445, 75);
            this.buttonSendFile.TabIndex = 1;
            this.buttonSendFile.Text = "SELECT AND SEND FILE";
            this.buttonSendFile.UseVisualStyleBackColor = true;
            this.buttonSendFile.Click += new System.EventHandler(this.buttonSendFile_Click);
            // 
            // openFileDialogLiveChat
            // 
            this.openFileDialogLiveChat.FileName = "openFileDialog1";
            // 
            // LiveChat
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(469, 152);
            this.Controls.Add(this.buttonSendFile);
            this.Controls.Add(this.labelSelectFile);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "LiveChat";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LiveChat Client";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelSelectFile;
        private System.Windows.Forms.Button buttonSendFile;
        private System.Windows.Forms.OpenFileDialog openFileDialogLiveChat;
    }
}