namespace Ecommerce.DesktopImpl
{
    partial class UserPage
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            label1 = new System.Windows.Forms.Label();
            cancelBtn = new System.Windows.Forms.Button();
            updateAdres = new System.Windows.Forms.Button();
            confirmBtn = new System.Windows.Forms.Button();
            label2 = new System.Windows.Forms.Label();
            infoBox = new System.Windows.Forms.Panel();
            orderItemsView = new System.Windows.Forms.ListView();
            editBtn = new System.Windows.Forms.Button();
            _changePasswordBtn = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.Font = new System.Drawing.Font("Segoe UI", 12F);
            label1.Location = new System.Drawing.Point(0, 183);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(107, 29);
            label1.TabIndex = 1;
            label1.Text = "Siparişler";
            // 
            // cancelBtn
            // 
            cancelBtn.Location = new System.Drawing.Point(327, 608);
            cancelBtn.Name = "cancelBtn";
            cancelBtn.Size = new System.Drawing.Size(97, 39);
            cancelBtn.TabIndex = 2;
            cancelBtn.Text = "İptal Et";
            cancelBtn.UseVisualStyleBackColor = true;
            cancelBtn.Click += cancelBtn_Click;
            // 
            // updateAdres
            // 
            updateAdres.Location = new System.Drawing.Point(430, 608);
            updateAdres.Name = "updateAdres";
            updateAdres.Size = new System.Drawing.Size(121, 39);
            updateAdres.TabIndex = 3;
            updateAdres.Text = "Adres Güncelle";
            updateAdres.UseVisualStyleBackColor = true;
            updateAdres.Click += updateAdres_Click;
            // 
            // confirmBtn
            // 
            confirmBtn.Location = new System.Drawing.Point(557, 608);
            confirmBtn.Name = "confirmBtn";
            confirmBtn.Size = new System.Drawing.Size(79, 39);
            confirmBtn.TabIndex = 4;
            confirmBtn.Text = "Onayla";
            confirmBtn.UseVisualStyleBackColor = true;
            confirmBtn.Click += confirmBtn_Click;
            // 
            // label2
            // 
            label2.Dock = System.Windows.Forms.DockStyle.Top;
            label2.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            label2.Location = new System.Drawing.Point(0, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(1020, 34);
            label2.TabIndex = 5;
            label2.Text = "Kullanıcı Bilgileri";
            label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // infoBox
            // 
            infoBox.Dock = System.Windows.Forms.DockStyle.Top;
            infoBox.Location = new System.Drawing.Point(0, 34);
            infoBox.Name = "infoBox";
            infoBox.Size = new System.Drawing.Size(1020, 146);
            infoBox.TabIndex = 6;
            // 
            // orderItemsView
            // 
            orderItemsView.FullRowSelect = true;
            orderItemsView.GridLines = true;
            orderItemsView.Location = new System.Drawing.Point(-3, 215);
            orderItemsView.Name = "orderItemsView";
            orderItemsView.Size = new System.Drawing.Size(1020, 387);
            orderItemsView.TabIndex = 7;
            orderItemsView.UseCompatibleStateImageBehavior = false;
            orderItemsView.View = System.Windows.Forms.View.Details;
            // 
            // editBtn
            // 
            editBtn.Location = new System.Drawing.Point(879, 183);
            editBtn.Name = "editBtn";
            editBtn.Size = new System.Drawing.Size(105, 29);
            editBtn.TabIndex = 8;
            editBtn.Text = "Düzenle";
            editBtn.UseVisualStyleBackColor = true;
            editBtn.Click += editBtn_Click;
            // 
            // _changePasswordBtn
            // 
            _changePasswordBtn.AutoSize = true;
            _changePasswordBtn.Location = new System.Drawing.Point(768, 183);
            _changePasswordBtn.Name = "_changePasswordBtn";
            _changePasswordBtn.Size = new System.Drawing.Size(105, 30);
            _changePasswordBtn.TabIndex = 9;
            _changePasswordBtn.Text = "Şifre Değiştir";
            _changePasswordBtn.UseVisualStyleBackColor = true;
            // 
            // UserPage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(_changePasswordBtn);
            Controls.Add(editBtn);
            Controls.Add(orderItemsView);
            Controls.Add(infoBox);
            Controls.Add(label2);
            Controls.Add(confirmBtn);
            Controls.Add(updateAdres);
            Controls.Add(cancelBtn);
            Controls.Add(label1);
            Size = new System.Drawing.Size(1020, 650);
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Button _changePasswordBtn;

        private System.Windows.Forms.Button editBtn;

        private System.Windows.Forms.ListView orderItemsView;

        private System.Windows.Forms.Panel infoBox;

        private System.Windows.Forms.Label label2;

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button cancelBtn;
        private System.Windows.Forms.Button updateAdres;
        private System.Windows.Forms.Button confirmBtn;

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
    }
}
