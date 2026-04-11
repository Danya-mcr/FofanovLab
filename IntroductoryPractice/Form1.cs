using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using PointObjectDetection.Core;

namespace PointObjectDetection.UI
{
    public partial class Form1 : Form
    {
        // Данные
        private Bitmap _originalImage;
        private Bitmap _resultImage;
        private bool[,] _damageMask;
        private string _currentImagePath;

        // UI элементы
        private MenuStrip _menuStrip;
        private ToolStrip _toolStrip;
        private PictureBox _pictureBox;
        private Panel _rightPanel;
        private GroupBox _paramsBox;
        private GroupBox _statsBox;
        private GroupBox _actionsBox;
        private GroupBox _resultsBox;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;

        private NumericUpDown _nudWindowSize;
        private NumericUpDown _nudFalseAlarmProb;
        private NumericUpDown _nudObjectSide;
        private Button _btnDamageMask;
        private Button _btnDetect;
        private Button _btnSave;
        private Label _lblMean;
        private Label _lblStdDev;
        private Label _lblLower;
        private Label _lblUpper;
        private TextBox _txtResults;

        // Текущие значения статистики (для отображения)
        private double _currentMean;
        private double _currentStdDev;
        private double _currentLower;
        private double _currentUpper;

        public Form1()
        {
            CreateUI();
        }

        private void CreateUI()
        {
            this.Text = "Обнаружение точечных объектов";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = SystemColors.Control;

            // ========== MENU ==========
            _menuStrip = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("Файл");
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("Открыть", null, (s, e) => OpenImage()));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("Выход", null, (s, e) => Application.Exit()));
            _menuStrip.Items.Add(fileMenu);
            this.MainMenuStrip = _menuStrip;

            // ========== TOOLSTRIP ==========
            _toolStrip = new ToolStrip();
            _toolStrip.Items.Add(new ToolStripButton("Открыть", null, (s, e) => OpenImage()) { ToolTipText = "Открыть изображение (Ctrl+O)" });
            _toolStrip.Items.Add(new ToolStripButton("Обнаружить", null, (s, e) => DetectObjects()) { ToolTipText = "Запустить обнаружение (Ctrl+D)" });
            _toolStrip.Items.Add(new ToolStripButton("Сохранить", null, (s, e) => SaveResult()) { ToolTipText = "Сохранить результат (Ctrl+S)" });

            // ========== PICTUREBOX ==========
            _pictureBox = new PictureBox()
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(40, 40, 40)
            };
            _pictureBox.MouseMove += PictureBox_MouseMove;

            // ========== RIGHT PANEL ==========
            _rightPanel = new Panel()
            {
                Dock = DockStyle.Right,
                Width = 350,
                Padding = new Padding(10)
            };

            // ---- Параметры ----
            _paramsBox = new GroupBox()
            {
                Text = "Параметры обнаружения",
                Dock = DockStyle.Top,
                Height = 200,
                Padding = new Padding(5)
            };

            int yPos = 25;
            int yStep = 35;

            Label lblWindow = new Label() { Text = "Сторона окрестности (нечетное):", Location = new Point(10, yPos), AutoSize = true };
            _nudWindowSize = new NumericUpDown() { Location = new Point(200, yPos - 3), Width = 80, Minimum = 3, Maximum = 31, Value = 3, Increment = 2 };
            yPos += yStep;

            Label lblProb = new Label() { Text = "Вероятность p (ложного обнар.):", Location = new Point(10, yPos), AutoSize = true };
            _nudFalseAlarmProb = new NumericUpDown() { Location = new Point(200, yPos - 3), Width = 120, Minimum = 0.0000001m, Maximum = 0.1m, DecimalPlaces = 7, Increment = 0.0001m, Value = 0.0005m };
            yPos += yStep;

            Label lblObject = new Label() { Text = "Сторона квадрата объекта:", Location = new Point(10, yPos), AutoSize = true };
            _nudObjectSide = new NumericUpDown() { Location = new Point(200, yPos - 3), Width = 80, Minimum = 1, Maximum = 31, Value = 3 };
            yPos += yStep;

            _btnDamageMask = new Button() { Text = "Редактировать маску повреждений", Location = new Point(10, yPos), Width = 310, Height = 30 };
            _btnDamageMask.Click += (s, e) => EditDamageMask();

            _paramsBox.Controls.AddRange(new Control[] { lblWindow, _nudWindowSize, lblProb, _nudFalseAlarmProb, lblObject, _nudObjectSide, _btnDamageMask });

            // ---- Статистика ----
            _statsBox = new GroupBox()
            {
                Text = "Статистика по окрестности",
                Dock = DockStyle.Top,
                Height = 120,
                Padding = new Padding(5)
            };

            yPos = 25;
            _lblMean = new Label() { Text = "Среднее (μ): —", Location = new Point(10, yPos), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            yPos += 25;
            _lblStdDev = new Label() { Text = "СКО (σ): —", Location = new Point(10, yPos), AutoSize = true };
            yPos += 25;
            _lblLower = new Label() { Text = "Нижняя граница: —", Location = new Point(10, yPos), AutoSize = true };
            yPos += 25;
            _lblUpper = new Label() { Text = "Верхняя граница: —", Location = new Point(10, yPos), AutoSize = true };

            _statsBox.Controls.AddRange(new Control[] { _lblMean, _lblStdDev, _lblLower, _lblUpper });

            // ---- Действия ----
            _actionsBox = new GroupBox()
            {
                Text = "Действия",
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(5)
            };

            _btnDetect = new Button() { Text = "ЗАПУСК ОБНАРУЖЕНИЯ", Location = new Point(10, 20), Width = 150, Height = 35, BackColor = Color.LightGreen };
            _btnDetect.Click += (s, e) => DetectObjects();

            _btnSave = new Button() { Text = "СОХРАНИТЬ РЕЗУЛЬТАТ", Location = new Point(170, 20), Width = 150, Height = 35 };
            _btnSave.Click += (s, e) => SaveResult();

            _actionsBox.Controls.AddRange(new Control[] { _btnDetect, _btnSave });

            // ---- Результаты ----
            _resultsBox = new GroupBox()
            {
                Text = "Результаты",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            _txtResults = new TextBox()
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9)
            };

            _resultsBox.Controls.Add(_txtResults);

            // Собираем правую панель
            _rightPanel.Controls.Add(_resultsBox);
            _rightPanel.Controls.Add(_actionsBox);
            _rightPanel.Controls.Add(_statsBox);
            _rightPanel.Controls.Add(_paramsBox);

            // ========== STATUS STRIP ==========
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Готов к работе. Откройте изображение.");
            _statusStrip.Items.Add(_statusLabel);

            // ========== СБОРКА ==========
            this.Controls.Add(_pictureBox);
            this.Controls.Add(_rightPanel);
            this.Controls.Add(_statusStrip);
            this.Controls.Add(_toolStrip);
            this.Controls.Add(_menuStrip);

            // Горячие клавиши
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.O) OpenImage();
                if (e.Control && e.KeyCode == Keys.D) DetectObjects();
                if (e.Control && e.KeyCode == Keys.S) SaveResult();
            };
        }

        /// <summary>
        /// Открытие изображения
        /// </summary>
        private void OpenImage()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Изображения|*.bmp;*.jpg;*.jpeg;*.png;*.tiff";
                ofd.Title = "Выберите изображение";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string error;
                    Bitmap loadedImage = ImageProcessor.LoadImage(ofd.FileName, out error);

                    if (loadedImage == null)
                    {
                        MessageBox.Show(error, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // КОНВЕРТАЦИЯ В 24-БИТНОЕ ИЗОБРАЖЕНИЕ
                    _originalImage = new Bitmap(loadedImage.Width, loadedImage.Height, PixelFormat.Format24bppRgb);
                    using (Graphics g = Graphics.FromImage(_originalImage))
                    {
                        g.DrawImage(loadedImage, 0, 0, loadedImage.Width, loadedImage.Height);
                    }
                    loadedImage.Dispose();

                    _currentImagePath = ofd.FileName;
                    _damageMask = ImageProcessor.CreateDamageMask(_originalImage.Width, _originalImage.Height);
                    _pictureBox.Image = _originalImage;
                    _txtResults.Clear();

                    UpdateStatus("Изображение загружено. Размер: {0}x{1}", _originalImage.Width, _originalImage.Height);
                }
            }
        }

        /// <summary>
        /// Редактирование маски повреждений
        /// </summary>
        private void EditDamageMask()
        {
            if (_originalImage == null)
            {
                MessageBox.Show("Сначала откройте изображение", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Form maskForm = new Form();
            maskForm.Text = "Редактор маски повреждений (клик по пикселю)";
            maskForm.Size = new Size(800, 600);
            maskForm.WindowState = FormWindowState.Maximized;

            PictureBox pb = new PictureBox();
            pb.Dock = DockStyle.Fill;
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.Image = new Bitmap(_originalImage);

            pb.MouseClick += (s, e) =>
            {
                float scaleX = (float)_originalImage.Width / pb.Width;
                float scaleY = (float)_originalImage.Height / pb.Height;
                int x = (int)(e.X * scaleX);
                int y = (int)(e.Y * scaleY);

                if (x >= 0 && x < _originalImage.Width && y >= 0 && y < _originalImage.Height)
                {
                    _damageMask[x, y] = !_damageMask[x, y];

                    Bitmap display = new Bitmap(_originalImage);
                    for (int ix = 0; ix < _originalImage.Width; ix++)
                    {
                        for (int iy = 0; iy < _originalImage.Height; iy++)
                        {
                            if (_damageMask[ix, iy])
                                display.SetPixel(ix, iy, Color.Red);
                        }
                    }
                    pb.Image = display;
                }
            };

            // Инициализация отображения
            Bitmap initDisplay = new Bitmap(_originalImage);
            for (int ix = 0; ix < _originalImage.Width; ix++)
            {
                for (int iy = 0; iy < _originalImage.Height; iy++)
                {
                    if (_damageMask[ix, iy])
                        initDisplay.SetPixel(ix, iy, Color.Red);
                }
            }
            pb.Image = initDisplay;

            maskForm.Controls.Add(pb);
            maskForm.ShowDialog();

            _pictureBox.Image = _originalImage;
        }

        /// <summary>
        /// Запуск обнаружения объектов
        /// </summary>
        private void DetectObjects()
        {
            if (_originalImage == null)
            {
                MessageBox.Show("Сначала откройте изображение", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                UpdateStatus("Выполняется обнаружение...");

                int windowSize = (int)_nudWindowSize.Value;
                double falseAlarmProb = (double)_nudFalseAlarmProb.Value;
                int objectSide = (int)_nudObjectSide.Value;
                int minArea = objectSide * objectSide;

                // Функция сегментации (Разработчик 4)
                Func<int, int, double, double, bool> segmentationFunc = (x, y, mean, stdDev) =>
                {
                    var (lower, upper) = ThresholdCalculator.ComputeBounds(mean, stdDev, falseAlarmProb);
                    byte brightness = ImageProcessor.GetPixelBrightness(_originalImage, x, y);

                    // Обновляем отображение статистики для последнего пикселя
                    _currentMean = mean;
                    _currentStdDev = stdDev;
                    _currentLower = lower;
                    _currentUpper = upper;

                    UpdateStatsDisplay();

                    return ThresholdCalculator.SegmentPixel(brightness, lower, upper);
                };

                // Перебор пикселей (Разработчик 2)
                bool[,] objectMask = StatisticsCalculator.IterateAllPixels(
                    _originalImage,
                    _damageMask,
                    windowSize,
                    segmentationFunc);

                // Маркировка связных компонент (Разработчик 3)
                var allObjects = ObjectDetector.LabelConnectedComponents(objectMask, _damageMask);

                // Отбраковка по площади (Разработчик 3)
                var filteredObjects = ObjectDetector.FilterByArea(allObjects, minArea);

                // Принятие решения и формирование отчета (Разработчик 3)
                int totalPixels = _originalImage.Width * _originalImage.Height;
                var result = ObjectDetector.MakeDecision(filteredObjects, windowSize, falseAlarmProb, minArea, totalPixels);

                // Отображение результатов
                _txtResults.Text = result.Report;

                // Визуализация
                _resultImage = VisualizeResults(filteredObjects);
                _pictureBox.Image = _resultImage;

                UpdateStatus("Обнаружение завершено. Найдено объектов: {0}", result.ObjectsCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обнаружении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Ошибка при обнаружении");
            }
        }

        /// <summary>
        /// Визуализация обнаруженных объектов - только четкая рамка
        /// </summary>
        private Bitmap VisualizeResults(List<DetectedObject> objects)
        {
            Bitmap result = new Bitmap(_originalImage);
            using (Graphics g = Graphics.FromImage(result))
            {
                // Используем предопределенные яркие цвета
                Color[] colors = new Color[]
                {
                    Color.Red,
                    Color.Lime,
                    Color.Cyan,
                    Color.Yellow,
                    Color.Magenta,
                    Color.Orange,
                    Color.HotPink,
                    Color.LightBlue
                };

                int colorIndex = 0;
                int padding = 10; // Отступ от границ объекта (рамка будет снаружи)

                foreach (var obj in objects)
                {
                    Color color = colors[colorIndex % colors.Length];
                    colorIndex++;

                    using (Pen pen = new Pen(color, 2))
                    {
                        // Рисуем прямоугольник С ОТСТУПОМ от границ объекта
                        // Чтобы рамка была вокруг объекта, а не поверх него
                        g.DrawRectangle(pen,
                            obj.Bounds.MinX - padding,
                            obj.Bounds.MinY - padding,
                            obj.Bounds.Width + padding * 2,
                            obj.Bounds.Height + padding * 2);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Сохранение результатов
        /// </summary>
        private void SaveResult()
        {
            if (_resultImage == null && string.IsNullOrEmpty(_txtResults.Text))
            {
                MessageBox.Show("Нет результатов для сохранения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Текстовый отчет (*.txt)|*.txt|Изображение с разметкой (*.png)|*.png";
                sfd.FileName = "detection_result";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string error;
                    bool success;

                    if (sfd.FileName.EndsWith(".txt"))
                    {
                        success = ImageProcessor.SaveResult(sfd.FileName, _txtResults.Text, null, out error);
                    }
                    else
                    {
                        success = ImageProcessor.SaveResult(sfd.FileName, "", _resultImage, out error);
                    }

                    if (success)
                    {
                        MessageBox.Show("Результат сохранен успешно!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        UpdateStatus("Результат сохранен: {0}", System.IO.Path.GetFileName(sfd.FileName));
                    }
                    else
                    {
                        MessageBox.Show(error, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Обновление отображения статистики в UI
        /// </summary>
        private void UpdateStatsDisplay()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateStatsDisplay));
                return;
            }

            _lblMean.Text = $"Среднее (μ): {_currentMean:F2}";
            _lblStdDev.Text = $"СКО (σ): {_currentStdDev:F2}";
            _lblLower.Text = $"Нижняя граница: {_currentLower:F2}";
            _lblUpper.Text = $"Верхняя граница: {_currentUpper:F2}";
        }

        /// <summary>
        /// Обновление строки состояния
        /// </summary>
        private void UpdateStatus(string format, params object[] args)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, object[]>(UpdateStatus), format, args);
                return;
            }

            _statusLabel.Text = string.Format(format, args);
        }

        /// <summary>
        /// Отображение информации о пикселе под курсором
        /// </summary>
        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_originalImage == null)
            {
                _statusLabel.Text = "Готов к работе. Откройте изображение.";
                return;
            }

            float scaleX = (float)_originalImage.Width / _pictureBox.Width;
            float scaleY = (float)_originalImage.Height / _pictureBox.Height;
            int x = (int)(e.X * scaleX);
            int y = (int)(e.Y * scaleY);

            if (x >= 0 && x < _originalImage.Width && y >= 0 && y < _originalImage.Height)
            {
                byte brightness = ImageProcessor.GetPixelBrightness(_originalImage, x, y);
                bool isDamaged = _damageMask != null && _damageMask[x, y];
                string damagedText = isDamaged ? " (ПОВРЕЖДЕН)" : "";

                _statusLabel.Text = $"X: {x,4}  Y: {y,4}  Яркость: {brightness,3}{damagedText}";
            }
        }
    }
}