using System;
using System.Drawing;
using System.Windows.Forms;

public static class FormScalerDynamic
{
    public static void ScaleForm(Form form)
    {
        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

        float scaleX = (float)screenWidth / form.Width;
        float scaleY = (float)screenHeight / form.Height;

        scaleX = Math.Min(Math.Max(scaleX, 0.5f), 3f);
        scaleY = Math.Min(Math.Max(scaleY, 0.5f), 3f);

        ScaleControlRecursive(form, scaleX, scaleY);

        form.Width = (int)(form.Width * scaleX);
        form.Height = (int)(form.Height * scaleY);
    }

    private static void ScaleControlRecursive(Control ctrl, float scaleX, float scaleY)
    {
        ctrl.Left = (int)(ctrl.Left * scaleX);
        ctrl.Top = (int)(ctrl.Top * scaleY);

        int newWidth = (int)(ctrl.Width * scaleX);
        int newHeight = (int)(ctrl.Height * scaleY);

        if (ctrl is PictureBox pb && pb.Image != null)
        {
            float imgRatio = (float)pb.Image.Width / pb.Image.Height;
            float boxRatio = (float)newWidth / newHeight;

            if (boxRatio > imgRatio)
            {
                newWidth = (int)(newHeight * imgRatio);
            }
            else
            {
                newHeight = (int)(newWidth / imgRatio);
            }

            pb.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        ctrl.Width = newWidth;
        ctrl.Height = newHeight;

        if (ctrl.Font != null)
        {
            float newFontSize = ctrl.Font.Size * scaleX;
            newFontSize = Math.Max(8, newFontSize);
            ctrl.Font = new Font(ctrl.Font.FontFamily, newFontSize, ctrl.Font.Style);
        }

        foreach (Control child in ctrl.Controls)
        {
            ScaleControlRecursive(child, scaleX, scaleY);
        }
    }
}
