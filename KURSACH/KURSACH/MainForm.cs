using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading;

namespace KURSACH
{
    public partial class MainForm : Form
    {
        private DBManager dbManager;
        private ViewAndMiskManager viewManager;
        private Lvl_1 lvl1;
        private Lvl_2 lvl2;
        private Lvl_3 lvl3;

        public MainForm()
        {
            InitializeComponent();
            pictureBox1.Enabled = false;
            dbManager = new DBManager(dataGridView1, textBox1, pictureBox1, textBox2);
            viewManager = new ViewAndMiskManager();

            lvl1 = new Lvl_1();
            lvl2 = new Lvl_2();
            lvl3 = new Lvl_3();
            listBox6.AllowDrop = true;


            checkBox7.Enabled = false;
            checkBox8.Enabled = false;
            checkBox9.Enabled = false;
            tabControl1.Enabled = false;
            
        }

        ///
        ///События
        ///

        //Программный код 7
        private void dataGridView1_DoubleClick(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                int rowIndex = dataGridView1.SelectedRows[0].Index;
                if (rowIndex != dataGridView1.Rows.Count)
                {
                    dataGridView1.Rows.RemoveAt(rowIndex);
                }
            }
        }
        //Программный код 8
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(dbManager.conn.ConnectionString))
                {
                    connection.Open();
                    using (SQLiteTransaction transaction = connection.BeginTransaction())
                    {
                        SQLiteCommand deleteCommand = new SQLiteCommand("DELETE FROM Данные", connection);
                        deleteCommand.ExecuteNonQuery();

                        foreach (DataGridViewRow row in dataGridView1.Rows)
                        {
                            // Проверка, является ли строка новой строкой для вставки
                            if (!row.IsNewRow)
                            {
                                // Создание команды для вставки данных
                                SQLiteCommand insertCommand = new SQLiteCommand();
                                insertCommand.Connection = connection;
                                insertCommand.Transaction = transaction;

                                // Формирование SQL-запроса для вставки данных
                                string columns = "";
                                string values = "";

                                foreach (DataGridViewColumn column in dataGridView1.Columns)
                                {
                                    string columnName = column.Name;
                                    string columnValue = row.Cells[column.Index].Value.ToString();

                                    // Замена запятых на точки, если значение является числом
                                    if (IsNumericValue(columnValue))
                                    {
                                        columnValue = columnValue.Replace(",", CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator);
                                    }

                                    columns += "[" + columnName + "]" + ",";
                                    values += "@" + columnName + ",";

                                    // Добавляем параметры в команду
                                    insertCommand.Parameters.AddWithValue("@" + columnName, columnValue);
                                }

                                // Удаляем последние запятые
                                columns = columns.TrimEnd(',');
                                values = values.TrimEnd(',');

                                // Составляем SQL-запрос
                                insertCommand.CommandText = "INSERT INTO Данные (" + columns + ") VALUES (" + values + ")";

                                // Выполнение команды вставки данных
                                insertCommand.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                }

                MessageBox.Show("Данные успешно сохранены в базу данных.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении данных в базу данных: " + ex.Message);
            }
        }

        private bool IsNumericValue(string value)
        {
            double number;
            return double.TryParse(value, out number);
        }

        private void button5_Click(object sender, EventArgs e)//кнопка с рекомендациями. 
        {
            Form resultForm = new Form();
            resultForm.Text = "Рекомендации";
            resultForm.StartPosition = FormStartPosition.CenterScreen;

            DataTable resultTable = dataGridView2.DataSource as DataTable;
            DataTable filteredTable = resultTable.Clone();

            foreach (DataRow row in resultTable.Rows)
            {
                string состояние = row["Состояние"].ToString();
                if (состояние == "Аварийное")
                {
                    filteredTable.ImportRow(row);
                }
            }

            DataGridView resultGridView = new DataGridView();
            resultGridView.DataSource = filteredTable;
            resultGridView.Dock = DockStyle.Bottom;
            resultGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            Label label = new Label();
            if (filteredTable.Rows.Count != 0)
            {
                label.Text = "На следующих эпохах возникли проблемы.";
                label.Dock = DockStyle.Top;
                label.Font = new Font(label.Font, FontStyle.Bold);
                label.TextAlign = ContentAlignment.MiddleCenter;
                resultForm.Controls.Add(label);

                // Отображение формы
                resultForm.Size = new Size(800, 400);
                resultForm.Controls.Add(resultGridView);
                resultForm.ShowDialog();
            }
            else
            {
                MessageBox.Show($"Добавьте больше эпох, пока состояние стабильно.");
            }
        }
        //Программный код 3
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedTable = comboBox3.SelectedItem.ToString();
            dbManager.table.Clear();
            viewManager.ShowTable(dataGridView1, dbManager.GetAllDataFromTable(selectedTable));
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemCheckState(i, CheckState.Unchecked);
            }
        }
        //Программный код 1
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            dbManager.conn.Close();
            dbManager.table.Clear();//предварительно очистим данные
            pictureBox1.Image = null;//именно null тк 0 не подойдет из-за типа, а null более полезен.
            pictureBox2.Image = null;

            if (dbManager.OpenDBFile())
            {
                tabControl1.Enabled = true;
                tabControl1.SelectedIndex = 0;
                //Для комбобокса
                findnames();
                comboBox3.SelectedIndex = 0;
                //0

                dbManager.ShowE("SELECT E FROM [Доп.Данные]"); dbManager.ShowA("SELECT A FROM [Доп.Данные]"); dbManager.ShowImage(pictureBox1);
                //1
                viewManager.PaintCells(dataGridView2);
                pictureBox1.Enabled = true;
                //2
                countpoint(listBox1);
                viewManager.PaintCells(dataGridView5);
                dbManager.ShowImage(pictureBox2);
                pictureBox2.Enabled = true;

                //3
                countpoint(listBox3);
                viewManager.PaintCells(dataGridView9);

                //4
                checkedcountpoint();
            }
        }

        private void findnames()
        {
            using (var connection = new SQLiteConnection(dbManager.conn))
            {
                DataTable schema = connection.GetSchema("Tables");
                List<string> tables = new List<string>();
                foreach (DataRow row in schema.Rows)
                {
                    string tableName = row["TABLE_NAME"].ToString();
                    if (tableName != "Доп.Данные")
                        tables.Add(tableName);
                }
                comboBox3.Items.Clear();
                comboBox3.Items.AddRange(tables.ToArray());
            }
        }
        private bool IsNumeric(string input)
        {
            return double.TryParse(input, System.Globalization.NumberStyles.AllowDecimalPoint,
                                  System.Globalization.NumberFormatInfo.InvariantInfo, out _);
        }
        //Программный код 4
        private void button1_Click_1(object sender, EventArgs e)
        {
            string input = textBox1.Text;
            if (IsNumeric(input))
            {
                dbManager.UpdateE(input);
            }
            else
            {
                MessageBox.Show("Введите числовое значение.");
            }
        }
        //Программный код 5 
        private void button2_Click(object sender, EventArgs e)
        {
            string input = textBox2.Text;
            if (IsNumeric(input))
            {
                dbManager.UpdateA(input);
            }
            else
            {
                MessageBox.Show("Введите числовое значение.");
            }
        }
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Form childForm = new Form();
            childForm.MaximizeBox = false;
            childForm.MinimizeBox = false;
            childForm.StartPosition = FormStartPosition.CenterScreen;
            childForm.FormBorderStyle = FormBorderStyle.FixedSingle;

            childForm.Text = "Схема";
            // Установка свойства BackgroundImage новой формы из pictureBox1
            childForm.BackgroundImage = pictureBox1.Image;

            // Установка размеров формы в соответствии с размерами изображения
            childForm.Size = pictureBox1.Image.Size;

            childForm.Show();
        }
        //Программный код 6
        private void generate_Button_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex != 1)
                dbManager.generate();
        }
        // №3 .
        // Программный код 19
        private void graphicgen(CheckBox checkBox, int serie, DataGridView dataGridView, Chart chart, string mu, string alpha)
        {
            if (checkBox.Checked)   
            {
                viewManager.ShowGraphs(serie, dataGridView, chart, mu, alpha);
            }
            else
            {
                chart.Series[serie].Points.Clear();
                chart.Refresh();
            }
        }
        // Программный код 21
        private void prognozgen(CheckBox checkBox, DataGridView dataGridView, string mu, string alpha, int serie, Chart chart, bool time)
        {
            if (checkBox.Checked)
            {
                double x = Convert.ToDouble(dataGridView.Rows[dataGridView.Rows.Count - 2].Cells[mu].Value);
                double y = Convert.ToDouble(dataGridView.Rows[dataGridView.Rows.Count - 2].Cells[alpha].Value);
                viewManager.ShowPoints(serie, chart, x, y, time);
            }
            else
            {
                chart.Series[serie].Points.Clear();
                chart.Refresh();
            }
        }
        private void checkBox2_CheckedChanged_1(object sender, EventArgs e)
        {
            graphicgen(checkBox2, 0, dataGridView3, chart1, "M", "alpha");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            graphicgen(checkBox1, 1, dataGridView3, chart1, "M-", "alpha-");
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            graphicgen(checkBox3, 2, dataGridView3, chart1, "M+", "alpha+");
        }
        private void checkBox4_CheckedChanged(object sender, EventArgs e)//прогноз a(mu)
        {
            prognozgen(checkBox4, dataGridView3, "M", "alpha", 3, chart1, false);
        }
        private void checkBox5_CheckedChanged(object sender, EventArgs e)//прогноз a(mu)+
        {
            prognozgen(checkBox5, dataGridView3, "M+", "alpha+", 4, chart1, false);
        }
        private void checkBox6_CheckedChanged(object sender, EventArgs e)//прогноз a(mu)-
        {
            prognozgen(checkBox6, dataGridView3, "M-", "alpha-", 5, chart1, false);
        }
        private void checkBox30_CheckedChanged(object sender, EventArgs e)
        {
            graphicgen(checkBox30, 1, dataGridView8, chart5, "M-", "alpha-");
        }

        private void checkBox28_CheckedChanged(object sender, EventArgs e)
        {
            graphicgen(checkBox28, 0, dataGridView8, chart5, "M", "alpha");
        }

        private void checkBox29_CheckedChanged(object sender, EventArgs e)
        {
            graphicgen(checkBox29, 2, dataGridView8, chart5, "M+", "alpha+");
        }

        private void checkBox27_CheckedChanged(object sender, EventArgs e)
        {
            prognozgen(checkBox27, dataGridView8, "M-", "alpha-", 4, chart5, false);
        }

        private void checkBox25_CheckedChanged(object sender, EventArgs e)
        {
            prognozgen(checkBox25, dataGridView8, "M", "alpha", 3, chart5, false);
        }

        private void checkBox26_CheckedChanged(object sender, EventArgs e)
        {
            prognozgen(checkBox26, dataGridView8, "M+", "alpha+", 5, chart5, false);
        }

        //увы, пока невозможно изменить тк поменял значения исключительно под этот случай, в класс viewandmisc такое не добавить.
        private void checkBox7_CheckedChanged(object sender, EventArgs e)//mu
        {
            if (checkBox7.Checked)
            {
                double x = 0;
                double y = Convert.ToDouble(dataGridView3.Rows[dataGridView3.Rows.Count - 2].Cells["M"].Value);
                viewManager.ShowPoints(3, chart2, x, y, true);
            }
            else
            {
                chart2.Series[3].Points.Clear();
                chart2.Refresh();
            }
        }
        private void checkBox8_CheckedChanged(object sender, EventArgs e)//mu-
        {
            if (checkBox8.Checked)
            {
                viewManager.ShowPoints(4, chart2, Convert.ToDouble(0), Convert.ToDouble(dataGridView3.Rows[dataGridView3.Rows.Count - 2].Cells["M-"].Value), true);
            }
            else
            {
                chart2.Series[4].Points.Clear();
                chart2.Refresh();
            }
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)//mu+
        {
            if (checkBox9.Checked)
            {
                viewManager.ShowPoints(5, chart2, Convert.ToDouble(0), Convert.ToDouble(dataGridView3.Rows[dataGridView3.Rows.Count - 2].Cells["M+"].Value), true);
            }
            else
            {
                chart2.Series[5].Points.Clear();
                chart2.Refresh();
            }
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            graphicgen(checkBox10, 0, dataGridView2, chart2, "M", "");
            if (checkBox10.Checked)
            {
                checkBox7.Enabled = true;
            }
            else
            {
                checkBox7.Enabled = false;
            }
        }
        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            graphicgen(checkBox11, 1, dataGridView2, chart2, "M-", "");
            if (checkBox11.Checked)
            {
                checkBox8.Enabled = true;
            }
            else
            {
                checkBox8.Enabled = false;
            }
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            graphicgen(checkBox12, 2, dataGridView2, chart2, "M+", "");
            if (checkBox12.Checked)
            {
                checkBox9.Enabled = true;
            }
            else
            {
                checkBox9.Enabled = false;
            }
        }
        private void checkBox32_CheckedChanged(object sender, EventArgs e)
        {
            graphicgen(checkBox32, 1, dataGridView8, chart7, "M-", "");
            if (checkBox32.Checked)
            {
                checkBox34.Enabled = true;
            }
            else
            {
                checkBox34.Enabled = false;
            }
        }

        private void checkBox36_CheckedChanged(object sender, EventArgs e)
        {
            graphicgen(checkBox36, 0, dataGridView8, chart7, "M", "");
            if (checkBox36.Checked)
            {
                checkBox35.Enabled = true;
            }
            else
            {
                checkBox35.Enabled = false;
            }
        }

        private void checkBox31_CheckedChanged(object sender, EventArgs e)
        {
            graphicgen(checkBox31, 2, dataGridView8, chart7, "M+", "");
            if (checkBox31.Checked)
            {
                checkBox33.Enabled = true;
            }
            else
            {
                checkBox33.Enabled = false;
            }
        }

        private void checkBox34_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox34.Checked)
            {
                viewManager.ShowPoints(4, chart7, Convert.ToDouble(chart7.Series[4 - 3].Points.Count), Convert.ToDouble(dataGridView8.Rows[dataGridView8.Rows.Count - 2].Cells["M-"].Value), true);
            }
            else
            {
                chart7.Series[4].Points.Clear();
                chart7.Refresh();
            }
        }

        private void checkBox33_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox33.Checked)
            {
                viewManager.ShowPoints(5, chart7, Convert.ToDouble(chart7.Series[5 - 3].Points.Count), Convert.ToDouble(dataGridView8.Rows[dataGridView8.Rows.Count - 2].Cells["M+"].Value), true);
            }
            else
            {
                chart7.Series[5].Points.Clear();
                chart7.Refresh();
            }
        }

        private void checkBox35_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox35.Checked)
            {
                viewManager.ShowPoints(3, chart7, Convert.ToDouble(chart7.Series[3 - 3].Points.Count), Convert.ToDouble(dataGridView8.Rows[dataGridView8.Rows.Count - 2].Cells["M"].Value), true);
            }
            else
            {
                chart7.Series[3].Points.Clear();
                chart7.Refresh();
            }
        }
        private void checkBox20_CheckedChanged(object sender, EventArgs e)//M- 2 decomp
        {
            graphicgen(checkBox20, 1, dataGridView4, chart4, "M-", "");
            if (checkBox20.Checked)
            {
                checkBox22.Enabled = true;
            }
            else
            {
                checkBox22.Enabled = false;
            }
        }

        private void checkBox24_CheckedChanged(object sender, EventArgs e)//M 2 decomp
        {
            graphicgen(checkBox24, 0, dataGridView4, chart4, "M", "");
            if (checkBox24.Checked)
            {
                checkBox23.Enabled = true;
            }
            else
            {
                checkBox23.Enabled = false;
            }
        }

        private void checkBox19_CheckedChanged(object sender, EventArgs e)//M+ 2 decomp
        {
            graphicgen(checkBox19, 2, dataGridView4, chart4, "M+", "");
            if (checkBox19.Checked)
            {
                checkBox21.Enabled = true;
            }
            else
            {
                checkBox21.Enabled = false;
            }
        }

        private void checkBox22_CheckedChanged(object sender, EventArgs e)//frcM- 2 decomp
        {
            if (checkBox22.Checked)
            {
                viewManager.ShowPoints(4, chart4, Convert.ToDouble(chart4.Series[4 - 3].Points.Count), Convert.ToDouble(dataGridView4.Rows[dataGridView4.Rows.Count - 2].Cells["M-"].Value), true);
            }
            else
            {
                chart4.Series[4].Points.Clear();
                chart4.Refresh();
            }
        }

        private void checkBox21_CheckedChanged(object sender, EventArgs e)//frcM+ 2 decomp
        {
            if (checkBox21.Checked)
            {
                viewManager.ShowPoints(5, chart4, Convert.ToDouble(chart4.Series[5 - 3].Points.Count), Convert.ToDouble(dataGridView4.Rows[dataGridView4.Rows.Count - 2].Cells["M+"].Value), true);
            }
            else
            {
                chart4.Series[5].Points.Clear();
                chart4.Refresh();
            }
        }

        private void checkBox23_CheckedChanged(object sender, EventArgs e)//frcM 2 decomp
        {
            if (checkBox23.Checked)
            {
                viewManager.ShowPoints(3, chart4, Convert.ToDouble(chart4.Series[3 - 3].Points.Count), Convert.ToDouble(dataGridView4.Rows[dataGridView4.Rows.Count - 2].Cells["M"].Value), true);
            }
            else
            {
                chart4.Series[3].Points.Clear();
                chart4.Refresh();
            }
        }
        //ПОСЛЕДНИЕ ГРАФИКИ!

        private void checkBox18_CheckedChanged(object sender, EventArgs e)//alpha- 2 decomp
        {
            graphicgen(checkBox18, 0, dataGridView4, chart3, "M-", "alpha-");
        }

        private void checkBox16_CheckedChanged(object sender, EventArgs e)//alpha 2 decomp
        {
            graphicgen(checkBox16, 1, dataGridView4, chart3, "M", "alpha");
        }

        private void checkBox17_CheckedChanged(object sender, EventArgs e)//alpha+ 2 decomp
        {
            graphicgen(checkBox17, 2, dataGridView4, chart3, "M+", "alpha+");
        }

        private void checkBox15_CheckedChanged(object sender, EventArgs e)//frcalpha- 2 decomp
        {

            if (checkBox15.Checked)
            {
                double x = Convert.ToDouble(dataGridView4.Rows[dataGridView4.Rows.Count - 2].Cells["M-"].Value);
                double y = Convert.ToDouble(dataGridView4.Rows[dataGridView4.Rows.Count - 2].Cells["alpha-"].Value);
                viewManager.ShowPoints(3, chart3, x, y, false);
            }
            else
            {
                chart3.Series[3].Points.Clear();
                chart3.Refresh();
            }
        }

        private void checkBox13_CheckedChanged(object sender, EventArgs e)//frcalpha 2 decomp
        {
            if (checkBox13.Checked)
            {
                double x = Convert.ToDouble(dataGridView4.Rows[dataGridView4.Rows.Count - 2].Cells["M"].Value);
                double y = Convert.ToDouble(dataGridView4.Rows[dataGridView4.Rows.Count - 2].Cells["alpha"].Value);
                viewManager.ShowPoints(4, chart3, x, y, false);
            }
            else
            {
                chart3.Series[4].Points.Clear();
                chart3.Refresh();
            }
        }

        private void checkBox14_CheckedChanged(object sender, EventArgs e)//frcalpha+ 2 decomp
        {
            if (checkBox14.Checked)
            {
                double x = Convert.ToDouble(dataGridView4.Rows[dataGridView4.Rows.Count - 2].Cells["M+"].Value);
                double y = Convert.ToDouble(dataGridView4.Rows[dataGridView4.Rows.Count - 2].Cells["alpha+"].Value);
                viewManager.ShowPoints(5, chart3, x, y, false);
            }
            else
            {
                chart3.Series[5].Points.Clear();
                chart3.Refresh();
            }
        }
        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int selectedIndex = listBox1.IndexFromPoint(e.Location); // Получаем индекс выбранного элемента
            if (comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Ошибка! Выберите, пожалуйста, блок!");
                return;
            }
            if (selectedIndex != ListBox.NoMatches)
            {
                object selectedItem = listBox1.Items[selectedIndex]; // Получаем выбранный элемент
                listBox2.Items.Add(selectedItem); // Добавляем элемент в ListBox2
                listBox1.Items.RemoveAt(selectedIndex); // Удаляем элемент из ListBox1
            }
            label24.Text = Convert.ToString(listBox2.Items.Count);
        }
        //Программный код 9
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)//выбор уровня.
        {
            switch (tabControl1.SelectedIndex)
            {
                case 1:
                    //1
                    lvl1.dt.Rows.Clear();
                    lvl1.dt.Columns.Clear();

                    viewManager.ShowTable(dataGridView3, lvl1.phase(DBManager.ConvertDataGridViewToDataTable(dataGridView1), dbManager.ShowE("SELECT E FROM [Доп.Данные]")));
                    lvl1.ExponentialSmoothing(DBManager.ConvertDataGridViewToDataTable(dataGridView3), dbManager.ShowA("SELECT A FROM [Доп.Данные]"));
                    lvl1.CreateColumnR(lvl1.table1);
                    lvl1.CreateColumnL(lvl1.table1);
                    viewManager.ShowTable(dataGridView2, lvl1.table1);
                    viewManager.PaintCells(dataGridView2);
                    break;
            }
        }
        private Dictionary<int, List<string>> listBoxData = new Dictionary<int, List<string>>();
        private int previousIndex = -1; 
        // Программный код 24
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            label24.Text = null;
            int selectedIndex = comboBox1.SelectedIndex;

            if (previousIndex != -1 && listBoxData.ContainsKey(previousIndex))
            {
                listBoxData[previousIndex].Clear();
            }

            else
            {
                listBoxData.Add(previousIndex, new List<string>());
            }

            foreach (var item in listBox2.Items)
            {
                listBoxData[previousIndex].Add(item.ToString());
            }

            //Теперь к отображению всего этого дела...
            listBox2.Items.Clear();
            if (previousIndex != -1 && listBoxData.ContainsKey(selectedIndex))
            {
                foreach (var item in listBoxData[selectedIndex])
                {
                    listBox2.Items.Add(item);
                }
            }
            previousIndex = selectedIndex;
        }
        // Программный код 28
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string tableName = Convert.ToString(comboBox2.SelectedItem);
            DataTable foundTable = lvl2.tables.Find(table => table.TableName == tableName);
            if (foundTable != null)
            {
                dataGridView4.DataSource = null;
                dataGridView5.DataSource = null;

                dataGridView4.Rows.Clear();
                dataGridView4.Columns.Clear();
                dataGridView5.Rows.Clear();
                dataGridView5.Columns.Clear();

                Lvl_1 block = new Lvl_1();

                viewManager.ShowTable(dataGridView4, block.phase(foundTable, dbManager.ShowE("SELECT E FROM [Доп.Данные]")));
                block.ExponentialSmoothing(DBManager.ConvertDataGridViewToDataTable(dataGridView4), dbManager.ShowA("SELECT A FROM [Доп.Данные]"));
                block.CreateColumnR(block.table1);
                block.CreateColumnL(block.table1);
                viewManager.ShowTable(dataGridView5, block.table1);
            }
        }

        //ДЕЛА С ЛИСТБОКСАМИ

        private void ListBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                string selectedItem = listBox1.SelectedItem.ToString();
                listBox1.DoDragDrop(selectedItem, DragDropEffects.Move);
            }
        }

        private void ListBox2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void ListBox2_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void ListBox2_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string item = e.Data.GetData(DataFormats.StringFormat).ToString();
                listBox2.Items.Add(item);
                listBox1.Items.Remove(item);
            }
        }

        private void listBox2_DoubleClick(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem != null)
            {
                string selectedItem = listBox2.SelectedItem.ToString();
                listBox2.Items.Remove(selectedItem);
                listBox1.Items.Add(selectedItem);
            }
            label24.Text = Convert.ToString(listBox2.Items.Count);
        }
        // Программный код 23
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            int selectedNumber;
            
            if (int.TryParse(textBox3.Text, out selectedNumber))
            {
                comboBox1.Items.Clear();

                for (int i = 0; i < selectedNumber; i++)
                {
                    char letter = (char)('А' + i);
                    comboBox1.Items.Add(letter.ToString());
                }
            }
            comboBox4.Items.Clear();
            label24.Text = null;
            foreach (object item in comboBox1.Items)
            {
                comboBox4.Items.Add(item);
            }
            if (!string.IsNullOrEmpty(textBox3.Text) && Convert.ToInt32(textBox3.Text) != 0)
            {
                label22.Text = Convert.ToString(listBox1.Items.Count / Convert.ToInt32(textBox3.Text));
            }
            else
            {
                label22.Text = "N/A"; 
            }

            // Очистить словарь listBoxData
            listBoxData.Clear();
        }

        // Программный код 26
        private void HandleButtonClick(ComboBox comboBox, ListBox listBox, Lvl_2 lvl, bool buttonClicked, TextBox textBox, int totalpoints, bool its_level3)
        {
            // Получение выбранного элемента из ComboBox
            string selectedComboBoxItem = comboBox.SelectedItem?.ToString();
            if (selectedComboBoxItem != null)
            {
                buttonClicked = true;
                DataSet dataSet = comboBox.Tag as DataSet;

                // Если DataSet не был создан, создаем новый
                if (dataSet == null)
                {
                    dataSet = new DataSet();
                    comboBox.Tag = dataSet;
                }

                // Проверка, что таблица с таким именем уже существует в DataSet
                if (dataSet.Tables.Contains(selectedComboBoxItem))
                {
                    // Удаление существующей таблицы
                    dataSet.Tables.Remove(selectedComboBoxItem);
                    lvl.RemoveTable(selectedComboBoxItem);

                    // Создание новой DataTable
                    DataTable newDataTable = new DataTable(selectedComboBoxItem);
                    newDataTable.Columns.Add("Эпоха");
                    // Добавление столбцов в DataTable на основе элементов ListBox
                    foreach (var item in listBox.Items)
                    {
                        string columnName = item.ToString();

                        // Проверка на уникальность имени столбца
                        if (!newDataTable.Columns.Contains(columnName))
                            newDataTable.Columns.Add(columnName);
                    }

                    // Добавление таблицы в DataSet
                    dataSet.Tables.Add(newDataTable);

                    // Добавление таблицы в класс Lvl_2
                    lvl.CreateBlock(dbManager.table, newDataTable);
                }
                else
                {
                    // Создание новой DataTable
                    DataTable newDataTable = new DataTable(selectedComboBoxItem);
                    newDataTable.Columns.Add("Эпоха");

                    // Добавление столбцов в DataTable на основе элементов ListBox
                    foreach (var item in listBox.Items)
                    {
                        string columnName = item.ToString();

                        // Проверка на уникальность имени столбца
                        if (!newDataTable.Columns.Contains(columnName))
                            newDataTable.Columns.Add(columnName);
                    }
                    dataSet.Tables.Add(newDataTable);

                    // Добавление таблицы в класс Lvl_2

                    lvl.CreateBlock(dbManager.table, newDataTable);
                }

                // Обновление списка элементов ListBox на основе выбранного индекса ComboBox

                lvl.AddTablesFromDataSet(dataSet);

                if (lvl.CheckControlPoints(totalpoints, listBox, textBox, comboBox))
                {
                    // Вывод информации о созданной таблице
                    MessageBox.Show($"Создан блок '{selectedComboBoxItem}' с {listBox.Items.Count} точками.");
                }
                else if (!its_level3)
                {
                    MessageBox.Show("Ошибка: Количество контрольных точек на всех блоках не совпадает! Блок не был создан.");
                    dataSet.Tables.Remove(selectedComboBoxItem);
                    lvl.RemoveTable(selectedComboBoxItem);
                }
            }
            else
            {
                MessageBox.Show("Ошибка! Выберите блок");
                return;
            }

        }
        //Программный код 30    
        private void button10_Click(object sender, EventArgs e)
        {
            
            HandleButtonClick(comboBox4, listBox6, lvl3, false, textBox3, dataGridView1.Columns.Count - 1, false);

            listBox5.Items.Clear();
            listBox4.Items.Clear();
            foreach (object item in listBox6.Items)
            {
                listBox5.Items.Add(item);
            }
            DataTable differenceTable = lvl3.CreateDifferenceTable(dbManager.table, listBox5);
            dataGridView6.DataSource = differenceTable;
            lvl3.CreateLastTable();
            DataTable connectionsTable = lvl3.CreateConnectionsTable(Convert.ToDouble(dbManager.ShowE("SELECT E FROM [Доп.Данные]")));
            dataGridView7.DataSource = connectionsTable;
            viewManager.Colorizeplusminus(dataGridView7);
        }
        // Программный код 25
        private void button8_Click(object sender, EventArgs e)
        {
            HandleButtonClick(comboBox1, listBox2, lvl2, false, textBox3, dataGridView1.Columns.Count - 1, false);
        }

        private void tabControl4_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox2.Items.Clear(); // Очищаем список элементов в comboBox2

            foreach (DataTable table in lvl2.tables)
            {
                comboBox2.Items.Add(table.TableName); // Добавляем название таблицы в comboBox2
            }
        }
        private void tabControl6_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabControl6.SelectedIndex)
            {
                case 0:
                    break;
                case 1:
                    viewManager.PaintCells(dataGridView5);
                    break;
            }
        }
        private void tabControl2_SelectedIndexChanged(object sender, EventArgs e)
        {
            viewManager.PaintCells(dataGridView2);
        }
        private void listBox6_DoubleClick(object sender, EventArgs e)
        {
            if (listBox6.SelectedItem != null)
            {
                string selectedItem = listBox6.SelectedItem.ToString();
                listBox6.Items.Remove(selectedItem);
                listBox3.Items.Add(selectedItem);
            }
        }

        private DataTable podblocktochki = new DataTable();
        //Программный код 34
        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            podblocktochki.Columns.Clear();
            podblocktochki.Rows.Clear();

            podblocktochki.Columns.Add("Подблок");
            podblocktochki.Columns.Add("Точки");
            if (int.TryParse(textBox5.Text, out int maxValue))//дабы не вводил юзер что вздумается
            {
                comboBox5.Items.Clear();
                listBox4.Items.Clear();
                podblocktochki.Clear();

                for (int i = 1; i <= maxValue; i++)
                {
                    comboBox5.Items.Add(i);
                    podblocktochki.Rows.Add(i, 0);//добавим попросту точку в первый столбец, в котором будут храниться эти названия подблоков, а правее будет список точек
                }
            }
            else 
            {
                MessageBox.Show("Количество подблоков должно быть положительным целым числом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox4.Items.Clear();

            if (comboBox5.SelectedItem != null)
            {
                int selectedIndex = (int)comboBox5.SelectedItem;

                if (podblocktochki.Rows.Count >= selectedIndex)
                {
                    string points = podblocktochki.Rows[selectedIndex - 1][1].ToString();
                    string[] pointArray = points.Split(' ');

                    listBox4.Items.AddRange(pointArray);
                }
            }
        }
        List<string> addedPoints = new List<string>();
        //Программный код 35
        private void textBox4_TextChanged(object sender, EventArgs e)
        {

            listBox5.Items.AddRange(addedPoints.ToArray());
            addedPoints.Clear();
            podblocktochki.Clear();

            if (int.TryParse(textBox4.Text, out int pointCount) && int.TryParse(textBox5.Text, out int podblockCount))
            {
                int listBoxItemCount = listBox5.Items.Count;

                if (pointCount <= 0|| podblockCount <= 0|| listBoxItemCount <= 0)
                {
                    MessageBox.Show("Количество точек должно быть положительным числом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    textBox4.Text = ""; // Очищаем поле ввода
                    return;
                }   
                for (int i = 0; i < podblockCount; i++)//распределяем по подблокам точки
                {
                    string points = "";

                    for (int j = 0; j < pointCount; j++)//распределяем точки
                    {
                        int index = (i * pointCount + j) % listBoxItemCount;
                        if (index < listBoxItemCount)
                        {
                            string point = listBox5.Items[index].ToString();
                            addedPoints.Add(point);
                            points += point + " ";
                        }
                    }
                    podblocktochki.Rows.Add(i + 1, points);
                }
                List<string> missingPoints = new List<string>();

                // Заполняем список недостающих точек из listBox5
                foreach (string point in listBox5.Items)
                {
                    bool found = false;

                    foreach (DataRow row in podblocktochki.Rows)
                    {
                        string[] pointsArray = row[1].ToString().Split(' ');

                        if (pointsArray.Contains(point))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        missingPoints.Add(point);
                    }
                }
                listBox5.Items.Clear();
                listBox5.Items.AddRange(missingPoints.ToArray());

            }
            else
            {
                listBox5.Items.Clear();
                addedPoints.Clear();
                podblocktochki.Clear();
            }

        }
        private bool button4Clicked = false;
        //Программный код 36
        private void button4_Click(object sender, EventArgs e)
        {
            button4Clicked = true;
            HandleButtonClick(comboBox5, listBox4, lvl3, button4Clicked, textBox5, listBox6.Items.Count, true);
        }
        // Программный код 37
        private void tabControl7_SelectedIndexChanged(object sender, EventArgs e)
        {
            string tableName = Convert.ToString(comboBox5.SelectedItem);
            DataTable foundTable = lvl3.tables.Find(table => table.TableName == tableName);
            if (foundTable != null)
            {
                // Очищаем DataGridView перед добавлением новых данных
                dataGridView8.DataSource = null;
                dataGridView9.DataSource = null;

                dataGridView8.Rows.Clear();
                dataGridView8.Columns.Clear();
                dataGridView9.Rows.Clear();
                dataGridView9.Columns.Clear();

                Lvl_1 block = new Lvl_1();

                viewManager.ShowTable(dataGridView8, block.phase(foundTable, dbManager.ShowE("SELECT E FROM [Доп.Данные]")));
                block.ExponentialSmoothing(DBManager.ConvertDataGridViewToDataTable(dataGridView8), dbManager.ShowA("SELECT A FROM [Доп.Данные]"));
                block.CreateColumnR(block.table1);
                block.CreateColumnL(block.table1);
                viewManager.ShowTable(dataGridView9, block.table1);
            }
        }

        private void pictureBox2_DoubleClick(object sender, EventArgs e)
        {
            Form childForm = new Form();
            childForm.MaximizeBox = false;
            childForm.MinimizeBox = false;
            childForm.StartPosition = FormStartPosition.CenterScreen;
            childForm.FormBorderStyle = FormBorderStyle.FixedSingle;
            childForm.Text = "Схема";

            // Установка свойства BackgroundImage новой формы из pictureBox2
            childForm.BackgroundImage = pictureBox2.Image;

            // Установка размеров формы в соответствии с размерами изображения
            childForm.Size = pictureBox2.Image.Size;

            childForm.Show();
        }
        ///
        /// ФУНКЦИИ
        ///
        private void countpoint(ListBox listbox)
        {
            int columnCount = dataGridView1.Columns.Count;

            // Очистка ListBox перед добавлением элементов
            listbox.Items.Clear();

            // Добавление столбцов в ListBox
            for (int i = 1; i < columnCount; i++)
            {
                string columnName = dataGridView1.Columns[i].HeaderText;
                listbox.Items.Add(columnName);
            }
        }
        //Программный код 38
        private void checkedcountpoint()
        {
            int columnCount = dataGridView1.Columns.Count;

            // Очистка ListBox перед добавлением элементов
            checkedListBox1.Items.Clear();

            // Добавление столбцов в ListBox
            for (int i = 1; i < columnCount; i++)
            {
                string columnName = dataGridView1.Columns[i].HeaderText;
                checkedListBox1.Items.Add(columnName);
            }
        }

        //Программный код 39 
            private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
            {
                // Проверяем, выбран ли элемент
                if (e.NewValue == CheckState.Checked)
                {
                    // Получаем выбранный элемент из checkedListBox1
                    string selectedItem = checkedListBox1.Items[e.Index].ToString();

                    // Создаем серию графика с выбранным элементом в качестве имени
                    Series series = new Series(selectedItem);
                    series.ChartType = SeriesChartType.Spline;

                    // Добавляем серию в chart6
                    chart6.Series.Add(series);

                    // Передаем правильное имя столбца в метод ShowGraphs
                    string yColumnName = selectedItem; // Передаем имя столбца, соответствующее выбранному элементу
                    viewManager.ShowGraphs(chart6.Series.Count - 1, dataGridView1, chart6, "Эпоха", yColumnName);

                    DataTable dataTable = new DataTable(selectedItem);
                    Lvl_1 lvl_4 = new Lvl_1();
                    dataTable.Columns.Add(selectedItem, typeof(double));
                    Lvl_2 prognoz = new Lvl_2();
                    prognoz.CreateBlock(DBManager.ConvertDataGridViewToDataTable(dataGridView1), dataTable);
                    double prevM = Convert.ToDouble(dataTable.Rows[0][selectedItem]);
                    double currentM = 0.0;
                    double forecastM = 0.0;
                    double sumM = 0.0;
                    double A = Convert.ToDouble(Convert.ToDouble(textBox2.Text.Replace(".",",")));
                    for (int i = 1; i < dataTable.Rows.Count - 1; i++)
                    {
                        double MValue = Convert.ToDouble(dataTable.Rows[i][selectedItem]);
                        currentM = A * MValue + (1 - A) * prevM;
                        dataTable.Rows[i][selectedItem] = currentM;
                        prevM = currentM;
                        sumM += currentM;
                    }

                    // Рассчитываем прогноз для последней точки
                    forecastM = A * dataTable.Rows[dataTable.Rows.Count - 2].Field<double>(selectedItem) + (1 - A) * prevM;
                    DataPoint newpoint = new DataPoint(chart6.Series[selectedItem].Points.Count + 1, forecastM);
                    newpoint.Label = "Прогноз";
                    chart6.Series[selectedItem].Points.Add(newpoint);

                    double minY = double.MaxValue;
                    double maxY = double.MinValue;
                    foreach (Series existingSeries in chart6.Series)
                    {
                        foreach (DataPoint point in existingSeries.Points)
                        {
                            double yValue = point.YValues[0];
                            minY = Math.Min(minY, yValue);
                            maxY = Math.Max(maxY, yValue);
                        }
                    }
 
                    double range = maxY - minY;
                    double padding = 0.05;

                    minY = minY - range * padding;
                    maxY = maxY + range * padding;
                    if (minY < 0)
                        minY = 0;
                    chart6.ChartAreas[0].AxisY.Minimum = minY;
                    chart6.ChartAreas[0].AxisY.Maximum = maxY;
                }
                else if (e.NewValue == CheckState.Unchecked)
                {
                    // Получаем имя серии, которую нужно удалить
                    string seriesName = checkedListBox1.Items[e.Index].ToString();

                    // Ищем серию по имени в chart6 и удаляем ее
                    Series seriesToRemove = chart6.Series.FindByName(seriesName);
                    if (seriesToRemove != null)
                    {
                        chart6.Series.Remove(seriesToRemove);
                    }

                    // Рассчитываем минимальное и максимальное значение
                    double minY = double.MaxValue;
                    double maxY = double.MinValue;

                    foreach (Series existingSeries in chart6.Series)
                    {
                        foreach (DataPoint point in existingSeries.Points)
                        {
                            double yValue = point.YValues[0];
                            minY = Math.Min(minY, yValue);
                            maxY = Math.Max(maxY, yValue);
                        }
                    }
                    double range = maxY - minY;
                    double padding = 0.05;

                    minY = minY - range * padding;
                    maxY = maxY + range * padding;
                    if (minY < 0)
                        minY = 0;
                    // Устанавливаем минимальное и максимальное значение оси Y
                    chart6.ChartAreas[0].AxisY.Minimum = minY;
                    chart6.ChartAreas[0].AxisY.Maximum = maxY;
                }
            }
        /// <summary>
        /// Третий уровень...
        /// </summary>
        private Dictionary<int, List<string>> listBoxData2 = new Dictionary<int, List<string>>();
        private int previousIndex1 = -1; // Изначально устанавливаем предыдущий индекс как -1
        //Программный код 29
        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox4.SelectedIndex;

            if (previousIndex != -1 && listBoxData2.ContainsKey(previousIndex1))
            {
                listBoxData2[previousIndex1].Clear();
            }

            else
            {
                listBoxData2.Add(previousIndex1, new List<string>());
            }

            foreach (var item in listBox6.Items)
            {
                listBoxData2[previousIndex1].Add(item.ToString());
            }

            //Теперь к отображению всего этого дела...
            listBox6.Items.Clear(); 
            if (previousIndex != -1 && listBoxData2.ContainsKey(selectedIndex))
            {
                foreach (var item in listBoxData2[selectedIndex])
                {
                    listBox6.Items.Add(item);
                }
            }
            previousIndex1 = selectedIndex;
        }

        private void listBox3_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int index = listBox3.IndexFromPoint(e.X, e.Y);
                if (index >= 0)
                {
                    listBox3.DoDragDrop(listBox3.Items[index], DragDropEffects.Move);
                }
            }
        }

        private void listBox6_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void listBox6_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)))
            {
                string item = (string)e.Data.GetData(typeof(string));
                listBox6.Items.Add(item);
                listBox3.Items.Remove(item);
            }
        }

        private void listBox5_MouseDown(object sender, MouseEventArgs e)
        {
            if (listBox5.SelectedItem != null)
            {
                listBox5.DoDragDrop(listBox5.SelectedItem, DragDropEffects.Move);
            }
        }

        private void listBox4_DragDrop(object sender, DragEventArgs e)
        {
            string item = (string)e.Data.GetData(typeof(string));

            if (!listBox4.Items.Contains(item))
            {
                listBox4.Items.Add(item);
                listBox5.Items.Remove(item);
            }
        }

        private void listBox4_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void listBox4_DoubleClick(object sender, EventArgs e)
        {
            if (listBox4.SelectedItem != null)
            {
                listBox4.Items.Remove(listBox4.SelectedItem);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // Создаем новую форму для отображения таблиц
            Form tableForm = new Form();
            tableForm.Text = "Таблицы графиков";

            // Создаем контейнер для размещения таблиц
            TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableForm.Controls.Add(tableLayoutPanel);

            // Проходим по всем сериям графиков
            foreach (Series series in chart6.Series)
            {
                // Создаем новую таблицу для текущей серии
                DataTable dataTable = new DataTable(series.Name);
                
                // Добавляем колонки в таблицу (вам нужно определить, какие колонки вам нужны)
                // Например, добавьте колонки для "Эпоха" и "Значение"
                dataTable.Columns.Add("Эпоха", typeof(int));
                dataTable.Columns.Add("Значение", typeof(double));

                // Заполняем таблицу данными из серии графика
                for (int i = 0; i < series.Points.Count; i++)
                {
                    double xValue = series.Points[i].XValue;
                    double yValue = series.Points[i].YValues[0];
                    dataTable.Rows.Add(xValue, yValue);
                }

                // Создаем новую DataGridView для отображения таблицы
                DataGridView dataGridView9 = new DataGridView();
                dataGridView9.Dock = DockStyle.Fill;
                dataGridView9.DataSource = dataTable;
                dataGridView9.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

                // Добавляем DataGridView в контейнер
                tableLayoutPanel.Controls.Add(dataGridView9);
            }

            // Показываем форму с таблицами
            tableForm.ShowDialog();
        }
    }
    /// 
    ///Классы
    ///
    public class DBManager
        {
            public SQLiteConnection conn;
            private SQLiteDataAdapter adapter;
            private TextBoxBase textBox;
            private TextBoxBase textBox1;
            private PictureBox pictureBox;
            private DataGridView dataGridView;
            public string folder;
            public DataTable table = new DataTable();
            Random random = new Random(); // инициализируем генератор случайных чисел

            public DBManager(DataGridView dataGridView, TextBoxBase textBox, PictureBox pictureBox, TextBoxBase textBox1)
            {
                this.textBox = textBox;
                this.textBox1 = textBox1;
                this.pictureBox = pictureBox;
                this.dataGridView = dataGridView;
                conn = new SQLiteConnection();
            }
            //Программный код 2
            public bool OpenDBFile()
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                folder = openFileDialog.InitialDirectory;
                openFileDialog.Filter = "База данных (*.sqlite)|*.sqlite|Все файлы (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    conn.ConnectionString = "Data Source=" + openFileDialog.FileName + ";Version=3;";
                    conn.Open();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        public DataTable GetAllDataFromTable(string tableName)
        {
            string sqlQuery = $"SELECT * FROM [{tableName}]";

            using (SQLiteCommand command = new SQLiteCommand(sqlQuery, conn))
            {
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                {
                    adapter.Fill(table);
                }
            }
            return table;
        }

        public void ShowImage(PictureBox pictureBox)
            {
                string sql = "SELECT Схема FROM [Доп.Данные];";
                using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            byte[] imageBytes = (byte[])reader["Схема"];
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            {
                                Image originalImage = Image.FromStream(ms);
                                Image scaledImage = new Bitmap(originalImage, pictureBox.Width, pictureBox.Height);
                                pictureBox.Image = scaledImage;
                            }
                        }
                    }
                }
            }
        //Программный код 11
        public string ShowE(string SQLQuery)
            {
                using (SQLiteCommand command = new SQLiteCommand(SQLQuery, conn))
                {
                    SQLiteDataReader reader = command.ExecuteReader();
                    string e = "";
                    while (reader.Read())
                    {
                        e += reader["E"].ToString();
                    }
                    textBox.Text = e;
                    return e;
                }   
            }

            public void UpdateE(string newValue)
            {
                // Заменяем запятые на точки в значении newValue
                newValue = newValue.Replace(",", CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator); 

                string sqlQuery = "UPDATE [Доп.Данные] SET E = @E";
                using (SQLiteCommand command = new SQLiteCommand(sqlQuery, conn))
                {
                    command.Parameters.AddWithValue("@E", newValue);
                    command.ExecuteNonQuery();
                }
            }
        //Программный код 13
        public double ShowA(string SQLQuery)
        {
            try
            {
                using (SQLiteCommand command = new SQLiteCommand(SQLQuery, conn))
                {
                    SQLiteDataReader reader = command.ExecuteReader();
                    string A = "";
                    while (reader.Read())
                    {
                        A += reader["A"].ToString();
                    }
                    if (A == "")
                    {
                        A = "0";
                    }
                    textBox1.Text = A;
                    double a = Convert.ToDouble(A);
                    return a;
                }
            }
            catch (SQLiteException)
            {
                return 0;
            }
        }


        public void UpdateA(string newValue)
            {
                // Заменяем запятые на точки в значении newValue
                newValue = newValue.Replace(",", CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator); 

                string sqlQuery = "UPDATE [Доп.Данные] SET A = @A";
                using (SQLiteCommand command = new SQLiteCommand(sqlQuery, conn))
                {
                    command.Parameters.AddWithValue("@A", newValue);
                    command.ExecuteNonQuery();
                }
            }
            public void generate()
            {
                double deltaSum = 0.0;
                int epocha = 1;

                if (table.Rows.Count > 0 && !table.Rows[table.Rows.Count - 1].RowState.Equals(DataRowState.Deleted))
                {
                    epocha = int.Parse(Convert.ToString(table.Rows[table.Rows.Count - 1][0])) + 1;
                }

                DataRow newRow = table.NewRow();
                newRow[0] = epocha;
                table.Rows.Add(newRow);

                // Определяем максимальный индекс, чтобы избежать проблем с удаленными строками
                int maxIndex = table.Rows.Count - 1;

                for (int j = 1; j < table.Columns.Count; j++)
                {
                    deltaSum = 0.0;

                    for (int i = 1; i < maxIndex; i++)
                    {
                        if (!table.Rows[i].RowState.Equals(DataRowState.Deleted) && !table.Rows[i - 1].RowState.Equals(DataRowState.Deleted))
                        {
                            double delta = Math.Round(Convert.ToDouble(table.Rows[i][j]), 5) - Math.Round(Convert.ToDouble(table.Rows[i - 1][j]), 5);
                            deltaSum += Math.Round(delta, 5);
                        }
                    }

                    double avgDelta = Math.Round(deltaSum / (maxIndex - 2), 5);
                    double randomValue = 0;
                    double randomOffset = random.NextDouble() * 2 - 1;

                    randomValue = avgDelta * randomOffset;

                    if (!table.Rows[maxIndex - 1].RowState.Equals(DataRowState.Deleted))
                    {
                        table.Rows[maxIndex][j] = Math.Round(Convert.ToDouble(table.Rows[maxIndex - 1][j]), 5) + Math.Round(randomValue, 5);
                    }
                }
                dataGridView.DataSource = table;
            }
            //Программный код 15
            public static DataTable ConvertDataGridViewToDataTable(DataGridView dataGridView)
            {
                DataTable dataTable = new DataTable();

                // Добавляем столбцы в DataTable на основе столбцов DataGridView
                foreach (DataGridViewColumn dataGridViewColumn in dataGridView.Columns)
                {
                    dataTable.Columns.Add(dataGridViewColumn.HeaderText, dataGridViewColumn.ValueType);
                }

                // Добавляем строки в DataTable на основе строк DataGridView
                foreach (DataGridViewRow dataGridViewRow in dataGridView.Rows)
                {
                    // Проверяем, является ли текущая строка последней
                    if (dataGridViewRow.IsNewRow)
                    {
                        break; // Прекращаем цикл при достижении последней строки
                    }

                    DataRow dataRow = dataTable.NewRow();
                    foreach (DataGridViewCell dataGridViewCell in dataGridViewRow.Cells)
                    {
                        dataRow[dataGridViewCell.ColumnIndex] = dataGridViewCell.Value;
                    }
                    dataTable.Rows.Add(dataRow);
                }
                return dataTable;
            }
        }
        public class ViewAndMiskManager
        {
            //Программный код 12
            public void ShowTable(DataGridView dataGridView, DataTable table)
            {
                dataGridView.DataSource = null;
                dataGridView.Rows.Clear();
                dataGridView.Columns.Clear();
                dataGridView.DataSource = table;
            }
            //Программный код 20
            public void ShowGraphs(int serie, DataGridView dgv, Chart chart, string xColumnName, string yColumnName)
            {
                if (yColumnName != "")
                {   
                    DataTable table = new DataTable();
                    table.Columns.Add("X", typeof(double));
                    table.Columns.Add("Y", typeof(double));
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        if (row.IsNewRow) continue; // проверяем, является ли строка последней
                        double x = Convert.ToDouble(row.Cells[xColumnName].Value);
                        double y = Convert.ToDouble(row.Cells[yColumnName].Value);
                        table.Rows.Add(x, y);
                    }
                    BindingSource bs = new BindingSource();
                    bs.DataSource = table.Copy();
                    chart.Series[serie].Points.DataBind(bs, "X", "Y", "");
                    
                    // Добавление подписей к каждой точке
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        string label = i.ToString(); // Значение из первого столбца
                        chart.Series[serie].Points[i].Label = label;
                    }
                    double minY = double.MaxValue;
                    double maxY = double.MinValue;
                    double minX = double.MaxValue;
                    double maxX = double.MinValue;
                    foreach (Series series in chart.Series)
                    {
                        foreach (DataPoint point in series.Points)
                        {
                            double y = point.YValues[0];
                            if (y < minY)
                                minY = y;
                            if (y > maxY)
                                maxY = y;
                            double x = point.XValue;
                            if (x < minX)
                                minX = x;
                            if (x > maxX)
                                maxX = x;
                        }
                    }
                    if (minY != double.MaxValue && maxY != double.MinValue && minY !=maxY)
                    {
                        double padding = (maxY - minY) * 0.09; // Добавление небольшого отступа
                        chart.ChartAreas[0].AxisY.Minimum = minY - padding;
                        chart.ChartAreas[0].AxisY.Maximum = maxY + padding;
                        double paddingX = (maxX - minX) * 0.09; // Добавление небольшого отступа
                        chart.ChartAreas[0].AxisX.Minimum = minX-paddingX;
                        chart.ChartAreas[0].AxisX.Maximum = maxX+paddingX;
                    }
                    else
                    {
                        // Очистка настроек интервала, так как данных нет
                        chart.ChartAreas[0].AxisY.Minimum = 0;
                        chart.ChartAreas[0].AxisY.Maximum = 0.0005;
                        double padding = (maxX - minX) * 0.255; 
                        chart.ChartAreas[0].AxisX.Minimum = minX-padding;
                        chart.ChartAreas[0].AxisX.Maximum = maxX+padding;
                    }
                    chart.ChartAreas[0].AxisX.Title = xColumnName;
                    chart.ChartAreas[0].AxisY.Title = yColumnName;
                }

                else
                {
                    chart.ChartAreas[0].AxisY.IsStartedFromZero = true;
                    DataTable table = new DataTable();
                    table.Columns.Add("X", typeof(double));
                    table.Columns.Add("Y", typeof(double));
                    for (int row = 0; row < dgv.Rows.Count - 2; row++)
                    {
                        double x = Convert.ToDouble(dgv.Rows[row].Cells[0].Value);
                        double y = Convert.ToDouble(dgv.Rows[row].Cells[xColumnName].Value);
                        table.Rows.Add(x, y);
                    }
                    BindingSource bs = new BindingSource();
                    bs.DataSource = table.Copy();
                    
                    chart.Series[serie].Points.DataBind(bs, "X", "Y", "");
                    chart.ChartAreas[0].AxisY.Title = xColumnName;
                    double minY = double.MaxValue;
                    double maxY = double.MinValue;
                    double minX = double.MaxValue;
                    double maxX = double.MinValue;
                    foreach (Series series in chart.Series)
                    {
                        foreach (DataPoint point in series.Points)
                        {
                            double y = point.YValues[0];
                            if (y < minY)
                                minY = y;
                            if (y > maxY)
                                maxY = y;

                            // Добавление подписи к значению оси Y
                            string label = y.ToString(); 
                            point.Label = label;
                        }
                        
                    }
                    // Проверка, что minY и maxY не являются значениями по умолчанию
                    if (minY != double.MaxValue && maxY != double.MinValue)
                    {
                        double padding = (maxY - minY) * 0.155+0.0001; // Добавление отступа
                        chart.ChartAreas[0].AxisY.Minimum = minY - padding;
                        chart.ChartAreas[0].AxisY.Maximum = maxY + padding;
                        
                    }
                    else
                    {
                        chart.ChartAreas[0].AxisY.Minimum = double.MinValue;
                        chart.ChartAreas[0].AxisY.Maximum = double.MaxValue; 
                        double padding = (maxX - minX) * 0.255 + 0.0001;
                        chart.ChartAreas[0].AxisX.Minimum = minX - padding; 
                        chart.ChartAreas[0].AxisX.Maximum = maxX + padding;
                    }   
                }
            }
            // Программный код 22
            public void ShowPoints(int serie, Chart chart, double x, double y, bool time)
            {
                DataTable table = new DataTable();
                if (!time)
                {
                    table.Columns.Add("X", typeof(double));
                    table.Columns.Add("Y", typeof(double));
                    table.Rows.Add(x, y);
                    BindingSource bs = new BindingSource();
                    bs.DataSource = table.Copy();
                    chart.Series[serie].Points.DataBind(bs, "X", "Y", "");

                    // Добавление подписи к точке данных
                    string label = "Прогноз"; // Используйте нужное форматирование подписи
                    chart.Series[serie].Points[0].Label = label;
                }
                else 
                {
                    int counter = chart.Series[serie - 3].Points.Count;
                    x = counter;
                    table.Columns.Add("X", typeof(double));
                    table.Columns.Add("Y", typeof(double));
                    table.Rows.Add(x, y);
                    BindingSource bs = new BindingSource();
                    bs.DataSource = table.Copy();
                    chart.Series[serie].Points.DataBind(bs, "X", "Y", "");

                    // Добавление подписи 
                    string label = "Прогноз";
                    chart.Series[serie].Points[0].Label = label;
                }
            }
            public void Colorizeplusminus(DataGridView dataGridView)
            {
                dataGridView.CellFormatting += (sender, e) =>
                {
                    if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                    {
                        DataGridViewCell cell = dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
                        string value = cell.Value?.ToString();

                        if (value == "+")
                            cell.Style.BackColor = Color.LightGreen;
                        else if (value == "-")
                            cell.Style.BackColor = Color.IndianRed;
                    }
                };
            }
            //Программный код 18
            public void PaintCells(DataGridView dataGridView)
            {
                for (int i = 0; i < dataGridView.Rows.Count - 1; i++)
                {
                    for (int j = 0; j < dataGridView.Columns.Count; j++)
                    {
                        if (dataGridView.Rows[i].Cells[j].Value.ToString() == "Не изменяемое")
                        {
                            dataGridView.Rows[i].Cells[j].Style.BackColor = Color.LightGreen;
                        }
                        else if (dataGridView.Rows[i].Cells[j].Value.ToString() == "Аварийное")
                        {
                            dataGridView.Rows[i].Cells[j].Style.BackColor = Color.Red;
                        }
                        else if (dataGridView.Rows[i].Cells[j].Value.ToString() == "Предаварийное")
                        {
                            dataGridView.Rows[i].Cells[j].Style.BackColor = Color.Orange;
                        }
                    }
                }
            }
        }

        public class Lvl_1
        {
            public DataTable dt = new DataTable();
            public DataTable table1 = new DataTable();
            //Программный код 10

            public DataTable phase(DataTable table, string e)
            {
                if (table.Columns.Contains("Column1"))
                    table.Columns.Remove("Column1");

                string[] columnNames = { "Эпоха", "M+", "M", "M-", "alpha+", "alpha", "alpha-" };
                foreach (string columnName in columnNames)
                {
                    dt.Columns.Add(columnName);
                }
                double E = Convert.ToDouble(e);
                // Пройти по каждой строке таблицы и рассчитать M+, M и M- с M+E
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    DataRow row = table.Rows[i];
                    double MValue = M(row);
                    double MPlusEValue = MPlusE(row, E);
                    double MMinusValue = MMinusE(row, E);
                    int epochValue = Convert.ToInt32(table.Rows[i]["Эпоха"]); // Предполагается, что столбец "Эпоха" имеет тип int
                    dt.Rows.Add(epochValue, MPlusEValue, MValue, MMinusValue, 0, 0, 0);
                }

                //Вычислить угол между каждой парой последовательных векторов
                for (int i = 1; i < dt.Rows.Count; i++)
                {
                    double M_0 = Convert.ToDouble(dt.Rows[0]["M"]);
                    double M_i = Convert.ToDouble(dt.Rows[i]["M"]);

                    double MPlusE_0 = Convert.ToDouble(dt.Rows[0]["M+"]);
                    double MPlusE_i = Convert.ToDouble(dt.Rows[i]["M+"]);

                    double MMinusE_0 = Convert.ToDouble(dt.Rows[0]["M-"]);
                    double MMinusE_i = Convert.ToDouble(dt.Rows[i]["M-"]);

                    double dotProduct = 0.0;
                    double dotProductPlusE = 0.0;
                    double dotProductMinusE = 0.0;

                    for (int j = 1; j < table.Columns.Count; j++)
                    {
                        double value0 = Convert.ToDouble(table.Rows[0][j]);
                        double valuei = Convert.ToDouble(table.Rows[i][j]);
                        dotProduct += valuei * value0;

                        dotProductPlusE += (valuei + E) * (value0 + E);
                        dotProductMinusE += (valuei - E) * (value0 - E);
                    }

                    double cosine = dotProduct / Math.Abs(M_0 * M_i);
                    double cosinePlusE = dotProductPlusE / Math.Abs(MPlusE_0 * MPlusE_i);
                    double cosineMinusE = dotProductMinusE / Math.Abs(MMinusE_0 * MMinusE_i);
                    if (cosine >= 1)
                    {
                        dt.Rows[i]["alpha"] = 0;
                        dt.Rows[i]["alpha+"] = 0;
                        dt.Rows[i]["alpha-"] = 0;
                    }
                    else 
                    { 
                        double angle = Math.Round(180 * Math.Acos(cosine) / Math.PI,5);
                        double anglePlusE = Math.Round(180 * Math.Acos(cosinePlusE) / Math.PI,5);
                        double angleMinusE = Math.Round(180 * Math.Acos(cosineMinusE) / Math.PI,5);
                    
                        dt.Rows[i]["alpha"] = angle;
                        dt.Rows[i]["alpha+"] = anglePlusE;
                        dt.Rows[i]["alpha-"] = angleMinusE;
                    }
                }
                table1 = dt.Copy();

                return dt;
            }


            public static double M(DataRow row)
            {
                double sumSquares = 0.0;
                for (int i = 1; i < row.Table.Columns.Count; i++)
                {
                    double value = Convert.ToDouble(row[i]);
                    sumSquares += value * value;
                }
                double SqrtSquares = Math.Sqrt(sumSquares);
                return SqrtSquares;
            }

            public static double MMinusE(DataRow row, double E)
            {
                double sumSquares = 0.0;
                for (int i = 1; i < row.Table.Columns.Count; i++)
                {
                    double value = Convert.ToDouble(row[i]) - E;
                    sumSquares += value * value;
                }
                double SqrtSquares = Math.Sqrt(sumSquares);
                return SqrtSquares;
            }

            public static double MPlusE(DataRow row, double e)
            {
                double sumSquares = 0.0;
                for (int i = 1; i < row.Table.Columns.Count; i++)
                {
                    double value = Convert.ToDouble(row[i]) + e;
                    sumSquares += value * value;
                }
                double sqrtSquares = Math.Sqrt(sumSquares);
                return sqrtSquares;
            }
            //Программный код 14
            public DataTable ExponentialSmoothing(DataTable t, double A)
            {
                double prevM = Convert.ToDouble(t.Rows[0]["M"]);
                double currentM = 0.0;
                double forecastM = 0.0;
                double forecastAlpha = 0.0;
                double prevAlpha = Convert.ToDouble(t.Rows[0]["alpha"]);
                double currentAlpha = 0.0;
                double sumM = 0.0;
                double sumAlpha = 0.0;

                double prevMplus = Convert.ToDouble(t.Rows[0]["M+"]);
                double currentMplus = 0.0;
                double forecastMplus = 0.0;
                double forecastAlphaplus = 0.0;
                double prevAlphaplus = Convert.ToDouble(t.Rows[0]["alpha+"]);
                double currentAlphaplus = 0.0;
                double sumMplus = 0.0;
                double sumAlphaplus = 0.0;

                double prevMminus = Convert.ToDouble(t.Rows[0]["M-"]);
                double currentMminus = 0.0;
                double forecastMminus = 0.0;
                double forecastAlphaminus = 0.0;
                double prevAlphaminus = Convert.ToDouble(t.Rows[0]["alpha-"]);
                double currentAlphaminus = 0.0;
                double sumMminus = 0.0;
                double sumAlphaminus = 0.0;
                //Куча переменных... можно было этого избежать.

                // вычисляем сглаженные значения M и M+-
                for (int i = 1; i < t.Rows.Count - 1; i++)
                {
                    double MValue = Convert.ToDouble(t.Rows[i]["M"]);
                    currentM = A * MValue + (1 - A) * prevM;
                    t.Rows[i]["M"] = currentM;
                    prevM = currentM;
                    sumM += currentM;
                }

                for (int i = 1; i < t.Rows.Count - 1; i++)
                {
                    double MplusValue = Convert.ToDouble(t.Rows[i]["M+"]);
                    currentMplus = A * MplusValue + (1 - A) * prevMplus;
                    t.Rows[i]["M+"] = currentMplus;
                    prevMplus = currentMplus;
                    sumMplus += currentMplus;
                }
                for (int i = 1; i < t.Rows.Count - 1; i++)
                {
                    double MminusValue = Convert.ToDouble(t.Rows[i]["M-"]);
                    currentMminus = A * MminusValue + (1 - A) * prevMminus;
                    t.Rows[i]["M-"] = currentMminus;
                    prevMminus = currentMminus;
                    sumMminus += currentMminus;
                }

                double avgM = sumM / (t.Rows.Count - 2);
                double avgMplus = sumMplus / (t.Rows.Count - 2);
                double avgMminus = sumMminus / (t.Rows.Count - 2);

                // вычисляем сглаженные значения alpha
                for (int i = 1; i < t.Rows.Count - 1; i++)
                {
                    double alphaValue = Convert.ToDouble(t.Rows[i]["alpha"]);
                    currentAlpha = A * alphaValue + (1 - A) * prevAlpha;
                    t.Rows[i]["alpha"] = currentAlpha;
                    prevAlpha = currentAlpha;
                    sumAlpha += currentAlpha;
                }

                // вычисляем сглаженные значения alpha+
                for (int i = 1; i < t.Rows.Count - 1; i++)
                {
                    double alphaValueplus = Convert.ToDouble(t.Rows[i]["alpha+"]);
                    currentAlphaplus = A * alphaValueplus + (1 - A) * prevAlphaplus;
                    t.Rows[i]["alpha+"] = currentAlphaplus;
                    prevAlphaplus = currentAlphaplus;
                    sumAlphaplus += currentAlphaplus;
                }

                // вычисляем сглаженные значения alpha-
                for (int i = 1; i < t.Rows.Count - 1; i++)
                {
                    double alphaValueminus = Convert.ToDouble(t.Rows[i]["alpha-"]);
                    currentAlphaminus = A * alphaValueminus + (1 - A) * prevAlphaminus;
                    t.Rows[i]["alpha-"] = currentAlphaminus;
                    prevAlphaminus = currentAlphaminus;
                    sumAlphaminus += currentAlphaminus;
                }

                double avgAlpha = sumAlpha / (t.Rows.Count - 2);

                double avgAlphaplus = sumAlphaplus / (t.Rows.Count - 2);

                double avgAlphaminus = sumAlphaminus / (t.Rows.Count - 2);
                // вычисляем прогнозное значение M и альфы
                forecastM = A * avgM + (1 - A) * prevM;
                forecastAlpha = A * avgAlpha + (1 - A) * prevAlpha;

                forecastMminus = A * avgMminus + (1 - A) * prevMminus;
                forecastAlphaminus = A * avgAlphaminus + (1 - A) * prevAlphaminus;

                forecastMplus = A * avgMplus + (1 - A) * prevMplus;
                forecastAlphaplus = A * avgAlphaplus + (1 - A) * prevAlphaplus;
                // добавляем в таблицу прогнозное значение 0M и сглаженное значение alpha
                DataRow newRow = dt.NewRow();
                DataRow newRow1 = table1.NewRow();

                newRow[0] = "Прогноз";
                newRow[1] = forecastMplus;
                newRow[2] = forecastM;

                newRow[3] = forecastMminus;
                newRow[4] = forecastAlphaplus;

                newRow[5] = forecastAlpha;
                newRow[6] = forecastAlphaminus;

                newRow1[0] = "Прогноз";
                newRow1[1] = forecastMplus;
                newRow1[2] = forecastM;

                newRow1[3] = forecastMminus;
                newRow1[4] = forecastAlphaplus;

                newRow1[5] = forecastAlpha;
                newRow1[6] = forecastAlphaminus;

                dt.Rows.Add(newRow);
                table1.Rows.Add(newRow1);
                // возвращаем dt с прогнозом
                return dt;
            }
            //Программный код 16
            public void CreateColumnR(DataTable table)
            {
                table.Columns.Add("R", typeof(double));

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    double mPlus = Convert.ToDouble(table.Rows[i]["M+"]);
                    double mMinus = Convert.ToDouble(table.Rows[i]["M-"]);
                    double r = Math.Round(Math.Abs(mPlus - mMinus),5);
                    table.Rows[i]["R"] = r;
                }
            }
            //Программный Код 17
            public void CreateColumnL(DataTable table)
            {

                table.Columns.Add("L", typeof(string));
                table.Columns.Add("Состояние", typeof(string));
                double m0 = Convert.ToDouble(table.Rows[0]["M"]);

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    double m = Convert.ToDouble(table.Rows[i]["M"]);
                    double r = Convert.ToDouble(table.Rows[i]["R"]);
                    table.Rows[i]["L"] = Math.Abs(m - m0);
                    double l = Math.Abs(m - m0);
                    if (l < r)
                    {
                        table.Rows[i]["Состояние"] = "Не изменяемое";
                    }
                    else if (l > r)
                    {
                        table.Rows[i]["Состояние"] = "Аварийное";
                    }
                    else
                    {
                        table.Rows[i]["Состояние"] = "Предаварийное";
                    }
                }

            }
        }
        public class Lvl_2 : Lvl_1
        {
            public List<DataTable> tables;

            public Lvl_2()
            {
                tables = new List<DataTable>();
            }

            public void AddTablesFromDataSet(DataSet dataSet)
            {
                foreach (DataTable table in dataSet.Tables)
                {
                    if (!tables.Contains(table))
                    {
                        tables.Add(table);
                    }
                }
            }
            public bool CheckControlPoints(int totalPoints, ListBox listBox2, TextBox textbox, ComboBox comboBox)
            {
                int requiredPoints = totalPoints / Convert.ToInt32(textbox.Text);  // Рекомендуемое количество точек в каждом блоке
                int remainingPoints = totalPoints % Convert.ToInt32(textbox.Text);  // Количество оставшихся точек для распределения

                string selectedTableName = comboBox.SelectedItem.ToString();  // Имя выбранной таблицы
                DataTable table = tables.FirstOrDefault(t => t.TableName == selectedTableName);  // Поиск таблицы по имени
                                                                                                 // Проверка количества контрольных точек для текущего блока
                int blockPointsCount = table.Columns.Count - 1;
                if (remainingPoints == 0)
                {
                    return true;
                }
                if (blockPointsCount != requiredPoints)
                {
                    return false;
                }

                return true;  // Количество точек соответствует для данного блока
            }

            // Программный код 27
            public void CreateBlock(DataTable sourceTable, DataTable destinationTable)
            {
                foreach (DataRow sourceRow in sourceTable.Rows)
                {
                    DataRow newRow = destinationTable.NewRow();
                    foreach (DataColumn destinationColumn in destinationTable.Columns)
                    {
                        if (sourceTable.Columns.Contains(destinationColumn.ColumnName))
                        {
                            DataColumn sourceColumn = sourceTable.Columns[destinationColumn.ColumnName];
                            newRow[destinationColumn.ColumnName] = sourceRow[sourceColumn];
                        }
                    }

                    destinationTable.Rows.Add(newRow);
                }
            }

            public void RemoveTable(string tableName)
            {
                // Находим таблицу с указанным именем в списке таблиц
                DataTable tableToRemove = tables.Find(table => table.TableName == tableName);

                // Если таблица найдена, удаляем её из списка таблиц
                if (tableToRemove != null)
                {
                    tables.Remove(tableToRemove);
                }
            }
        }
        public class Lvl_3 : Lvl_2
        {
            private DataTable differenceTable;
            private DataTable lastTable;
            //Программный код 31
            public DataTable CreateDifferenceTable(DataTable sourceTable, ListBox listBox)
            {
                DataTable neededtable = new DataTable();
                differenceTable = new DataTable();
                neededtable.Columns.Clear();
                neededtable.Rows.Clear();

                differenceTable.Clear();
                foreach (object item in listBox.Items)
                {
                    neededtable.Columns.Add(item.ToString());
                }

                foreach (DataRow sourceRow in sourceTable.Rows)
                {
                    DataRow newRow = neededtable.NewRow();
                    foreach (DataColumn neededcol in neededtable.Columns)
                    {
                        if (sourceTable.Columns.Contains(neededcol.ColumnName))
                        {
                            DataColumn sourceColumn = sourceTable.Columns[neededcol.ColumnName];
                            newRow[neededcol.ColumnName] = sourceRow[sourceColumn];
                        }
                    }
                    neededtable.Rows.Add(newRow);
                }

                for (int i = 0; i < neededtable.Columns.Count - 1; i++)
                {
                    for (int j = i + 1; j < neededtable.Columns.Count; j++)
                    {
                        string columnName = $"{neededtable.Columns[i].ColumnName}-{neededtable.Columns[j].ColumnName}";
                        differenceTable.Columns.Add(columnName, typeof(double));
                    }
                }

                for (int k = 0; k < neededtable.Rows.Count; k++)
                {
                    DataRow newRow = differenceTable.NewRow(); // Создаем одну строку

                    for (int i = 0; i < neededtable.Columns.Count - 1; i++)
                    {
                        for (int j = i + 1; j < neededtable.Columns.Count; j++)
                        {
                            string columnName = $"{neededtable.Columns[i].ColumnName}-{neededtable.Columns[j].ColumnName}";
                            double value1 = Convert.ToDouble(neededtable.Rows[k][i]);
                            double value2 = Convert.ToDouble(neededtable.Rows[k][j]);
                            double difference = Math.Abs(value1 - value2);

                            newRow[columnName] = difference; // Обновляем значение в текущем столбце для строки newRow
                        }
                    }

                    differenceTable.Rows.Add(newRow);
                }

                return differenceTable;
            }
            //Программный код 32
            public DataTable CreateLastTable()
            {
                lastTable = new DataTable();
                lastTable.Rows.Add();
                foreach (DataColumn column in differenceTable.Columns)
                {
                    lastTable.Columns.Add(column.ColumnName, typeof(double));
                }

                for (int i = 0; i < differenceTable.Columns.Count; i++)//нулевую строку добавляем, в принципе можно и без нее 
                {
                    lastTable.Rows[0][i] = 0;
                }


                if (differenceTable.Rows.Count > 0)
                {
                    DataRow firstRow = differenceTable.Rows[0];
                    lastTable.NewRow();

                    for (int i = 1; i < differenceTable.Rows.Count; i++)
                    {
                        DataRow currentRow = differenceTable.Rows[i];
                        DataRow previousRow = differenceTable.Rows[i - 1];
                        DataRow newLastRow = lastTable.NewRow();

                        for (int j = 0; j < differenceTable.Columns.Count; j++)
                        {
                            double currentValue = Convert.ToDouble(currentRow[j]);
                            double firstValue = Convert.ToDouble(firstRow[j]);
                            double difference = Math.Abs(currentValue - firstValue);

                            newLastRow[j] = difference;
                        }

                        lastTable.Rows.Add(newLastRow);
                    }
                }

                return lastTable;
            }
            //Программный код 33
            public DataTable CreateConnectionsTable(double threshold)
            {
                DataTable connectionsTable = new DataTable("Связи");

                foreach (DataColumn column in lastTable.Columns)
                {
                    connectionsTable.Columns.Add(column.ColumnName, typeof(string));
                }

                if (lastTable.Rows.Count > 0)
                {
                    int maxIterations = 1001; // Максимальное количество итераций
                    int currentIteration = 0;

                    bool isLastRowValid = false;
                    DataRow lastConnectionsRow = null;

                    while (!isLastRowValid && currentIteration < maxIterations)
                    {
                        connectionsTable.Clear();

                        for (int i = 0; i < lastTable.Rows.Count; i++)
                        {
                            DataRow currentRow = lastTable.Rows[i];
                            DataRow newConnectionsRow = connectionsTable.NewRow();

                            for (int j = 0; j < lastTable.Columns.Count; j++)
                            {
                                double currentValue = Convert.ToDouble(currentRow[j]);
                                string connection = currentValue < threshold ? "+" : "-";

                                newConnectionsRow[j] = connection;
                            }

                            connectionsTable.Rows.Add(newConnectionsRow);
                        }

                        lastConnectionsRow = connectionsTable.NewRow();
                        int plusCount = 0;
                        int minusCount = 0;

                        for (int j = 0; j < lastTable.Columns.Count; j++)
                        {
                            var previousValues = lastTable.AsEnumerable().Take(lastTable.Rows.Count - 1).Select(r => Convert.ToDouble(r[j]));
                            bool hasPreviousValuesAboveThreshold = previousValues.Any(value => value > threshold);

                            if (j == lastTable.Columns.Count - 1)
                            {
                                if (hasPreviousValuesAboveThreshold)
                                {
                                    lastConnectionsRow[j] = "-";
                                    minusCount++;
                                }
                                else
                                {
                                    lastConnectionsRow[j] = "+";
                                    plusCount++;
                                }
                            }
                            else
                            {
                                string connection = hasPreviousValuesAboveThreshold ? "-" : "+";
                                lastConnectionsRow[j] = connection;

                                if (connection == "+")
                                    plusCount++;
                                else
                                    minusCount++;
                            }
                        }

                        if (Math.Abs(plusCount - minusCount) <= 1)
                            isLastRowValid = true;

                        currentIteration++;
                        threshold += 0.0001; // Изменение порогового значения в каждой итерации
                        if (currentIteration >= 500)
                        {
                            threshold -= 0.0001;
                        }
                    }
                    connectionsTable.Rows.Add(lastConnectionsRow);
                }

                return connectionsTable;
            }
        }
}