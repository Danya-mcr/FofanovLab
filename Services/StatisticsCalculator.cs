using System;
using System.Collections.Generic;
using System.Drawing;

namespace PointObjectDetection.Core
{
    /// <summary>
    /// Класс для перебора пикселей и расчета статистических характеристик
    /// Разработчик 2 (Данил): Отвечает за пункты: перебор пикселей, оценка μ и σ
    /// </summary>
    public static class StatisticsCalculator
    {
        /// <summary>
        /// Получение яркости пикселя (0-255)
        /// </summary>
        private static byte GetBrightness(Bitmap image, int x, int y)
        {
            if (x < 0 || x >= image.Width || y < 0 || y >= image.Height)
                return 0;

            Color pixel = image.GetPixel(x, y);
            return (byte)((pixel.R + pixel.G + pixel.B) / 3);
        }

        /// <summary>
        /// Расчет среднего значения и стандартного отклонения по окрестности пикселя
        /// </summary>
        /// <param name="image">Изображение</param>
        /// <param name="centerX">X проверяемого пикселя</param>
        /// <param name="centerY">Y проверяемого пикселя</param>
        /// <param name="windowSize">Размер окрестности (нечетное число)</param>
        /// <param name="objectSide">Размер стороны объекта для исключения из статистики</param>
        /// <param name="damageMask">Маска поврежденных пикселей</param>
        /// <returns>(среднее, стандартное отклонение)</returns>
        public static (double mean, double stdDev) CalculateStatistics(
            Bitmap image,
            int centerX,
            int centerY,
            int windowSize,
            int objectSide,
            bool[,] damageMask)
        {
            int radius = windowSize / 2;
            int halfObject = objectSide / 2;
            List<double> values = new List<double>();

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Пропускаем всю область предполагаемого объекта
                    // а не только центральный пиксель
                    if (Math.Abs(dx) <= halfObject && Math.Abs(dy) <= halfObject)
                        continue;

                    // Проверка границ изображения
                    if (x < 0 || x >= image.Width || y < 0 || y >= image.Height)
                        continue;

                    // Проверка поврежденных пикселей
                    if (damageMask != null && damageMask[x, y])
                        continue;

                    // Получаем яркость пикселя
                    Color pixel = image.GetPixel(x, y);
                    double brightness = (pixel.R + pixel.G + pixel.B) / 3.0;
                    values.Add(brightness);
                }
            }

            // Если нет ни одного пикселя для анализа
            if (values.Count == 0)
            {
                return (0, 0);
            }

            // Вычисляем среднее
            double sum = 0;
            foreach (double val in values)
                sum += val;
            double mean = sum / values.Count;

            // Вычисляем стандартное отклонение
            double sumSquaredDiff = 0;
            foreach (double val in values)
                sumSquaredDiff += (val - mean) * (val - mean);
            double variance = sumSquaredDiff / values.Count;
            double stdDev = Math.Sqrt(variance);

            return (mean, stdDev);
        }


        /// <summary>
        /// Перебор всех пикселей изображения и применение функции сегментации
        /// </summary>
        /// <param name="image">Изображение</param>
        /// <param name="damageMask">Маска повреждений</param>
        /// <param name="windowSize">Размер окрестности</param>
        /// <param name="objectSide">Размер стороны объекта</param>
        /// <param name="segmentationFunc">Функция сегментации (принимает координаты, μ, σ, возвращает true если объект)</param>
        /// <returns>Бинарная маска (true - объект, false - фон)</returns>
        public static bool[,] IterateAllPixels(
            Bitmap image,
            bool[,] damageMask,
            int windowSize,
            int objectSide,
            Func<int, int, double, double, bool> segmentationFunc)
        {
            int width = image.Width;
            int height = image.Height;
            bool[,] resultMask = new bool[width, height];

            // Проходим по всем пикселям изображения
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Пропускаем поврежденные пиксели
                    if (damageMask != null && damageMask[x, y])
                        continue;

                    // Вычисляем статистику по окрестности с учетом размера объекта
                    var (mean, stdDev) = CalculateStatistics(image, x, y, windowSize, objectSide, damageMask);

                    // Применяем функцию сегментации
                    resultMask[x, y] = segmentationFunc(x, y, mean, stdDev);
                }
            }

            return resultMask;
        }
    }
}