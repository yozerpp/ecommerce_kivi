namespace Ecommerce.DesktopImpl;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
        if (disposing && (components != null)){
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
        navbar = new System.Windows.Forms.Panel();
        registerBtn = new System.Windows.Forms.Button();
        logoutBtn = new System.Windows.Forms.Button();
        loginBtn = new System.Windows.Forms.Button();
        cartBtn = new System.Windows.Forms.Button();
        homepageBtn = new System.Windows.Forms.Button();
        forwardBtn = new System.Windows.Forms.Button();
        refreshBtn = new System.Windows.Forms.Button();
        backBtn = new System.Windows.Forms.Button();
        pageContainer = new System.Windows.Forms.Panel();
        navbar.SuspendLayout();
        SuspendLayout();
        // 
        // navbar
        // 
        navbar.Controls.Add(registerBtn);
        navbar.Controls.Add(logoutBtn);
        navbar.Controls.Add(loginBtn);
        navbar.Controls.Add(cartBtn);
        navbar.Controls.Add(homepageBtn);
        navbar.Controls.Add(forwardBtn);
        navbar.Controls.Add(refreshBtn);
        navbar.Controls.Add(backBtn);
        navbar.Dock = System.Windows.Forms.DockStyle.Top;
        navbar.Location = new System.Drawing.Point(0, 0);
        navbar.Name = "navbar";
        navbar.Size = new System.Drawing.Size(1003, 96);
        navbar.TabIndex = 0;
        // 
        // registerBtn
        // 
        registerBtn.Location = new System.Drawing.Point(743, 3);
        registerBtn.Name = "registerBtn";
        registerBtn.Size = new System.Drawing.Size(124, 45);
        registerBtn.TabIndex = 8;
        registerBtn.Text = "Hesap Aç";
        registerBtn.UseVisualStyleBackColor = true;
        registerBtn.Click += registerBtn_Click;
        // 
        // logoutBtn
        // 
        logoutBtn.Enabled = false;
        logoutBtn.Location = new System.Drawing.Point(870, 3);
        logoutBtn.Name = "logoutBtn";
        logoutBtn.Size = new System.Drawing.Size(130, 45);
        logoutBtn.TabIndex = 6;
        logoutBtn.Text = " Çıkış Yap";
        logoutBtn.UseVisualStyleBackColor = true;
        logoutBtn.Visible = false;
        // 
        // loginBtn
        // 
        loginBtn.Location = new System.Drawing.Point(870, 51);
        loginBtn.Name = "loginBtn";
        loginBtn.Size = new System.Drawing.Size(130, 45);
        loginBtn.TabIndex = 5;
        loginBtn.Text = "Giriş Yap";
        loginBtn.UseVisualStyleBackColor = true;
        loginBtn.Click += LoginBtnClick;
        // 
        // cartBtn
        // 
        cartBtn.Location = new System.Drawing.Point(743, 51);
        cartBtn.Name = "cartBtn";
        cartBtn.Size = new System.Drawing.Size(124, 45);
        cartBtn.TabIndex = 4;
        cartBtn.Text = "Sepet";
        cartBtn.UseVisualStyleBackColor = true;
        cartBtn.Click += cartBtn_Click;
        // 
        // homepageBtn
        // 
        homepageBtn.Location = new System.Drawing.Point(0, 29);
        homepageBtn.Name = "homepageBtn";
        homepageBtn.Size = new System.Drawing.Size(159, 67);
        homepageBtn.TabIndex = 3;
        homepageBtn.Text = "Anasayfa";
        homepageBtn.UseVisualStyleBackColor = true;
        homepageBtn.Click += homepageBtn_Click;
        // 
        // forwardBtn
        // 
        forwardBtn.Location = new System.Drawing.Point(105, 3);
        forwardBtn.Name = "forwardBtn";
        forwardBtn.Size = new System.Drawing.Size(54, 33);
        forwardBtn.TabIndex = 2;
        forwardBtn.Text = "İleri";
        forwardBtn.UseVisualStyleBackColor = true;
        forwardBtn.Click += forwardBtn_Click;
        // 
        // refreshBtn
        // 
        refreshBtn.Location = new System.Drawing.Point(45, 3);
        refreshBtn.Name = "refreshBtn";
        refreshBtn.Size = new System.Drawing.Size(63, 33);
        refreshBtn.TabIndex = 1;
        refreshBtn.Text = "Yenile";
        refreshBtn.UseVisualStyleBackColor = true;
        refreshBtn.Click += refreshBtn_Click_1;
        // 
        // backBtn
        // 
        backBtn.Location = new System.Drawing.Point(3, 3);
        backBtn.Name = "backBtn";
        backBtn.Size = new System.Drawing.Size(47, 33);
        backBtn.TabIndex = 0;
        backBtn.Text = "Geri";
        backBtn.UseVisualStyleBackColor = true;
        backBtn.Click += backBtn_Click_1;
        // 
        // pageContainer
        // 
        pageContainer.Dock = System.Windows.Forms.DockStyle.Fill;
        pageContainer.Location = new System.Drawing.Point(0, 96);
        pageContainer.Name = "pageContainer";
        pageContainer.Size = new System.Drawing.Size(1003, 657);
        pageContainer.TabIndex = 1;
        // 
        // Form1
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1003, 753);
        Controls.Add(pageContainer);
        Controls.Add(navbar);
        Text = "Form1";
        navbar.ResumeLayout(false);
        ResumeLayout(false);
    }

    private System.Windows.Forms.Button registerBtn;

    private System.Windows.Forms.Button logoutBtn;

    private System.Windows.Forms.Button homepageBtn;
    private System.Windows.Forms.Button cartBtn;
    private System.Windows.Forms.Button loginBtn;

    private System.Windows.Forms.Panel navbar;
    private System.Windows.Forms.Panel pageContainer;
    private System.Windows.Forms.Button backBtn;
    private System.Windows.Forms.Button refreshBtn;
    private System.Windows.Forms.Button forwardBtn;
    private System.Windows.Forms.Button orderBtn;

    #endregion
}