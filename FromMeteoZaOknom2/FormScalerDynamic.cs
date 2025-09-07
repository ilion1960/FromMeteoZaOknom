using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace FromMeteoZaOknom2
{
    public static class FormScalerDynamic
    {
        private const int BASE_WIDTH = 1024;
        private const int BASE_HEIGHT = 768;
        private const float MAX_FONT_SCALE = 1.2f;  // Максимальный масштаб шрифта (1.2x)
        private const float MIN_FONT_SIZE = 8f;    // Минимальный размер шрифта
        private const float MAX_FONT_SIZE = 14f;   // Максимальный размер шрифта (кроме крупных меток)

        public static void ScaleForm(Form form)
        {
            // Получаем текущее разрешение экрана
            var screen = Screen.PrimaryScreen.Bounds;
            float heightScale = (float)screen.Height / BASE_HEIGHT; // 1080 / 768 ≈ 1.40625
            float fontScale = Math.Min(heightScale, MAX_FONT_SCALE); // Ограничиваем масштаб шрифта

            // Устанавливаем размер формы
            form.FormBorderStyle = FormBorderStyle.None;
            form.WindowState = FormWindowState.Maximized;
            form.Bounds = screen;

            // Масштабируем элементы управления (кроме WebView2)
            ScaleControls(form.Controls, heightScale, fontScale);

            // Логируем размер формы и экрана для отладки
            form.Tag = $"Размер формы: {form.Width}x{form.Height}, Экран: {screen.Width}x{screen.Height}";
        }

        private static void ScaleControls(Control.ControlCollection controls, float heightScale, float fontScale)
        {
            foreach (Control control in controls)
            {
                // Пропускаем WebView2 элементы
                if (control is WebView2)
                {
                    continue;
                }

                // Масштабируем только вертикальные параметры (Top, Height)
                control.Top = (int)(control.Top * heightScale);
                control.Height = (int)(control.Height * heightScale);

                // Масштабируем шрифт с ограничением
                if (control.Font != null)
                {
                    // Проверяем, является ли элемент крупной меткой (например, label6, label7, label4, label15)
                    float maxFontSize = control.Name is "label6" or "label7" or "label4" or "label15" ? 100f : MAX_FONT_SIZE;
                    float newFontSize = control.Font.Size * fontScale;
                    newFontSize = Math.Max(MIN_FONT_SIZE, Math.Min(newFontSize, maxFontSize));
                    control.Font = new Font(control.Font.FontFamily, newFontSize, control.Font.Style);
                }

                // Рекурсивно масштабируем вложенные элементы (например, в панелях)
                if (control.HasChildren)
                {
                    ScaleControls(control.Controls, heightScale, fontScale);
                }
            }
        }
    }
}