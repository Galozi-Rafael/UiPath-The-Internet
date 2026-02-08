using FinanceApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FinanceApp
{
    public partial class Earning : Form
    {
        private int? editingId = null;
        public Earning()
        {
            InitializeComponent();
            LoadTableIncome();
            SelectTheMostCategoryIncomes();
            CalculateIncomeInThisMonth();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveIncomeDB();
            LoadTableIncome();
            clearInputData();
        }

        private void SaveIncomeDB()
        {
            string category = "";
            var checkedRadio = Array.Find(new[] { radioButtonCar, radioButtonQA }, rb => rb.Checked);
            category = checkedRadio?.Text ?? "";

            using (var db = new AppDbContext())
            {
                if (editingId.HasValue)
                {
                    var income = db.Income.Find(editingId.Value);
                    if(income != null)
                    {
                        income.Category = category;
                        income.Description = DescriptionTextBox.Text.Trim();
                        income.Price = SumNumericUpDown.Value;
                        income.Date = monthCalendar.SelectionStart;
                    }
                    
                }
                else
                {
                    db.Income.Add(new Income
                    {
                        Category = category,
                        Description = DescriptionTextBox.Text.Trim(),
                        Price = SumNumericUpDown.Value,
                        Date = monthCalendar.SelectionStart
                    });
                }                    
                db.SaveChanges();
                SaveButton.Text = "Записати";
                editingId = null;
            }         
        }
        private void LoadTableIncome()
        {
            var time = DateTime.Today;
            Sql.LoadTableFilter(db => db.Income, e => e.Date.Year == time.Year && e.Date.Month == time.Month, TableIncome);

        }

        private void clearInputData()
        {
            DescriptionTextBox.Text = string.Empty;
            SumNumericUpDown.Value = 0;
            monthCalendar.SelectionStart = DateTime.Today;
        }

        private void SelectTheMostCategoryIncomes()
        {
            var categorySums = TableIncome.Rows
                .Cast<DataGridViewRow>()
                .Where(r => r.Cells["Category_Column"].Value != null &&
                            r.Cells["Earning_Column"].Value != null)
                .GroupBy(r => r.Cells["Category_Column"].Value.ToString())
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(r => Convert.ToDecimal(r.Cells["Earning_Column"].Value))
                })
                .ToList();

            if (categorySums.Count == 0)
            {
                label8.Text = "Немає даних";
                return;
            }

            var maxCategory = categorySums
                .OrderByDescending(x => x.Total)
                .First();

            label8.Text = $"{maxCategory.Category}: {maxCategory.Total:0.00} zł";
        }

        private void CalculateIncomeInThisMonth()
        {
            decimal total = TableIncome.Rows
                .Cast<DataGridViewRow>()
                .Where(r => r.Cells["Earning_Column"].Value != null &&
                            r.Cells["Earning_Column"].Value.ToString() != "")
                .Sum(r => Convert.ToDecimal(r.Cells["Earning_Column"].Value));

            label7.Text = total.ToString("0.00");
        }

        private void TableIncome_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == TableIncome.Columns["DeleteColumn"].Index && e.RowIndex >= 0)
            {
                int id = (int)TableIncome.Rows[e.RowIndex].Cells["IdColumn"].Value;

                if (MessageBox.Show("Видалити запис?", "Підтвердження",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    Sql.DeleteRecordDb<Income>(id);
                    LoadTableIncome();
                }
            }
            else if (e.ColumnIndex == TableIncome.Columns["EditColumn"].Index && e.RowIndex >= 0)
            {
                int id = (int)TableIncome.Rows[e.RowIndex].Cells["IdColumn"].Value;
                using (var db = new AppDbContext())
                {
                    var income = db.Income.Find(id);
                    if (income != null)
                    {
                        editingId = id; // <-- ПОМИТАЄМО ЯКИЙ РЕДАГУЄМО
                        radioButtonCar.Checked = income.Category == "Автоелектрика";

                            DescriptionTextBox.Text = income.Description;
                        SumNumericUpDown.Value = income.Price;
                        monthCalendar.SelectionStart = income.Date;
                    }
                }
                SaveButton.Text = "Оновити";
            }
        }
    }
    
}