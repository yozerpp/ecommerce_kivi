namespace Ecommerce.DesktopImpl
{
    partial class SellerPage
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
        private void InitializeComponent()
        {
            OffersView = new DataGridView();
            shopNameBox = new TextBox();
            addressBox = new TextBox();
            reviewsView = new DataGridView();
            offersNextBtn = new Button();
            offersPrevBtn = new Button();
            reviewsPrevBtn = new Button();
            reviewsNextBtn = new Button();
            addressBox1 = new RichTextBox();
            couponsView = new DataGridView();
            aggregateBox = new ListBox();
            ((System.ComponentModel.ISupportInitialize)OffersView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)reviewsView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)couponsView).BeginInit();
            SuspendLayout();
            // 
            // OffersView
            // 
            OffersView.AllowUserToAddRows = false;
            OffersView.AllowUserToDeleteRows = false;
            OffersView.ColumnHeadersHeight = 29;
            OffersView.Location = new Point(0, 56);
            OffersView.Name = "OffersView";
            OffersView.RowHeadersWidth = 51;
            OffersView.Size = new Size(609, 218);
            OffersView.TabIndex = 0;
            OffersView.Text = "dataGridView1";
            OffersView.RowHeaderMouseDoubleClick += GoToProduct;
            // 
            // shopNameBox
            // 
            shopNameBox.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 162);
            shopNameBox.Location = new Point(343, 3);
            shopNameBox.Name = "shopNameBox";
            shopNameBox.ReadOnly = true;
            shopNameBox.Size = new Size(269, 47);
            shopNameBox.TabIndex = 1;
            shopNameBox.TextAlign = HorizontalAlignment.Center;
            // 
            // addressBox
            // 
            addressBox.Anchor = AnchorStyles.Bottom;
            addressBox.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 162);
            addressBox.Location = new Point(2099, 1767);
            addressBox.Name = "addressBox";
            addressBox.Size = new Size(403, 30);
            addressBox.TabIndex = 2;
            // 
            // reviewsView
            // 
            reviewsView.AllowUserToAddRows = false;
            reviewsView.AllowUserToDeleteRows = false;
            reviewsView.ColumnHeadersHeight = 29;
            reviewsView.Location = new Point(3, 280);
            reviewsView.Name = "reviewsView";
            reviewsView.RowHeadersWidth = 51;
            reviewsView.Size = new Size(501, 197);
            reviewsView.TabIndex = 3;
            reviewsView.Text = "dataGridView2";
            // 
            // offersNextBtn
            // 
            offersNextBtn.Location = new Point(914, 178);
            offersNextBtn.Name = "offersNextBtn";
            offersNextBtn.Size = new Size(29, 51);
            offersNextBtn.TabIndex = 4;
            offersNextBtn.Text = "S";
            offersNextBtn.UseVisualStyleBackColor = true;
            offersNextBtn.Click += offersNextBtn_Click;
            // 
            // offersPrevBtn
            // 
            offersPrevBtn.Location = new Point(914, 122);
            offersPrevBtn.Name = "offersPrevBtn";
            offersPrevBtn.Size = new Size(28, 50);
            offersPrevBtn.TabIndex = 5;
            offersPrevBtn.Text = "Ö";
            offersPrevBtn.UseVisualStyleBackColor = true;
            offersPrevBtn.Click += offersPrevBtn_Click;
            // 
            // reviewsPrevBtn
            // 
            reviewsPrevBtn.Location = new Point(507, 348);
            reviewsPrevBtn.Name = "reviewsPrevBtn";
            reviewsPrevBtn.Size = new Size(27, 33);
            reviewsPrevBtn.TabIndex = 6;
            reviewsPrevBtn.Text = "Ö";
            reviewsPrevBtn.UseVisualStyleBackColor = true;
            reviewsPrevBtn.Click += reviewsPrevBtn_Click;
            // 
            // reviewsNextBtn
            // 
            reviewsNextBtn.Location = new Point(507, 387);
            reviewsNextBtn.Name = "reviewsNextBtn";
            reviewsNextBtn.Size = new Size(27, 32);
            reviewsNextBtn.TabIndex = 7;
            reviewsNextBtn.Text = "S";
            reviewsNextBtn.UseVisualStyleBackColor = true;
            reviewsNextBtn.Click += reviewsNextBtn_Click;
            // 
            // addressBox1
            // 
            addressBox1.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 162);
            addressBox1.Location = new Point(540, 350);
            addressBox1.Name = "addressBox1";
            addressBox1.Size = new Size(402, 127);
            addressBox1.TabIndex = 8;
            addressBox1.Text = "";
            // 
            // couponsView
            // 
            couponsView.AllowUserToAddRows = false;
            couponsView.AllowUserToDeleteRows = false;
            couponsView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            couponsView.Location = new Point(615, 56);
            couponsView.Name = "couponsView";
            couponsView.ReadOnly = true;
            couponsView.RowHeadersWidth = 51;
            couponsView.Size = new Size(290, 218);
            couponsView.TabIndex = 9;
            // 
            // aggregateBox
            // 
            aggregateBox.Font = new Font("Segoe UI", 9F);
            aggregateBox.FormattingEnabled = true;
            aggregateBox.Location = new Point(540, 280);
            aggregateBox.MultiColumn = true;
            aggregateBox.Name = "aggregateBox";
            aggregateBox.Size = new Size(402, 64);
            aggregateBox.TabIndex = 10;
            // 
            // SellerPage
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(aggregateBox);
            Controls.Add(couponsView);
            Controls.Add(addressBox1);
            Controls.Add(reviewsNextBtn);
            Controls.Add(reviewsPrevBtn);
            Controls.Add(offersPrevBtn);
            Controls.Add(offersNextBtn);
            Controls.Add(reviewsView);
            Controls.Add(addressBox);
            Controls.Add(shopNameBox);
            Controls.Add(OffersView);
            Name = "SellerPage";
            Size = new Size(947, 480);
            ((System.ComponentModel.ISupportInitialize)OffersView).EndInit();
            ((System.ComponentModel.ISupportInitialize)reviewsView).EndInit();
            ((System.ComponentModel.ISupportInitialize)couponsView).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.RichTextBox addressBox1;

        private System.Windows.Forms.Button offersNextBtn;
        private System.Windows.Forms.Button offersPrevBtn;
        private System.Windows.Forms.Button reviewsPrevBtn;
        private System.Windows.Forms.Button reviewsNextBtn;

        private System.Windows.Forms.DataGridView reviewsView;

        private System.Windows.Forms.DataGridView OffersView;
        private System.Windows.Forms.TextBox shopNameBox;
        private System.Windows.Forms.TextBox addressBox;

        #endregion

        private DataGridView couponsView;
        private ListBox aggregateBox;
    }
}
