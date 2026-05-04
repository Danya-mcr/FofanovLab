using System;
using System.Collections.Generic;

namespace PointObjectDetection.Core
{
    /// <summary>
    /// Структура для хранения координат пикселя
    /// </summary>
    /// Арина
    public struct PixelPoint
    {
        public int X;
        public int Y;

        public PixelPoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Структура для хранения границ объекта
    /// </summary>
    /// <summary>
    /// Класс для хранения границ объекта (class вместо struct)
    /// </summary>
    public class ObjectBounds
    {
        public int MinX;
        public int MinY;
        public int MaxX;
        public int MaxY;

        public int Width => MaxX - MinX + 1;
        public int Height => MaxY - MinY + 1;

        public ObjectBounds(int x, int y)
        {
            MinX = x;
            MinY = y;
            MaxX = x;
            MaxY = y;
        }

        public void Update(int x, int y)
        {
            if (x < MinX) MinX = x;
            if (x > MaxX) MaxX = x;
            if (y < MinY) MinY = y;
            if (y > MaxY) MaxY = y;
        }
    }

    /// <summary>
    /// Класс обнаруженного объекта
    /// </summary>
    public class DetectedObject
    {
        public int Id { get; set; }
        public int PixelCount { get; set; }
        public ObjectBounds Bounds { get; set; }
        public List<PixelPoint> Pixels { get; set; }

        public DetectedObject(int id, int x, int y)
        {
            Id = id;
            PixelCount = 1;
            Bounds = new ObjectBounds(x, y);
            Pixels = new List<PixelPoint> { new PixelPoint(x, y) };
        }
    }

    /// <summary>
    /// Результат обнаружения
    /// </summary>
    public class DetectionResult
    {
        public bool ObjectFound { get; set; }
        public int ObjectsCount { get; set; }
        public List<DetectedObject> Objects { get; set; }
        public string Report { get; set; }

        public DetectionResult()
        {
            Objects = new List<DetectedObject>();
            Report = string.Empty;
        }
    }

    /// <summary>
    /// Класс для маркировки связных компонент, отбраковки и принятия решения
    /// Разработчик 3: Отвечает за пункты: маркировка, отбраковка по площади, принятие решения
    /// </summary>
    public static class ObjectDetector
    {
        /// <summary>
        /// Маркировка связных компонент (4-связность)
        /// Объединяет соседние пиксели-объекты в группы
        /// </summary>
        /// <param name="mask">Бинарная маска (true - объект, false - фон)</param>
        /// <param name="damageMask">Маска повреждений (true - поврежден, false - целый)</param>
        /// <returns>Список обнаруженных объектов</returns>
        /// <summary>
        /// Маркировка связных компонент (4-связность)
        /// </summary>
        public static List<DetectedObject> LabelConnectedComponents(bool[,] mask, bool[,] damageMask)
        {
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);
            int[,] labels = new int[width, height];
            List<DetectedObject> objects = new List<DetectedObject>();
            int nextId = 1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!mask[x, y]) continue;
                    if (damageMask != null && damageMask[x, y]) continue;

                    int leftLabel = (x > 0 && (damageMask == null || !damageMask[x - 1, y])) ? labels[x - 1, y] : 0;
                    int upLabel = (y > 0 && (damageMask == null || !damageMask[x, y - 1])) ? labels[x, y - 1] : 0;

                    if (leftLabel == 0 && upLabel == 0)
                    {
                        // Новый объект
                        labels[x, y] = nextId;
                        objects.Add(new DetectedObject(nextId, x, y));
                        nextId++;
                    }
                    else if (leftLabel != 0 && upLabel == 0)
                    {
                        // Присоединяем к левому
                        labels[x, y] = leftLabel;
                        DetectedObject obj = objects[leftLabel - 1];
                        obj.PixelCount++;
                        obj.Pixels.Add(new PixelPoint(x, y));
                        // ВАЖНО: обновляем границы правильно
                        if (x < obj.Bounds.MinX) obj.Bounds.MinX = x;
                        if (x > obj.Bounds.MaxX) obj.Bounds.MaxX = x;
                        if (y < obj.Bounds.MinY) obj.Bounds.MinY = y;
                        if (y > obj.Bounds.MaxY) obj.Bounds.MaxY = y;
                    }
                    else if (leftLabel == 0 && upLabel != 0)
                    {
                        // Присоединяем к верхнему
                        labels[x, y] = upLabel;
                        DetectedObject obj = objects[upLabel - 1];
                        obj.PixelCount++;
                        obj.Pixels.Add(new PixelPoint(x, y));
                        // ВАЖНО: обновляем границы правильно
                        if (x < obj.Bounds.MinX) obj.Bounds.MinX = x;
                        if (x > obj.Bounds.MaxX) obj.Bounds.MaxX = x;
                        if (y < obj.Bounds.MinY) obj.Bounds.MinY = y;
                        if (y > obj.Bounds.MaxY) obj.Bounds.MaxY = y;
                    }
                    else
                    {
                        // Объединение двух компонент
                        int minLabel = Math.Min(leftLabel, upLabel);
                        int maxLabel = Math.Max(leftLabel, upLabel);
                        labels[x, y] = minLabel;

                        if (minLabel != maxLabel)
                        {
                            DetectedObject objMin = objects[minLabel - 1];
                            DetectedObject objMax = objects[maxLabel - 1];

                            // Перемещаем пиксели
                            objMin.PixelCount += objMax.PixelCount;
                            objMin.Pixels.AddRange(objMax.Pixels);

                            // Обновляем границы - берем минимум и максимум из обоих
                            objMin.Bounds.MinX = Math.Min(objMin.Bounds.MinX, objMax.Bounds.MinX);
                            objMin.Bounds.MinY = Math.Min(objMin.Bounds.MinY, objMax.Bounds.MinY);
                            objMin.Bounds.MaxX = Math.Max(objMin.Bounds.MaxX, objMax.Bounds.MaxX);
                            objMin.Bounds.MaxY = Math.Max(objMin.Bounds.MaxY, objMax.Bounds.MaxY);

                            objects[maxLabel - 1] = null;

                            // Переназначаем метки
                            for (int i = 0; i < height; i++)
                            {
                                for (int j = 0; j < width; j++)
                                {
                                    if (labels[j, i] == maxLabel)
                                        labels[j, i] = minLabel;
                                }
                            }
                        }

                        // Добавляем текущий пиксель
                        DetectedObject objCurrent = objects[minLabel - 1];
                        objCurrent.PixelCount++;
                        objCurrent.Pixels.Add(new PixelPoint(x, y));
                        // ВАЖНО: обновляем границы
                        if (x < objCurrent.Bounds.MinX) objCurrent.Bounds.MinX = x;
                        if (x > objCurrent.Bounds.MaxX) objCurrent.Bounds.MaxX = x;
                        if (y < objCurrent.Bounds.MinY) objCurrent.Bounds.MinY = y;
                        if (y > objCurrent.Bounds.MaxY) objCurrent.Bounds.MaxY = y;
                    }
                }
            }

            // Удаляем null-объекты
            List<DetectedObject> result = new List<DetectedObject>();
            foreach (var obj in objects)
            {
                if (obj != null)
                    result.Add(obj);
            }

            return result;
        }

        /// <summary>
        /// Отбраковка объектов по минимальной площади
        /// </summary>
        /// <param name="objects">Список объектов</param>
        /// <param name="minArea">Минимальная площадь (количество пикселей)</param>
        /// <returns>Отфильтрованный список объектов</returns>
        public static List<DetectedObject> FilterByArea(List<DetectedObject> objects, int minArea)
        {
            List<DetectedObject> result = new List<DetectedObject>();

            foreach (var obj in objects)
            {
                if (obj.PixelCount >= minArea)
                    result.Add(obj);
            }

            return result;
        }

        /// <summary>
        /// Формирование отчета и принятие решения
        /// </summary>
        /// <param name="objects">Обнаруженные объекты</param>
        /// <param name="windowSize">Размер окрестности</param>
        /// <param name="falseAlarmProb">Вероятность ложного обнаружения</param>
        /// <param name="minArea">Минимальная площадь объекта</param>
        /// <param name="totalPixels">Общее количество пикселей</param>
        /// <returns>Результат обнаружения с отчетом</returns>
        public static DetectionResult MakeDecision(
            List<DetectedObject> objects,
            int windowSize,
            double falseAlarmProb,
            int minArea,
            int totalPixels)
        {
            DetectionResult result = new DetectionResult();
            result.ObjectFound = objects.Count > 0;
            result.ObjectsCount = objects.Count;
            result.Objects = objects;

            // Формируем отчет
            result.Report = "=== РЕЗУЛЬТАТ ОБНАРУЖЕНИЯ ===\n\n";
            result.Report += $"Параметры:\n";
            result.Report += $"  Размер окрестности: {windowSize}x{windowSize}\n";
            result.Report += $"  Вероятность ложного обнаружения p = {falseAlarmProb:E}\n";
            result.Report += $"  Минимальная площадь объекта: {minArea} пикселей\n";
            result.Report += $"  Всего пикселей на изображении: {totalPixels}\n\n";
            result.Report += $"Обнаружено объектов: {objects.Count}\n\n";

            if (objects.Count > 0)
            {
                result.Report += "Список обнаруженных объектов:\n";
                for (int i = 0; i < objects.Count; i++)
                {
                    var obj = objects[i];
                    result.Report += $"  {i + 1}. Площадь: {obj.PixelCount} пикселей, ";
                    result.Report += $"границы: ({obj.Bounds.MinX}, {obj.Bounds.MinY}) - ";
                    result.Report += $"({obj.Bounds.MaxX}, {obj.Bounds.MaxY})\n";
                }
                result.Report += "\nРЕШЕНИЕ: ОБЪЕКТ ОБНАРУЖЕН\n";
            }
            else 
            {
                result.Report += "РЕШЕНИЕ: ОБЪЕКТ НЕ ОБНАРУЖЕН\n";
            }

            return result;
        }
    }
}