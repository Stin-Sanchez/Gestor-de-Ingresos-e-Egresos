namespace GestorIngresosEgresos.Vista
{
    partial class FormPeriodo
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FormPeriodo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 750);
            this.Name = "FormPeriodo";
            this.Text = "Periodo";
            this.Load += new System.EventHandler(this.FormPeriodo_Load);
            this.ResumeLayout(false);

        }
    }
}
