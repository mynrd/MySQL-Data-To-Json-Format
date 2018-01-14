using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;

using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MySQLDataToJson
{
    public partial class frmMain : Form
    {
        private MySqlConnection _connection;
        private string CurrentFilePath = Environment.CurrentDirectory;

        public frmMain()
        {
            InitializeComponent();
            txtServer.Text = "localhost";
            txtUsername.Text = "root";
            lvColumns.DoubleClick += lvColumns_DoubleClick;
        }

        private void btnValidate_Click(object sender, EventArgs e)
        {
            try
            {
                string connstring = $"Server={txtServer.Text}; database={txtDatabase.Text}; UID={txtUsername.Text}; password={txtPassword.Text}";
                _connection = new MySqlConnection(connstring);
                _connection.Open();

                this.LoadTable();

                MessageBox.Show("You are now connected, table list is available now.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTable()
        {
            lbTableList.Items.Clear();
            if (_connection.State != ConnectionState.Open) throw new Exception("MySql Connection is currently close");

            using (var adapter = new MySqlDataAdapter(new MySqlCommand()
            {
                CommandText = "show tables",
                Connection = _connection
            }))
            {
                var dt = new DataTable("tableList");
                adapter.Fill(dt);

                foreach (DataRow dr in dt.Rows)
                {
                    lbTableList.Items.Add(dr[0].ToString());
                }

                btnConvert.Enabled = dt.Rows.Count != 0;
            }
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            try
            {
                using (var adapter = new MySqlDataAdapter(new MySqlCommand()
                {
                    CommandText = "select * from " + lbTableList.Text,
                    Connection = _connection
                }))
                {
                    var dt = new DataTable(lbTableList.Text);
                    adapter.Fill(dt);

                    var columns = lvColumns.Items.Cast<ListViewItem>().Select(x => new FieldStructure()
                    {
                        FieldName = x.Text,
                        UpdatedFieldName = x.SubItems[1].Text,
                    });
#if DEBUG
                    foreach (var item in columns)
                    {
                        Debug.WriteLine($"{item.FieldName} - {item.UpdatedFieldName}");
                    }
#endif
                    var listColumnToBeRemove = new List<DataColumn>();

                    var validColumns = columns.Where(x => !string.IsNullOrWhiteSpace(x.UpdatedFieldName)).ToList();

                    foreach (DataColumn dc in dt.Columns)
                    {
                        var col = validColumns.FirstOrDefault(x => x.FieldName == dc.ColumnName);
                        if (col != null)
                        {
                            dc.ColumnName = col.UpdatedFieldName;
                            col.FieldDataType = dc.DataType.ToString();
                        }
                        else
                        {
                            listColumnToBeRemove.Add(dc);
                        }
                    }

                    foreach (var dc in listColumnToBeRemove)
                    {
                        dt.Columns.Remove(dc);
                    }
                    foreach (DataRow dr in dt.Rows)
                    {
                        foreach (var dc in validColumns)
                        {
                            if (dr[dc.UpdatedFieldName] == null && dc.FieldDataType == typeof(string).ToString())
                            {
                                dr[dc.UpdatedFieldName] = "";
                            }
                        }
                    }
                    txtResult.Text = JsonConvert.SerializeObject(dt);

                    File.WriteAllText(Path.Combine(this.CurrentFilePath, $"{txtDatabase.Text}-{lbTableList.Text.ToLower()}.fieldconfig"), JsonConvert.SerializeObject(columns));
                    var fileSave = new SaveFileDialog() { DefaultExt = ".json" };
                
                    if (fileSave.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(fileSave.FileName, txtResult.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lbTableList_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string query = "select column_name from information_schema.columns where table_name = '" + lbTableList.Text + "'";
                using (var adapter = new MySqlDataAdapter(new MySqlCommand()
                {
                    CommandText = query,
                    Connection = _connection
                }))
                {
                    var dt = new DataTable(lbTableList.Text);
                    adapter.Fill(dt);
                    var dtPreviousSetup = GetPreviousSetup(lbTableList.Text);
                    lvColumns.Items.Clear();
                    foreach (DataRow dr in dt.Rows)
                    {
                        string columnName = dr[0].ToString();
                        ListViewItem lv = lvColumns.Items.Add(columnName);

                        var prevConfig = dtPreviousSetup.FirstOrDefault(x => x.FieldName == columnName);

                        lv.SubItems.Add(prevConfig != null ? prevConfig.UpdatedFieldName : "");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<FieldStructure> GetPreviousSetup(string text)
        {
            var list = new List<FieldStructure>();

            try
            {
                string readColumns = File.ReadAllText(Path.Combine(this.CurrentFilePath, $"{txtDatabase.Text}-{lbTableList.Text.ToLower()}.fieldconfig"));

                var data = JsonConvert.DeserializeObject<IEnumerable<FieldStructure>>(readColumns);
                list = data.ToList();
            }
            catch { }

            return list;
        }

        private void lvColumns_DoubleClick(object sender, EventArgs e)
        {
            if (lvColumns.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select table to list all available column or double click the columns you want to update");
                return;
            };
            var f = new frmEditName(lvColumns.FocusedItem.SubItems[1].Text);
            if (f.ShowDialog() == DialogResult.OK)
            {
                lvColumns.FocusedItem.SubItems[1].Text = f.FieldName;
            }
        }
    }
}