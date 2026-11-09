using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Newton_sMethod
{
  public partial class Form1 : Form
  {
    private TextBox txtA, txtB, txtE, txtFormula;
    private Label lblResult, lblStepInfo;
    private Chart chart1;
    private MenuStrip menuStrip1;
    private ToolStripMenuItem calcMenuItem, clearMenuItem, prevStepMenuItem, nextStepMenuItem;
    private ComboBox cmbType;

    private List<double> stepsX = new List<double>();
    private int currentStepIndex = 0;
    private double a, b, eps;
    private string formula;
    private bool isMin;

    public Form1()
    {
      this.Text = "Лаба №2: Метод Ньютона";
      this.Size = new Size(1100, 700);
      this.StartPosition = FormStartPosition.CenterScreen;
      this.BackColor = Color.FromArgb(240, 240, 240);

      org.mariuszgromada.math.mxparser.License.iConfirmNonCommercialUse("Student");
      InitializeCustomComponents();
    }

    private void InitializeCustomComponents()
    {
      // Меню
      menuStrip1 = new MenuStrip();
      menuStrip1.BackColor = Color.White;
      calcMenuItem = new ToolStripMenuItem("Рассчитать");
      clearMenuItem = new ToolStripMenuItem("Очистить");
      prevStepMenuItem = new ToolStripMenuItem("< Назад");
      nextStepMenuItem = new ToolStripMenuItem("Вперед >");
      prevStepMenuItem.Enabled = false;
      nextStepMenuItem.Enabled = false;

      calcMenuItem.Click += CalcMenuItem_Click;
      clearMenuItem.Click += ClearMenuItem_Click;
      prevStepMenuItem.Click += PrevStepMenuItem_Click;
      nextStepMenuItem.Click += NextStepMenuItem_Click;

      menuStrip1.Items.AddRange(new ToolStripItem[] { calcMenuItem, clearMenuItem, new ToolStripSeparator(), prevStepMenuItem, nextStepMenuItem });
      this.Controls.Add(menuStrip1);
      this.MainMenuStrip = menuStrip1;

      // Левая панель
      Panel leftPanel = new Panel();
      leftPanel.Dock = DockStyle.Left;
      leftPanel.Width = 350;
      leftPanel.BackColor = Color.White;
      leftPanel.Padding = new Padding(15);
      this.Controls.Add(leftPanel);

      Label lblTitle = new Label { Text = "Параметры функции", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true };
      leftPanel.Controls.Add(lblTitle);

      CreateInputPair(leftPanel, "Начало (a):", 50, out txtA, true);
      CreateInputPair(leftPanel, "Конец (b):", 90, out txtB, true);
      CreateInputPair(leftPanel, "Точность (e):", 130, out txtE, true);
      CreateInputPair(leftPanel, "Функция f(x):", 170, out txtFormula, false);

      Label lblType = new Label { Text = "Искать:", Font = new Font("Segoe UI", 10), Location = new Point(10, 215), AutoSize = true };
      leftPanel.Controls.Add(lblType);

      cmbType = new ComboBox { Location = new Point(120, 212), Width = 200, Font = new Font("Segoe UI", 10), DropDownStyle = ComboBoxStyle.DropDownList };
      cmbType.Items.AddRange(new string[] { "Минимум", "Максимум" });
      cmbType.SelectedIndex = 0;
      leftPanel.Controls.Add(cmbType);

      txtA.Text = "-3";
      txtB.Text = "3";
      txtE.Text = "0,001";
      txtFormula.Text = "x^4 - 4*x^2 + x"; // Сложная функция для шагов

      Panel resultPanel = new Panel { Location = new Point(10, 260), Size = new Size(320, 300), BackColor = Color.FromArgb(250, 250, 250), BorderStyle = BorderStyle.FixedSingle };
      leftPanel.Controls.Add(resultPanel);
      lblResult = new Label { Dock = DockStyle.Fill, Text = "Здесь будет результат...", Font = new Font("Consolas", 10), Padding = new Padding(10) };
      resultPanel.Controls.Add(lblResult);

      lblStepInfo = new Label { Location = new Point(10, 570), Size = new Size(320, 30), Text = "", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.DarkBlue };
      leftPanel.Controls.Add(lblStepInfo);

      // График
      chart1 = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
      ChartArea area = new ChartArea("MainArea");
      area.AxisX.MajorGrid.LineColor = Color.LightGray;
      area.AxisY.MajorGrid.LineColor = Color.LightGray;
      area.AxisX.Crossing = 0;
      area.AxisY.Crossing = 0;
      chart1.ChartAreas.Add(area);
      chart1.Legends.Add("Legend").Docking = Docking.Top;
      this.Controls.Add(chart1);
      chart1.BringToFront();

      SetupChartSeries();
    }

    private void CreateInputPair(Panel parent, string labelText, int yPos, out TextBox textBox, bool isNumberOnly)
    {
      Label lbl = new Label { Text = labelText, Font = new Font("Segoe UI", 10), Location = new Point(10, yPos), AutoSize = true };
      parent.Controls.Add(lbl);

      textBox = new TextBox { Location = new Point(120, yPos - 3), Width = 200, Font = new Font("Segoe UI", 10) };
      if (isNumberOnly) textBox.KeyPress += NumberTextBox_KeyPress;
      parent.Controls.Add(textBox);
    }

    private void NumberTextBox_KeyPress(object sender, KeyPressEventArgs e)
    {
      TextBox tb = (TextBox)sender;
      if (char.IsControl(e.KeyChar)) return;
      if (e.KeyChar == ' ') { e.Handled = true; return; }
      if (char.IsDigit(e.KeyChar))
      {
        if (tb.SelectionStart == 0 && e.KeyChar == '0' && tb.Text.Length > 0 && tb.Text[0] != ',' && tb.Text[0] != '.') { e.Handled = true; return; }
        return;
      }
      if (e.KeyChar == ',') { if (tb.Text.Contains(",")) e.Handled = true; return; }
      if (e.KeyChar == '-') { if (tb.SelectionStart != 0 || tb.Text.Contains("-")) e.Handled = true; return; }
      e.Handled = true;
    }

    private void SetupChartSeries()
    {
      chart1.Series.Clear();
      chart1.Series.Add(new Series("F(x)") { ChartType = SeriesChartType.Spline, BorderWidth = 3, Color = Color.FromArgb(50, 100, 200) });
      chart1.Series.Add(new Series("История шагов") { ChartType = SeriesChartType.Point, MarkerSize = 8, MarkerStyle = MarkerStyle.Diamond, Color = Color.LightGray });
      chart1.Series.Add(new Series("Текущий шаг") { ChartType = SeriesChartType.Point, MarkerSize = 14, MarkerStyle = MarkerStyle.Circle, Color = Color.Orange });
      chart1.Series.Add(new Series("Экстремум") { ChartType = SeriesChartType.Point, MarkerSize = 16, MarkerStyle = MarkerStyle.Star5, Color = Color.Red });
    }

    private void CalcMenuItem_Click(object sender, EventArgs e)
    {
      if (txtA.Text.Contains(" ") || txtB.Text.Contains(" ") || txtE.Text.Contains(" ")) { MessageBox.Show("Пробелы недопустимы!"); return; }
      if (!ValidateInputs()) return;

      stepsX.Clear();
      currentStepIndex = 0;
      foreach (var s in chart1.Series) s.Points.Clear();

      a = double.Parse(txtA.Text.Replace('.', ','));
      b = double.Parse(txtB.Text.Replace('.', ','));
      eps = double.Parse(txtE.Text.Replace('.', ','));
      formula = txtFormula.Text;
      isMin = cmbType.SelectedIndex == 0;

      DrawGraph(formula, a, b);
      List<double> bestPath = FindGlobalExtremum();

      if (bestPath != null && bestPath.Count > 0)
      {
        stepsX = bestPath;
        currentStepIndex = 0;
        prevStepMenuItem.Enabled = true;
        nextStepMenuItem.Enabled = true;
        UpdateStepVisualization();
      }
      else lblResult.Text = "Экстремум не найден.";
    }

    private List<double> FindGlobalExtremum()
    {
      double bestX = 0, bestY = isMin ? double.MaxValue : double.MinValue;
      List<double> bestPath = null;
      int startPoints = 20;
      double stepSize = (b - a) / startPoints;

      for (int i = 0; i <= startPoints; i++)
      {
        double startX = a + i * stepSize;
        if (startX < a || startX > b) continue;

        List<double> path = new List<double>();
        double x = startX;
        int iter = 0;

        while (iter < 100)
        {
          double y = EvaluateFunction(formula, x);
          double dy = EvaluateDerivative(formula, x);
          double ddy = EvaluateSecondDerivative(formula, x);
          path.Add(x);

          if (double.IsNaN(y) || double.IsInfinity(y) || Math.Abs(ddy) < 1e-6) break;
          double nextX = x - dy / ddy;
          if (nextX < a || nextX > b) break;
          if (Math.Abs(nextX - x) < eps) break;
          x = nextX;
          iter++;
        }

        double yFinal = EvaluateFunction(formula, x);
        if (!double.IsNaN(yFinal) && !double.IsInfinity(yFinal))
        {
          if ((isMin && yFinal < bestY) || (!isMin && yFinal > bestY))
          {
            bestY = yFinal; bestX = x; bestPath = new List<double>(path);
          }
        }
      }
      if (bestPath != null && !bestPath.Contains(bestX)) bestPath.Add(bestX);
      if (bestPath != null && bestPath.Count > 0) chart1.Series["Экстремум"].Points.AddXY(bestX, bestY);
      return bestPath;
    }

    private void UpdateStepVisualization()
    {
      if (stepsX.Count == 0) return;
      chart1.Series["История шагов"].Points.Clear();
      chart1.Series["Текущий шаг"].Points.Clear();

      for (int i = 0; i < stepsX.Count; i++)
      {
        double x = stepsX[i];
        double y = EvaluateFunction(formula, x);
        if (i < currentStepIndex) chart1.Series["История шагов"].Points.AddXY(x, y);
        else if (i == currentStepIndex) chart1.Series["Текущий шаг"].Points.AddXY(x, y);
      }

      double currentX = stepsX[currentStepIndex];
      double currentY = EvaluateFunction(formula, currentX);
      lblResult.Text = $"Ищем: {(isMin ? "Минимум" : "Максимум")}\n--- Текущий шаг ---\nИтерация: {currentStepIndex}\nx = {Math.Round(currentX, 5)}\nf(x) = {Math.Round(currentY, 5)}\n";
      if (currentStepIndex == stepsX.Count - 1) lblResult.Text += "\n[ДОСТИГНУТ ЭКСТРЕМУМ]";
      lblStepInfo.Text = $"Шаг {currentStepIndex + 1} из {stepsX.Count}";
    }

    private void PrevStepMenuItem_Click(object sender, EventArgs e) { if (currentStepIndex > 0) { currentStepIndex--; UpdateStepVisualization(); } }
    private void NextStepMenuItem_Click(object sender, EventArgs e) { if (currentStepIndex < stepsX.Count - 1) { currentStepIndex++; UpdateStepVisualization(); } }

    private void ClearMenuItem_Click(object sender, EventArgs e)
    {
      txtA.Text = "-3"; txtB.Text = "3"; txtE.Text = "0,001"; txtFormula.Text = "x^4 - 4*x^2 + x";
      lblResult.Text = "Здесь будет результат..."; lblStepInfo.Text = ""; stepsX.Clear(); currentStepIndex = 0;
      prevStepMenuItem.Enabled = false; nextStepMenuItem.Enabled = false;
      foreach (var s in chart1.Series) s.Points.Clear();
    }

    private double EvaluateFunction(string formula, double xValue)
    {
      var x = new org.mariuszgromada.math.mxparser.Argument("x", xValue);
      var e = new org.mariuszgromada.math.mxparser.Expression(formula, x);
      return e.calculate();
    }

    private double EvaluateDerivative(string formula, double x) => (EvaluateFunction(formula, x + 1e-5) - EvaluateFunction(formula, x - 1e-5)) / (2 * 1e-5);
    private double EvaluateSecondDerivative(string formula, double x) => (EvaluateFunction(formula, x + 1e-4) - 2 * EvaluateFunction(formula, x) + EvaluateFunction(formula, x - 1e-4)) / (1e-4 * 1e-4);

    private bool ValidateInputs()
    {
      if (!double.TryParse(txtA.Text.Replace('.', ','), out double _) || !double.TryParse(txtB.Text.Replace('.', ','), out double _) || !double.TryParse(txtE.Text.Replace('.', ','), out double _))
      { MessageBox.Show("Введите корректные числа!"); return false; }
      if (double.Parse(txtA.Text.Replace('.', ',')) >= double.Parse(txtB.Text.Replace('.', ','))) { MessageBox.Show("'a' должно быть меньше 'b'!"); return false; }
      if (double.Parse(txtE.Text.Replace('.', ',')) <= 0) { MessageBox.Show("Точность 'e' должна быть > 0!"); return false; }
      return true;
    }

    private void DrawGraph(string formula, double a, double b)
    {
      chart1.Series["F(x)"].Points.Clear();
      double step = (b - a) / 300.0;
      for (double x = a; x <= b; x += step)
      {
        double y = EvaluateFunction(formula, x);
        if (!double.IsNaN(y) && !double.IsInfinity(y) && Math.Abs(y) < 10000) chart1.Series["F(x)"].Points.AddXY(x, y);
      }
      chart1.ChartAreas[0].RecalculateAxesScale();
    }
  }
}