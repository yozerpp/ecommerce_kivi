namespace Ecommerce.DesktopImpl
{
    partial class ProductPage
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
            panel1 = new System.Windows.Forms.Panel();
            censorNameBtn = new System.Windows.Forms.CheckBox();
            pageNext = new System.Windows.Forms.Button();
            pageBack = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            ratingBox = new System.Windows.Forms.TextBox();
            adetLabel = new System.Windows.Forms.Label();
            quantityBox = new System.Windows.Forms.TextBox();
            commentBox = new System.Windows.Forms.TextBox();
            commentBtn = new System.Windows.Forms.Button();
            addToCartBtn = new System.Windows.Forms.Button();
            listBox1 = new System.Windows.Forms.ListBox();
            reviewView = new System.Windows.Forms.DataGridView();
            offersView = new System.Windows.Forms.DataGridView();
            textBox1 = new System.Windows.Forms.TextBox();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            title1 = new System.Windows.Forms.TextBox();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)reviewView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)offersView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(censorNameBtn);
            panel1.Controls.Add(pageNext);
            panel1.Controls.Add(pageBack);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(ratingBox);
            panel1.Controls.Add(adetLabel);
            panel1.Controls.Add(quantityBox);
            panel1.Controls.Add(commentBox);
            panel1.Controls.Add(commentBtn);
            panel1.Controls.Add(addToCartBtn);
            panel1.Controls.Add(listBox1);
            panel1.Controls.Add(reviewView);
            panel1.Controls.Add(offersView);
            panel1.Controls.Add(textBox1);
            panel1.Controls.Add(pictureBox1);
            panel1.Controls.Add(title1);
            panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1020, 650);
            panel1.TabIndex = 0;
            // 
            // censorNameBtn
            // 
            censorNameBtn.Location = new System.Drawing.Point(892, 621);
            censorNameBtn.Name = "censorNameBtn";
            censorNameBtn.Size = new System.Drawing.Size(125, 24);
            censorNameBtn.TabIndex = 16;
            censorNameBtn.Text = "İsmini Gizle";
            censorNameBtn.UseVisualStyleBackColor = true;
            // 
            // pageNext
            // 
            pageNext.Location = new System.Drawing.Point(991, 544);
            pageNext.Name = "pageNext";
            pageNext.Size = new System.Drawing.Size(26, 43);
            pageNext.TabIndex = 15;
            pageNext.Text = "S";
            pageNext.UseVisualStyleBackColor = true;
            pageNext.Click += pageNext_Click;
            // 
            // pageBack
            // 
            pageBack.Location = new System.Drawing.Point(991, 496);
            pageBack.Name = "pageBack";
            pageBack.Size = new System.Drawing.Size(26, 42);
            pageBack.TabIndex = 14;
            pageBack.Text = "Ö";
            pageBack.UseVisualStyleBackColor = true;
            pageBack.Click += pageBack_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(151, 623);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(41, 20);
            label1.TabIndex = 13;
            label1.Text = "Puan";
            // 
            // ratingBox
            // 
            ratingBox.Anchor = System.Windows.Forms.AnchorStyles.Top;
            ratingBox.Location = new System.Drawing.Point(198, 619);
            ratingBox.Name = "ratingBox";
            ratingBox.Size = new System.Drawing.Size(77, 27);
            ratingBox.TabIndex = 12;
            ratingBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // adetLabel
            // 
            adetLabel.AutoSize = true;
            adetLabel.Location = new System.Drawing.Point(544, 416);
            adetLabel.Name = "adetLabel";
            adetLabel.Size = new System.Drawing.Size(41, 20);
            adetLabel.TabIndex = 11;
            adetLabel.Text = "Adet";
            // 
            // quantityBox
            // 
            quantityBox.Location = new System.Drawing.Point(591, 413);
            quantityBox.Name = "quantityBox";
            quantityBox.Size = new System.Drawing.Size(157, 27);
            quantityBox.TabIndex = 10;
            // 
            // commentBox
            // 
            commentBox.Location = new System.Drawing.Point(281, 614);
            commentBox.Multiline = true;
            commentBox.Name = "commentBox";
            commentBox.Size = new System.Drawing.Size(605, 34);
            commentBox.TabIndex = 9;
            // 
            // commentBtn
            // 
            commentBtn.Location = new System.Drawing.Point(0, 614);
            commentBtn.Name = "commentBtn";
            commentBtn.Size = new System.Drawing.Size(145, 36);
            commentBtn.TabIndex = 8;
            commentBtn.Text = "Yorum/Değerlendir";
            commentBtn.UseVisualStyleBackColor = true;
            commentBtn.Click += commentBtn_Click;
            // 
            // addToCartBtn
            // 
            addToCartBtn.Location = new System.Drawing.Point(433, 411);
            addToCartBtn.Name = "addToCartBtn";
            addToCartBtn.Size = new System.Drawing.Size(101, 29);
            addToCartBtn.TabIndex = 7;
            addToCartBtn.Text = "Sepete Ekle";
            addToCartBtn.UseVisualStyleBackColor = true;
            addToCartBtn.Click += addToCartBtn_Click;
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.Location = new System.Drawing.Point(206, 176);
            listBox1.Name = "listBox1";
            listBox1.Size = new System.Drawing.Size(221, 264);
            listBox1.TabIndex = 6;
            // 
            // reviewView
            // 
            reviewView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            reviewView.Location = new System.Drawing.Point(3, 446);
            reviewView.Name = "reviewView";
            reviewView.RowHeadersWidth = 51;
            reviewView.Size = new System.Drawing.Size(982, 167);
            reviewView.TabIndex = 5;
            reviewView.CellContentClick += reviewsView_CellContentClick;
            reviewView.RowHeaderMouseDoubleClick+= reviewView_RowHeaderMouseDoubleClick;
            // 
            // offersView
            // 
            offersView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            offersView.Location = new System.Drawing.Point(433, 176);
            offersView.Name = "offersView";
            offersView.RowHeadersWidth = 51;
            offersView.Size = new System.Drawing.Size(584, 229);
            offersView.TabIndex = 4;
            offersView.CellContentClick += offersView_CellContentClick;
            // 
            // textBox1
            // 
            textBox1.Location = new System.Drawing.Point(206, 43);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ReadOnly = true;
            textBox1.Size = new System.Drawing.Size(792, 127);
            textBox1.TabIndex = 2;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new System.Drawing.Point(3, 105);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(200, 300);
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // title1
            // 
            title1.AllowDrop = true;
            title1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)162));
            title1.Location = new System.Drawing.Point(151, 3);
            title1.Multiline = true;
            title1.Name = "title1";
            title1.ReadOnly = true;
            title1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            title1.Size = new System.Drawing.Size(689, 34);
            title1.TabIndex = 0;
            // 
            // ProductPage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(panel1);
            Size = new System.Drawing.Size(1020, 650);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)reviewView).EndInit();
            ((System.ComponentModel.ISupportInitialize)offersView).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        private System.Windows.Forms.CheckBox censorNameBtn;

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox title1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.DataGridView offersView;
        private System.Windows.Forms.DataGridView reviewView;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button addToCartBtn;
        private System.Windows.Forms.TextBox commentBox;
        private System.Windows.Forms.Button commentBtn;
        private System.Windows.Forms.Label adetLabel;
        private System.Windows.Forms.TextBox quantityBox;
        private System.Windows.Forms.TextBox ratingBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button pageNext;
        private System.Windows.Forms.Button pageBack;
    }
}
