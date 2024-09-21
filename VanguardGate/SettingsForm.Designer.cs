namespace VanguardGate
{
    partial class SettingsForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox1 = new GroupBox();
            toggleOff = new RadioButton();
            toggleOn = new RadioButton();
            toggleAuto = new RadioButton();
            cancelButton = new Button();
            saveButton = new Button();
            button1 = new Button();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(toggleOff);
            groupBox1.Controls.Add(toggleOn);
            groupBox1.Controls.Add(toggleAuto);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(230, 108);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Control";
            // 
            // toggleOff
            // 
            toggleOff.AutoSize = true;
            toggleOff.Location = new Point(12, 74);
            toggleOff.Name = "toggleOff";
            toggleOff.Size = new Size(163, 19);
            toggleOff.TabIndex = 2;
            toggleOff.TabStop = true;
            toggleOff.Text = "Off (Vanguard Always Off)";
            toggleOff.UseVisualStyleBackColor = true;
            // 
            // toggleOn
            // 
            toggleOn.AutoSize = true;
            toggleOn.Location = new Point(12, 49);
            toggleOn.Name = "toggleOn";
            toggleOn.Size = new Size(161, 19);
            toggleOn.TabIndex = 1;
            toggleOn.TabStop = true;
            toggleOn.Text = "On (Vanguard Always On)";
            toggleOn.UseVisualStyleBackColor = true;
            // 
            // toggleAuto
            // 
            toggleAuto.AutoSize = true;
            toggleAuto.Location = new Point(12, 24);
            toggleAuto.Name = "toggleAuto";
            toggleAuto.Size = new Size(211, 19);
            toggleAuto.TabIndex = 0;
            toggleAuto.TabStop = true;
            toggleAuto.Text = "Auto (Detects League and Valorant)";
            toggleAuto.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            cancelButton.ImageAlign = ContentAlignment.BottomRight;
            cancelButton.Location = new Point(400, 240);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(75, 23);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // saveButton
            // 
            saveButton.Location = new Point(320, 240);
            saveButton.Name = "saveButton";
            saveButton.Size = new Size(75, 23);
            saveButton.TabIndex = 2;
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            button1.Location = new Point(440, 120);
            button1.Name = "button1";
            button1.Size = new Size(8, 8);
            button1.TabIndex = 3;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(486, 276);
            Controls.Add(button1);
            Controls.Add(saveButton);
            Controls.Add(cancelButton);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Margin = new Padding(2);
            Name = "SettingsForm";
            Text = "VanguardGate Settings";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox1;
        private RadioButton toggleOff;
        private RadioButton toggleOn;
        private RadioButton toggleAuto;
        private Button cancelButton;
        private Button saveButton;
        private Button button1;
    }
}