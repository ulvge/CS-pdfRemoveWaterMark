
namespace pdfRemoveWaterMark
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.tb_fileRoot = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.tb_warterMark = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cb_isText = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cb_isImage = new System.Windows.Forms.CheckBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.w_max = new System.Windows.Forms.TextBox();
            this.w_min = new System.Windows.Forms.TextBox();
            this.h_min = new System.Windows.Forms.TextBox();
            this.h_max = new System.Windows.Forms.TextBox();
            this.tb_log = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.rb_range = new System.Windows.Forms.RadioButton();
            this.rb_all = new System.Windows.Forms.RadioButton();
            this.tb_Range = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // tb_fileRoot
            // 
            this.tb_fileRoot.AllowDrop = true;
            this.tb_fileRoot.Location = new System.Drawing.Point(130, 22);
            this.tb_fileRoot.Margin = new System.Windows.Forms.Padding(4);
            this.tb_fileRoot.Name = "tb_fileRoot";
            this.tb_fileRoot.Size = new System.Drawing.Size(264, 25);
            this.tb_fileRoot.TabIndex = 5;
            this.tb_fileRoot.DragDrop += new System.Windows.Forms.DragEventHandler(this.tb_fileRoot_DragDrop);
            this.tb_fileRoot.DragEnter += new System.Windows.Forms.DragEventHandler(this.tb_fileRoot_DragEnter);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(31, 30);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 15);
            this.label1.TabIndex = 4;
            this.label1.Text = "pdf文件目录";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(402, 18);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(88, 29);
            this.button1.TabIndex = 3;
            this.button1.Text = "选择文件";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tb_warterMark
            // 
            this.tb_warterMark.Location = new System.Drawing.Point(17, 18);
            this.tb_warterMark.Margin = new System.Windows.Forms.Padding(4);
            this.tb_warterMark.Multiline = true;
            this.tb_warterMark.Name = "tb_warterMark";
            this.tb_warterMark.Size = new System.Drawing.Size(343, 82);
            this.tb_warterMark.TabIndex = 5;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cb_isText);
            this.groupBox1.Controls.Add(this.tb_warterMark);
            this.groupBox1.Location = new System.Drawing.Point(34, 142);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(375, 100);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            // 
            // cb_isText
            // 
            this.cb_isText.AutoSize = true;
            this.cb_isText.Location = new System.Drawing.Point(6, 0);
            this.cb_isText.Name = "cb_isText";
            this.cb_isText.Size = new System.Drawing.Size(104, 19);
            this.cb_isText.TabIndex = 6;
            this.cb_isText.Text = "水印是文本";
            this.cb_isText.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.cb_isImage);
            this.groupBox2.Controls.Add(this.textBox2);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.w_max);
            this.groupBox2.Controls.Add(this.w_min);
            this.groupBox2.Controls.Add(this.h_min);
            this.groupBox2.Controls.Add(this.h_max);
            this.groupBox2.Location = new System.Drawing.Point(34, 249);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(456, 166);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            // 
            // cb_isImage
            // 
            this.cb_isImage.AutoSize = true;
            this.cb_isImage.Location = new System.Drawing.Point(6, -1);
            this.cb_isImage.Name = "cb_isImage";
            this.cb_isImage.Size = new System.Drawing.Size(104, 19);
            this.cb_isImage.TabIndex = 6;
            this.cb_isImage.Text = "水印是图片";
            this.cb_isImage.UseVisualStyleBackColor = true;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(17, 25);
            this.textBox2.Margin = new System.Windows.Forms.Padding(4);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(283, 93);
            this.textBox2.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(308, 97);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(67, 15);
            this.label4.TabIndex = 4;
            this.label4.Text = "最大高度";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(308, 49);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 15);
            this.label3.TabIndex = 4;
            this.label3.Text = "最小高度";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(164, 129);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(67, 15);
            this.label5.TabIndex = 4;
            this.label5.Text = "最大宽度";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 129);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 15);
            this.label2.TabIndex = 4;
            this.label2.Text = "最小宽度";
            // 
            // w_max
            // 
            this.w_max.Location = new System.Drawing.Point(235, 126);
            this.w_max.Margin = new System.Windows.Forms.Padding(4);
            this.w_max.Name = "w_max";
            this.w_max.Size = new System.Drawing.Size(65, 25);
            this.w_max.TabIndex = 5;
            // 
            // w_min
            // 
            this.w_min.Location = new System.Drawing.Point(84, 126);
            this.w_min.Margin = new System.Windows.Forms.Padding(4);
            this.w_min.Name = "w_min";
            this.w_min.Size = new System.Drawing.Size(74, 25);
            this.w_min.TabIndex = 5;
            // 
            // h_min
            // 
            this.h_min.Location = new System.Drawing.Point(383, 39);
            this.h_min.Margin = new System.Windows.Forms.Padding(4);
            this.h_min.Name = "h_min";
            this.h_min.Size = new System.Drawing.Size(54, 25);
            this.h_min.TabIndex = 5;
            // 
            // h_max
            // 
            this.h_max.Location = new System.Drawing.Point(383, 87);
            this.h_max.Margin = new System.Windows.Forms.Padding(4);
            this.h_max.Name = "h_max";
            this.h_max.Size = new System.Drawing.Size(54, 25);
            this.h_max.TabIndex = 5;
            // 
            // tb_log
            // 
            this.tb_log.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tb_log.Location = new System.Drawing.Point(7, 25);
            this.tb_log.Margin = new System.Windows.Forms.Padding(4);
            this.tb_log.Multiline = true;
            this.tb_log.Name = "tb_log";
            this.tb_log.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tb_log.Size = new System.Drawing.Size(485, 371);
            this.tb_log.TabIndex = 30;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.tb_log);
            this.groupBox3.Location = new System.Drawing.Point(496, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(499, 403);
            this.groupBox3.TabIndex = 31;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "日志";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(402, 60);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(87, 29);
            this.button2.TabIndex = 3;
            this.button2.Text = "处  理";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.rb_range);
            this.groupBox4.Controls.Add(this.rb_all);
            this.groupBox4.Controls.Add(this.tb_Range);
            this.groupBox4.Location = new System.Drawing.Point(40, 71);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(355, 56);
            this.groupBox4.TabIndex = 32;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "指定页面";
            // 
            // rb_range
            // 
            this.rb_range.AutoSize = true;
            this.rb_range.Location = new System.Drawing.Point(84, 24);
            this.rb_range.Name = "rb_range";
            this.rb_range.Size = new System.Drawing.Size(58, 19);
            this.rb_range.TabIndex = 0;
            this.rb_range.TabStop = true;
            this.rb_range.Text = "范围";
            this.rb_range.UseVisualStyleBackColor = true;
            this.rb_range.CheckedChanged += new System.EventHandler(this.rb_CheckedChanged);
            // 
            // rb_all
            // 
            this.rb_all.AutoSize = true;
            this.rb_all.Location = new System.Drawing.Point(20, 25);
            this.rb_all.Name = "rb_all";
            this.rb_all.Size = new System.Drawing.Size(58, 19);
            this.rb_all.TabIndex = 0;
            this.rb_all.TabStop = true;
            this.rb_all.Text = "全部";
            this.rb_all.UseVisualStyleBackColor = true;
            this.rb_all.CheckedChanged += new System.EventHandler(this.rb_CheckedChanged);
            // 
            // tb_Range
            // 
            this.tb_Range.Location = new System.Drawing.Point(149, 18);
            this.tb_Range.Margin = new System.Windows.Forms.Padding(4);
            this.tb_Range.Name = "tb_Range";
            this.tb_Range.Size = new System.Drawing.Size(199, 25);
            this.tb_Range.TabIndex = 5;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1010, 450);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.tb_fileRoot);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "pdfRemoveWaterMark";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tb_fileRoot;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox tb_warterMark;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox h_min;
        private System.Windows.Forms.TextBox h_max;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox w_max;
        private System.Windows.Forms.TextBox w_min;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tb_log;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox cb_isText;
        private System.Windows.Forms.CheckBox cb_isImage;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.RadioButton rb_range;
        private System.Windows.Forms.RadioButton rb_all;
        private System.Windows.Forms.TextBox tb_Range;
    }
}

