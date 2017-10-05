using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Alfa_CashBack_Calc
{
    public partial class Form1 : Form
    {
        public string filepath = "";

        public Form1()
        {
            InitializeComponent();
            InitializeComboBox();

            // Определяем текущий год и месяц
            int current_year = DateTime.Now.Year;
            while(2009<current_year)
                {
                comboBox2.Items.Add(current_year);
                current_year--;
                }
            comboBox2.SelectedIndex = 0;

            int select_mon = DateTime.Now.Month;
            comboBox1.SelectedIndex = select_mon-1;
        }

       
        // Автоматически пересчитываем статистику при изменении месяца, года или номера карты
        private void InitializeComboBox()
        {
            this.comboBox1.SelectedIndexChanged +=
                new System.EventHandler(ComboBox1_SelectedIndexChanged);
            this.comboBox2.SelectedIndexChanged +=
                new System.EventHandler(ComboBox1_SelectedIndexChanged);
            this.comboBox3.SelectedIndexChanged +=
                new System.EventHandler(ComboBox1_SelectedIndexChanged);
        }

        private void ComboBox1_SelectedIndexChanged(object sender,
        System.EventArgs e)
        {
            if (filepath != "") {calc_cashback(filepath); }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Загружаем выписку для анализа
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Выписка в формате CSV|*.csv";
            System.Windows.Forms.DialogResult dr = ofd.ShowDialog();
            if (dr == DialogResult.OK)
                {
                filepath = ofd.FileName;
                toolStripStatusLabel1.ForeColor = Color.Black;
                toolStripStatusLabel1.Text = "Загружен файл: " + System.IO.Path.GetFileName(filepath);
                get_cardnumbers(filepath); // Находим все номера банковских карт, которые присутствуют в выписке
                calc_cashback(filepath); // Расчет кэшкэка        
                }
            button1.Text = "Выбрать другой файл";
        }

        // Узнаем какие в файле использовались номера карт
        public void get_cardnumbers(string filepath)
        {
            string[] data = File.ReadAllLines(filepath);
            int str_count = data.Length;

            List<string> cardlist = new List<string>();
            cardlist.Clear();
            comboBox3.Items.Clear();

            int i = 0;
            while (i < str_count)
            {
                // Находим в выписке строки с MCC
                String[] line = data[i].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                var mcc_code_full = "";
                if (line.Length < 7) {
                    toolStripStatusLabel1.Text = "Некорректный формат файла";
                    toolStripStatusLabel1.ForeColor = Color.Red;
                    label5.Text = "";
                    label1.Text = "";
                    comboBox3.Enabled = false;
                    return;}

                mcc_code_full = line[5].Substring(line[5].Length - 7);
                if (mcc_code_full.Remove(3) == "MCC")
                {
                    // Определяем номер карты   
                    cardlist.Add(line[5].Remove(16));
                }
                i++;
            }

            comboBox3.Enabled = true;
            cardlist = cardlist.Distinct().ToList();
            // Выводим список картв списком
            i = 0;
            int card_count = cardlist.Count;
            while (i < card_count)
                {
                comboBox3.Items.Add(cardlist[i]);
                i++;
                }
            if (comboBox3.Items.Count > 0) { comboBox3.SelectedIndex = 0; } else { comboBox3.Text = ""; }
        }

        public void calc_cashback(string filepath)
        {
            int current_month = comboBox1.SelectedIndex + 1;
            string current_year = comboBox2.Text; 
            int year_index = Int32.Parse(current_year);
            year_index = year_index - 2000; // отрезаем 20, чтобы получить год в формате гг:)
            string filter = current_month.ToString() + "." + year_index.ToString(); // Строка по которой мы будем определять транзакции совершенные в выбранном месяце.

            string current_month_reverse;
            if (current_month.ToString().Length < 2) { current_month_reverse = "0" + current_month.ToString(); } else { current_month_reverse = current_month.ToString(); }
            string filter_reverse = year_index.ToString() + "." + current_month_reverse; // У операций в состоянии HOLD другой формат даты, поэтому делаем для них другой фильтр.

            // массив с соотношением код МСС - размер кэшбэка
            int[] mcc_coef = new int[9999];
            // нет кэшбэка
            mcc_coef[4812] = -1;
            mcc_coef[4814] = -1;
            mcc_coef[5968] = -1;
            mcc_coef[6050] = -1;
            mcc_coef[6051] = -1;
            mcc_coef[6529] = -1;
            mcc_coef[6530] = -1;
            mcc_coef[7311] = -1;
            mcc_coef[7399] = -1;
            mcc_coef[7511] = -1;
            mcc_coef[7995] = -1;
            mcc_coef[6540] = -1;
            mcc_coef[4829] = -1;
            mcc_coef[6532] = -1;
            mcc_coef[6010] = -1;
            mcc_coef[6011] = -1;
            mcc_coef[6012] = -1;
            mcc_coef[6534] = -1;
            mcc_coef[6536] = -1;
            mcc_coef[6537] = -1;
            mcc_coef[6538] = -1;
            // АЗС
            mcc_coef[5541] = 10;
            mcc_coef[5542] = 10;
            mcc_coef[9752] = 10;
            mcc_coef[5983] = 10;
            // Кафе и рестораны
            mcc_coef[5812] = 5;
            mcc_coef[5813] = 5;
            mcc_coef[5814] = 5;
            mcc_coef[5811] = 5;

            // Загружаем данные для анализа
            string[] data = File.ReadAllLines(filepath);
            int str_count = data.Length;

            double total_money=0;
            double total_hold_money=0;
            double cash_value;
            double hold_money;
            double total_hold_money_back;
            var mcc_code="";
            var mcc_code_full = "";
            int coef;
            string paid_date;
            Match match;
            String[] line;

            var selected_card = comboBox3.Text;
            try
            {
                int i = 0;
                while (i < str_count)
                {
                    // Находим в выписке строки с MCC
                    line = data[i].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                   
                    if (line.Length < 7) {
                        toolStripStatusLabel1.Text = "Некорректный формат файла";
                        toolStripStatusLabel1.ForeColor = Color.Red;
                        label5.Text = "";
                        label1.Text = "";
                        comboBox3.Enabled = false;
                        return; }
                        mcc_code_full = line[5].Substring(line[5].Length - 7);
                    if (mcc_code_full.Remove(3) == "MCC")
                    {
                        // Определяем MCC
                        mcc_code = mcc_code_full.Remove(0, 3);
                        if (line[5].Remove(16) == selected_card) // Если номер карты совпадает с тем, что выбран в настройках - едем дальше
                        {
                            // Находим дату совершения транзакции(а не дату списания).
                            match = Regex.Match(line[5], @"\d \d\d[.]\d\d[.]\d\d");
                            paid_date = match.Captures[0].Value.Remove(0, 2);

                            // Если транзакция попадает в выбранный месяц, то считаем кэшбэк
                            if (paid_date.Contains(filter))
                            {
                                coef = mcc_coef[Int32.Parse(mcc_code)];
                                if (coef == 0) { coef = 1; }
                                if (coef == -1) { coef = 0; }
                                cash_value = Double.Parse(line[7]) * coef / 100;
                                total_money = total_money + cash_value;
                            }
                        }
                    }
                    else // Если у транзакции еще нет MCC, то вероятно это непроведенная транзакция. Посчитаем примерный размер ожидаемого кэшбэка.
                    {
                        if (line[4] == "HOLD")
                        {
                            if (line[5].Contains(selected_card) && line[3].Contains(filter))
                            {
                                line = line[5].Split(new string[] { filter_reverse }, StringSplitOptions.RemoveEmptyEntries);
                                line = line[2].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                double.TryParse(line[1], System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out hold_money);
                                total_hold_money = total_hold_money + hold_money;
                            }
                        }
                    }
                    i++;
                }
            }
            catch (Exception)
                {
                toolStripStatusLabel1.Text = "Некорректный формат файла";
                toolStripStatusLabel1.ForeColor = Color.Red;
                label5.Text = "";
                label1.Text = "";
                comboBox3.Enabled = false;
                return;
            }

                total_money = Math.Round(total_money, 2); ;
                label1.Text = "Кэшбэк " + total_money.ToString() + " руб.";

                if (total_hold_money > 0)
                {
                    total_hold_money_back = total_hold_money / 100;
                    total_hold_money_back = Math.Round(total_hold_money_back, 2); ;
                    label5.Text = "Непроведенных транзакций на:\r\n" + total_hold_money.ToString() + " руб.    Кэшбэк: ~" + total_hold_money_back.ToString() + " руб.";
                }
                else { label5.Text = ""; }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form aboutform = new Form2();
            aboutform.Show(); // Отобразим окно с информацией о программе
        }
    }
}
