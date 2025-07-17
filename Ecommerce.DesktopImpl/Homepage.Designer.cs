namespace Ecommerce.DesktopImpl
{
    partial class Homepage
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
            nextBtn = new System.Windows.Forms.Button();
            prevBtn = new System.Windows.Forms.Button();
            search = new System.Windows.Forms.Button();
            searchBar1 = new System.Windows.Forms.TextBox();
            panel2 = new System.Windows.Forms.Panel();
            searchResults = new System.Windows.Forms.DataGridView();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)searchResults).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(nextBtn);
            panel1.Controls.Add(prevBtn);
            panel1.Controls.Add(search);
            panel1.Controls.Add(searchBar1);
            panel1.Dock = System.Windows.Forms.DockStyle.Top;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1020, 76);
            panel1.TabIndex = 0;
            // 
            // nextBtn
            // 
            nextBtn.Location = new System.Drawing.Point(829, 49);
            nextBtn.Name = "nextBtn";
            nextBtn.Size = new System.Drawing.Size(75, 23);
            nextBtn.TabIndex = 3;
            nextBtn.Text = "İleri";
            nextBtn.UseVisualStyleBackColor = true;
            nextBtn.Click += nextBtn_Click;
            // 
            // prevBtn
            // 
            prevBtn.Location = new System.Drawing.Point(748, 49);
            prevBtn.Name = "prevBtn";
            prevBtn.Size = new System.Drawing.Size(75, 23);
            prevBtn.TabIndex = 2;
            prevBtn.Text = "Geri";
            prevBtn.UseVisualStyleBackColor = true;
            prevBtn.Click += prevBtn_Click;
            // 
            // search
            // 
            search.Location = new System.Drawing.Point(8, 6);
            search.Name = "search";
            search.Size = new System.Drawing.Size(126, 50);
            search.TabIndex = 1;
            search.Text = "Ara";
            search.UseVisualStyleBackColor = true;
            search.Click += search_Click;
            // 
            // searchBar1
            // 
            searchBar1.Location = new System.Drawing.Point(140, 16);
            searchBar1.Name = "searchBar1";
            searchBar1.Size = new System.Drawing.Size(764, 27);
            searchBar1.TabIndex = 0;
            // 
            // panel2
            // 
            panel2.Controls.Add(searchResults);
            panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            panel2.Location = new System.Drawing.Point(0, 76);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(1020, 574);
            panel2.TabIndex = 1;
            // 
            // searchResults
            // 
            searchResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            searchResults.Dock = System.Windows.Forms.DockStyle.Fill;
            searchResults.Location = new System.Drawing.Point(0, 0);
            searchResults.Name = "searchResults";
            searchResults.RowHeadersWidth = 51;
            searchResults.Size = new System.Drawing.Size(1020, 574);
            searchResults.TabIndex = 0;
            searchResults.RowHeaderMouseDoubleClick += searchResults_CellContentClick_1;
            // 
            // Homepage
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(panel2);
            Controls.Add(panel1);
            Size = new System.Drawing.Size(1020, 650);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)searchResults).EndInit();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Button prevBtn;
        private System.Windows.Forms.Button nextBtn;

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button search;
        private System.Windows.Forms.TextBox searchBar1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView searchResults;
    }
}
