using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace PointObjectDetection.Core
{
    /// <summary>
    /// Класс для перебора пикселей и расчета статистических характеристик
    /// Разработчик 2 (Данил): Отвечает за пункты: перебор пикселей, оценка μ и σ
    /// </summary>
    public static class StatisticsCalculator
    {
        /// <summary>
        /// Расчет среднего значения и стандартного отклонения по окрестности пикселя
        /// </summary>
        public static unsafe (double mean, double stdDev) CalculateStatistics(
            Bitmap image, int centerX, int centerY, int windowSize,
            int objectSide, bool[,] damageMask)
        {
            int width = image.Width;
            int height = image.Height;
            int radius = windowSize / 2;
            List<double> values = new List<double>(windowSize * windowSize);

            // Блокируем битмап в памяти
            BitmapData bmpData = image.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                int stride = bmpData.Stride;

                for (int dy = -radius; dy <= radius; dy++)
                {
                    int y = centerY + dy;
                    if (y < 0 || y >= height) continue;

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        // Исключаем центральный пиксель
                        if (dx == 0 && dy == 0) continue;

                        int x = centerX + dx;
                        if (x < 0 || x >= width) continue;

                        // Проверка маски повреждений
                        if (damageMask != null && damageMask[x, y]) continue;

                        // Прямой доступ к байтам (BGR порядок)
                        byte* pixel = ptr + y * stride + x * 3;
                        double brightness = (pixel[2] + pixel[1] + pixel[0]) / 3.0;
                        values.Add(brightness);
                    }
                }
            }
            finally
            {
                image.UnlockBits(bmpData);
            }

            // Расчет статистики
            if (values.Count == 0) return (0, 0);

            double sum = 0;
            foreach (double val in values) sum += val;
            double mean = sum / values.Count;

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

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (damageMask != null && damageMask[x, y])
                        continue;

                    var (mean, stdDev) = CalculateStatistics(image, x, y, windowSize, objectSide, damageMask);
                    resultMask[x, y] = segmentationFunc(x, y, mean, stdDev);
                }
            }

            return resultMask;
        }
    }
}