namespace Ecommerce.DesktopImpl
{
    partial class RegisteryPage
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
            button1 = new System.Windows.Forms.Button();
            formTabs = new System.Windows.Forms.TabControl();
            userForm = new System.Windows.Forms.TabPage();
            sellerForm = new System.Windows.Forms.TabPage();
            formTabs.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(408, 606);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(203, 44);
            button1.TabIndex = 0;
            button1.Text = "Kaydol";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // formTabs
            // 
            formTabs.Controls.Add(userForm);
            formTabs.Controls.Add(sellerForm);
            formTabs.Location = new System.Drawing.Point(206, 3);
            formTabs.Name = "formTabs";
            formTabs.SelectedIndex = 0;
            formTabs.Size = new System.Drawing.Size(630, 597);
            formTabs.TabIndex = 1;
            // 
            // userForm
            // 
            userForm.AutoScroll = true;
            userForm.Location = new System.Drawing.Point(4, 29);
            userForm.Name = "userForm";
            userForm.Padding = new System.Windows.Forms.Padding(3);
            userForm.Size = new System.Drawing.Size(622, 564);
            userForm.TabIndex = 0;
            userForm.Text = "Kullanıcı Kaydı";
            userForm.UseVisualStyleBackColor = true;
            // 
            // sellerForm
            // 
            sellerForm.Location = new System.Drawing.Point(4, 29);
            sellerForm.Name = "sellerForm";
            sellerForm.Padding = new System.Windows.Forms.Padding(3);
            sellerForm.Size = new System.Drawing.Size(622, 564);
            sellerForm.TabIndex = 1;
            sellerForm.Text = "Satıcı Kaydı";
            sellerForm.UseVisualStyleBackColor = true;
            // 
            // RegisteryPage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(formTabs);
            Controls.Add(button1);
            Size = new System.Drawing.Size(1020, 650);
            formTabs.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.TabControl formTabs;
        private System.Windows.Forms.TabPage userForm;
        private System.Windows.Forms.TabPage sellerForm;

        private System.Windows.Forms.Button button1;

        #endregion
    }
}
