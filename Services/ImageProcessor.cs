using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PointObjectDetection.Core
{
    /// <summary>
    /// Класс для работы с файлами изображений и маской повреждений
    /// Разработчик 1 (Алина): Отвечает за пункты: ввод данных, маска повреждений, сохранение результатов
    /// </summary>
    public static class ImageProcessor
    {
        /// <summary>
        /// Загрузка изображения из файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="errorMessage">Сообщение об ошибке (если есть)</param>
        /// <returns>Загруженное изображение или null</returns>
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

        /// <summary>
        /// Сохранение результатов
        /// </summary>
        /// <param name="filePath">Путь для сохранения</param>
        /// <param name="reportText">Текст отчета</param>
        /// <param name="image">Изображение с разметкой</param>
        /// <param name="errorMessage">Сообщение об ошибке</param>
        /// <returns>Успешно ли сохранение</returns>
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

        /// <summary>
        /// Создание новой маски повреждений
        /// </summary>
        public static bool[,] CreateDamageMask(int width, int height)
        {
            return new bool[width, height];
        }

        /// <summary>
        /// Получение яркости пикселя (для отображения в строке состояния)
        /// </summary>
        public static byte GetPixelBrightness(Bitmap image, int x, int y)
        {
            if (x < 0 || x >= image.Width || y < 0 || y >= image.Height)
                return 0;

            Color pixel = image.GetPixel(x, y);
            return (byte)((pixel.R + pixel.G + pixel.B) / 3);
        }
    }
}