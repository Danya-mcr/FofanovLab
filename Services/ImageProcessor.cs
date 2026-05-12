using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PointObjectDetection.Core
{
    /// Класс для работы с файлами изображений и маской повреждений
    /// Разработчик 1 (Алина)
    public static class ImageProcessor
    {
        /// Загрузка изображения из файла
        public static Bitmap LoadImage(string filePath, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                if (!File.Exists(filePath))
                {
                    errorMessage = "Файл не найден";
                    return null;
                }

                Bitmap image = new Bitmap(filePath);
                return image;
            }
            catch (Exception ex)
            {
                errorMessage = $"Ошибка загрузки: {ex.Message}";
                return null;
            }
        }

        /// Сохранение результатов
        public static bool SaveResult(string filePath, string reportText, Bitmap image, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                string ext = Path.GetExtension(filePath).ToLower();

                if (ext == ".txt")
                {
                    File.WriteAllText(filePath, reportText);
                }
                else if (ext == ".png" && image != null)
                {
                    image.Save(filePath, ImageFormat.Png);
                }
                else if ((ext == ".jpg" || ext == ".jpeg") && image != null)
                {
                    image.Save(filePath, ImageFormat.Jpeg);
                }
                else if (ext == ".bmp" && image != null)
                {
                    image.Save(filePath, ImageFormat.Bmp);
                }
                else
                {
                    errorMessage = "Неподдерживаемый формат или нет изображения";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Ошибка сохранения: {ex.Message}";
                return false;
            }
        }

        /// Создание новой маски повреждений
        public static bool[,] CreateDamageMask(int width, int height)
        {
            return new bool[width, height];
        }

        /// Получение яркости пикселя (для отображения в строке состояния)
        public static byte GetPixelBrightness(Bitmap image, int x, int y)
        {
            if (x < 0 || x >= image.Width || y < 0 || y >= image.Height)
                return 0;

            Color pixel = image.GetPixel(x, y);
            return (byte)((pixel.R + pixel.G + pixel.B) / 3);
        }
    }
}