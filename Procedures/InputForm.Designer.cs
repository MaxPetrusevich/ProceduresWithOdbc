using System.ComponentModel;

namespace Procedures
{
    partial class InputForm
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
            this.lblBookID = new System.Windows.Forms.Label();
            this.txtBookID = new System.Windows.Forms.TextBox();
            this.lblBookName = new System.Windows.Forms.Label();
            this.txtBookName = new System.Windows.Forms.TextBox();
            this.lblAuthor = new System.Windows.Forms.Label();
            this.txtAuthor = new System.Windows.Forms.TextBox();
            this.lblYear = new System.Windows.Forms.Label();
            this.txtYear = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblBookID
            // 
            this.lblBookID.AutoSize = true;
            this.lblBookID.Location = new System.Drawing.Point(32, 27);
            this.lblBookID.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblBookID.Name = "lblBookID";
            this.lblBookID.Size = new System.Drawing.Size(58, 16);
            this.lblBookID.TabIndex = 0;
            this.lblBookID.Text = "Book ID:";
            // 
            // txtBookID
            // 
            this.txtBookID.Location = new System.Drawing.Point(156, 23);
            this.txtBookID.Margin = new System.Windows.Forms.Padding(4);
            this.txtBookID.Name = "txtBookID";
            this.txtBookID.Size = new System.Drawing.Size(132, 22);
            this.txtBookID.TabIndex = 1;
            // 
            // lblBookName
            // 
            this.lblBookName.AutoSize = true;
            this.lblBookName.Location = new System.Drawing.Point(32, 64);
            this.lblBookName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblBookName.Name = "lblBookName";
            this.lblBookName.Size = new System.Drawing.Size(82, 16);
            this.lblBookName.TabIndex = 2;
            this.lblBookName.Text = "Book Name:";
            // 
            // txtBookName
            // 
            this.txtBookName.Location = new System.Drawing.Point(156, 60);
            this.txtBookName.Margin = new System.Windows.Forms.Padding(4);
            this.txtBookName.Name = "txtBookName";
            this.txtBookName.Size = new System.Drawing.Size(132, 22);
            this.txtBookName.TabIndex = 3;
            // 
            // lblAuthor
            // 
            this.lblAuthor.AutoSize = true;
            this.lblAuthor.Location = new System.Drawing.Point(32, 101);
            this.lblAuthor.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAuthor.Name = "lblAuthor";
            this.lblAuthor.Size = new System.Drawing.Size(48, 16);
            this.lblAuthor.TabIndex = 4;
            this.lblAuthor.Text = "Author:";
            // 
            // txtAuthor
            // 
            this.txtAuthor.Location = new System.Drawing.Point(156, 97);
            this.txtAuthor.Margin = new System.Windows.Forms.Padding(4);
            this.txtAuthor.Name = "txtAuthor";
            this.txtAuthor.Size = new System.Drawing.Size(132, 22);
            this.txtAuthor.TabIndex = 5;
            // 
            // lblYear
            // 
            this.lblYear.AutoSize = true;
            this.lblYear.Location = new System.Drawing.Point(32, 138);
            this.lblYear.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblYear.Name = "lblYear";
            this.lblYear.Size = new System.Drawing.Size(39, 16);
            this.lblYear.TabIndex = 6;
            this.lblYear.Text = "Year:";
            // 
            // txtYear
            // 
            this.txtYear.Location = new System.Drawing.Point(156, 134);
            this.txtYear.Margin = new System.Windows.Forms.Padding(4);
            this.txtYear.Name = "txtYear";
            this.txtYear.Size = new System.Drawing.Size(132, 22);
            this.txtYear.TabIndex = 7;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(36, 186);
            this.btnOK.Margin = new System.Windows.Forms.Padding(4);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 28);
            this.btnOK.TabIndex = 8;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(189, 186);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 28);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // InputForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(336, 239);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.txtYear);
            this.Controls.Add(this.lblYear);
            this.Controls.Add(this.txtAuthor);
            this.Controls.Add(this.lblAuthor);
            this.Controls.Add(this.txtBookName);
            this.Controls.Add(this.lblBookName);
            this.Controls.Add(this.txtBookID);
            this.Controls.Add(this.lblBookID);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "InputForm";
            this.Text = "Custom Input Form";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

    }
}