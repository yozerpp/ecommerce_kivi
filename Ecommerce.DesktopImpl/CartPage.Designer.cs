namespace Ecommerce.DesktopImpl
{
    partial class CartPage
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
            cartView = new System.Windows.Forms.DataGridView();
            orderBtn = new System.Windows.Forms.Button();
            deleteBtn = new System.Windows.Forms.Button();
            clearBtn = new System.Windows.Forms.Button();
            addBtn = new System.Windows.Forms.Button();
            decrementButton = new System.Windows.Forms.Button();
            couponBox = new System.Windows.Forms.TextBox();
            couponBtn = new System.Windows.Forms.Button();
            aggregateBpx = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)cartView).BeginInit();
            SuspendLayout();
            // 
            // cartView
            // 
            cartView.ColumnHeadersHeight = 29;
            cartView.Location = new System.Drawing.Point(3, 0);
            cartView.Name = "cartView";
            cartView.RowHeadersWidth = 51;
            cartView.Size = new System.Drawing.Size(791, 345);
            cartView.TabIndex = 0;
            cartView.CellContentClick += cartView_CellContentClick;
            // 
            // orderBtn
            // 
            orderBtn.Location = new System.Drawing.Point(656, 348);
            orderBtn.Name = "orderBtn";
            orderBtn.Size = new System.Drawing.Size(135, 52);
            orderBtn.TabIndex = 1;
            orderBtn.Text = "Siparişi Tamamla";
            orderBtn.UseVisualStyleBackColor = true;
            orderBtn.Click += orderBtn_Click;
            // 
            // deleteBtn
            // 
            deleteBtn.Location = new System.Drawing.Point(3, 351);
            deleteBtn.Name = "deleteBtn";
            deleteBtn.Size = new System.Drawing.Size(108, 49);
            deleteBtn.TabIndex = 2;
            deleteBtn.Text = "Sil";
            deleteBtn.UseVisualStyleBackColor = true;
            deleteBtn.Click += deleteBtn_Click;
            // 
            // clearBtn
            // 
            clearBtn.Location = new System.Drawing.Point(552, 349);
            clearBtn.Name = "clearBtn";
            clearBtn.Size = new System.Drawing.Size(98, 49);
            clearBtn.TabIndex = 3;
            clearBtn.Text = "Temizle";
            clearBtn.UseVisualStyleBackColor = true;
            clearBtn.Click += clearBtn_Click;
            // 
            // addBtn
            // 
            addBtn.Location = new System.Drawing.Point(117, 351);
            addBtn.Name = "addBtn";
            addBtn.Size = new System.Drawing.Size(47, 49);
            addBtn.TabIndex = 4;
            addBtn.Text = "Ekle";
            addBtn.UseVisualStyleBackColor = true;
            addBtn.Click += addBtn_Click;
            // 
            // decrementButton
            // 
            decrementButton.Location = new System.Drawing.Point(170, 351);
            decrementButton.Name = "decrementButton";
            decrementButton.Size = new System.Drawing.Size(53, 49);
            decrementButton.TabIndex = 5;
            decrementButton.Text = "Azalt";
            decrementButton.UseVisualStyleBackColor = true;
            decrementButton.Click += decrementButton_Click;
            // 
            // couponBox
            // 
            couponBox.Location = new System.Drawing.Point(329, 361);
            couponBox.Name = "couponBox";
            couponBox.Size = new System.Drawing.Size(217, 27);
            couponBox.TabIndex = 6;
            // 
            // couponBtn
            // 
            couponBtn.Location = new System.Drawing.Point(229, 360);
            couponBtn.Name = "couponBtn";
            couponBtn.Size = new System.Drawing.Size(94, 29);
            couponBtn.TabIndex = 7;
            couponBtn.Text = "Kupon Ekle";
            couponBtn.UseVisualStyleBackColor = true;
            couponBtn.Click += couponBtn_Click;
            // 
            // aggregateBpx
            // 
            aggregateBpx.Location = new System.Drawing.Point(800, 0);
            aggregateBpx.Name = "aggregateBpx";
            aggregateBpx.Size = new System.Drawing.Size(176, 345);
            aggregateBpx.TabIndex = 8;
            aggregateBpx.Text = "";
            // 
            // CartPage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(aggregateBpx);
            Controls.Add(couponBtn);
            Controls.Add(couponBox);
            Controls.Add(decrementButton);
            Controls.Add(addBtn);
            Controls.Add(clearBtn);
            Controls.Add(deleteBtn);
            Controls.Add(orderBtn);
            Controls.Add(cartView);
            Size = new System.Drawing.Size(979, 403);
            ((System.ComponentModel.ISupportInitialize)cartView).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.RichTextBox aggregateBpx;

        #endregion

        private System.Windows.Forms.DataGridView cartView;
        private System.Windows.Forms.Button orderBtn;
        private Button deleteBtn;
        private System.Windows.Forms.Button clearBtn;
        private Button addBtn;
        private Button decrementButton;
        private System.Windows.Forms.TextBox couponBox;
        private System.Windows.Forms.Button couponBtn;
    }
}
