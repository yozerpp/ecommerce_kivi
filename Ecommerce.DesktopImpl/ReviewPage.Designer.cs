using System.ComponentModel;

namespace Ecommerce.DesktopImpl;

partial class ReviewPage 
{
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
        if (disposing && (components != null)){
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
        reviewView = new System.Windows.Forms.TreeView();
        sendBtn = new System.Windows.Forms.Button();
        textBox1 = new System.Windows.Forms.TextBox();
        SuspendLayout();
        // 
        // reviewView
        // 
        reviewView.Dock = System.Windows.Forms.DockStyle.Top;
        reviewView.Location = new System.Drawing.Point(0, 0);
        reviewView.Name = "reviewView";
        reviewView.Size = new System.Drawing.Size(1020, 559);
        reviewView.TabIndex = 0;
        // 
        // sendBtn
        // 
        sendBtn.Location = new System.Drawing.Point(3, 580);
        sendBtn.Name = "sendBtn";
        sendBtn.Size = new System.Drawing.Size(101, 44);
        sendBtn.TabIndex = 1;
        sendBtn.Text = "Gönder";
        sendBtn.UseVisualStyleBackColor = true;
        sendBtn.Click += sendBtn_Click;
        // 
        // textBox1
        // 
        textBox1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)162));
        textBox1.Location = new System.Drawing.Point(110, 565);
        textBox1.Multiline = true;
        textBox1.Name = "textBox1";
        textBox1.Size = new System.Drawing.Size(910, 73);
        textBox1.TabIndex = 2;
        // 
        // ReviewPage
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        Controls.Add(textBox1);
        Controls.Add(sendBtn);
        Controls.Add(reviewView);
        Size = new System.Drawing.Size(1020, 650);
        ResumeLayout(false);
        PerformLayout();
    }

    private System.Windows.Forms.Button sendBtn;
    private System.Windows.Forms.TextBox textBox1;

    private System.Windows.Forms.TreeView reviewView;

    #endregion
}