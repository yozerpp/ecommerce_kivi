namespace Ecommerce.DesktopImpl
{
    partial class LoginPage
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
            emailBox = new System.Windows.Forms.TextBox();
            rememberMeBtn = new System.Windows.Forms.CheckBox();
            passwordBox = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            sellerLoginBtn = new System.Windows.Forms.Button();
            userLgnBtn = new System.Windows.Forms.Button();
            registerUserBtn = new System.Windows.Forms.Button();
            registerSellerBtn = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // emailBox
            // 
            emailBox.Font = new System.Drawing.Font("Segoe UI", 12F);
            emailBox.Location = new System.Drawing.Point(341, 204);
            emailBox.Name = "emailBox";
            emailBox.Size = new System.Drawing.Size(334, 34);
            emailBox.TabIndex = 0;
            // 
            // rememberMeBtn
            // 
            rememberMeBtn.AutoSize = true;
            rememberMeBtn.Location = new System.Drawing.Point(341, 314);
            rememberMeBtn.Name = "rememberMeBtn";
            rememberMeBtn.Size = new System.Drawing.Size(109, 24);
            rememberMeBtn.TabIndex = 1;
            rememberMeBtn.Text = "Beni Hatırla";
            rememberMeBtn.UseVisualStyleBackColor = true;

            // 
            // passwordBox
            // 
            passwordBox.Font = new System.Drawing.Font("Segoe UI", 12F);
            passwordBox.Location = new System.Drawing.Point(341, 264);
            passwordBox.Name = "passwordBox";
            passwordBox.Size = new System.Drawing.Size(334, 34);
            passwordBox.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(341, 241);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(39, 20);
            label1.TabIndex = 3;
            label1.Text = "Şifre";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(341, 181);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(46, 20);
            label2.TabIndex = 4;
            label2.Text = "Email";
            // 
            // sellerLoginBtn
            // 
            sellerLoginBtn.Location = new System.Drawing.Point(350, 344);
            sellerLoginBtn.Name = "sellerLoginBtn";
            sellerLoginBtn.Size = new System.Drawing.Size(150, 49);
            sellerLoginBtn.TabIndex = 5;
            sellerLoginBtn.Text = "Satıc Girişi";
            sellerLoginBtn.UseVisualStyleBackColor = true;
            sellerLoginBtn.Click += sellerLoginBtn_Click;
            // 
            // userLgnBtn
            // 
            userLgnBtn.Font = new System.Drawing.Font("Segoe UI", 9F);
            userLgnBtn.Location = new System.Drawing.Point(506, 344);
            userLgnBtn.Name = "userLgnBtn";
            userLgnBtn.Size = new System.Drawing.Size(148, 49);
            userLgnBtn.TabIndex = 6;
            userLgnBtn.Text = "Kullanıcı Girişi";
            userLgnBtn.UseVisualStyleBackColor = true;
            userLgnBtn.Click += userLgnBtn_Click;
            // 
            // registerUserBtn
            // 
            registerUserBtn.Location = new System.Drawing.Point(506, 399);
            registerUserBtn.Name = "registerUserBtn";
            registerUserBtn.Size = new System.Drawing.Size(148, 44);
            registerUserBtn.TabIndex = 7;
            registerUserBtn.Text = "Kullanıcı Kaydı";
            registerUserBtn.UseVisualStyleBackColor = true;
            registerUserBtn.Click += registerBtn_Click;
            // 
            // registerSellerBtn
            // 
            registerSellerBtn.Location = new System.Drawing.Point(350, 399);
            registerSellerBtn.Name = "registerSellerBtn";
            registerSellerBtn.Size = new System.Drawing.Size(150, 44);
            registerSellerBtn.TabIndex = 8;
            registerSellerBtn.Text = "Satıcı Kaydı";
            registerSellerBtn.UseVisualStyleBackColor = true;
            // 
            // LoginPage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(registerSellerBtn);
            Controls.Add(registerUserBtn);
            Controls.Add(userLgnBtn);
            Controls.Add(sellerLoginBtn);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(passwordBox);
            Controls.Add(rememberMeBtn);
            Controls.Add(emailBox);
            Size = new System.Drawing.Size(1020, 650);
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Button registerSellerBtn;

        private System.Windows.Forms.Button registerUserBtn;

        #endregion

        private System.Windows.Forms.TextBox emailBox;
        private System.Windows.Forms.CheckBox rememberMeBtn;
        private System.Windows.Forms.TextBox passwordBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button sellerLoginBtn;
        private System.Windows.Forms.Button userLgnBtn;
    }
}
