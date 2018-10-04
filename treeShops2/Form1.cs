using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Data.SQLite;
using System.IO;

namespace treeShops2
{
    public partial class Form1 : Form
    {
        private String dbFileName = "def.db";
        private SQLiteConnection db;
        private SQLiteCommand sqlCmd;
        
        int i = 0;

        public Form1()
        {
            InitializeComponent();

            // подключение бд
            db = new SQLiteConnection("Data Source=" + dbFileName + ";");
            sqlCmd = new SQLiteCommand();

            if (!File.Exists(dbFileName))
                SQLiteConnection.CreateFile(dbFileName);
            db.Open();

            // создание древовидной таблицы
            sqlCmd.Connection = db;
            sqlCmd.CommandText = "drop table tree;" +
                                  "create table tree (" +
                                    "id integer, " +
                                    "parent integer, " +
                                    "name text, " +
                                    "discount decimal(7), " +
                                    "dependency bool, " +
                                    "description varchar(124), " +
                                    "primary key(id), " +
                                    "foreign key(parent) references tree(id) " +
                                    "on update cascade on delete cascade" +
                                    "); ";
            sqlCmd.ExecuteNonQuery();
            // создание таблицы конечных скидок
            
            sqlCmd.CommandText = "drop table if exists totaldiscount;" +
                                 "create table totaldiscount(id integer, parent integer, discount integer, parentNames text);";
            sqlCmd.ExecuteNonQuery();
            sqlCmd.CommandText = "drop table if exists totaldiscounts;" +
                                 "create table totaldiscounts(id integer, parent integer, discount integer, totaldisc text, pnames text);";
            sqlCmd.ExecuteNonQuery();
            // заполнение таблицы магазинами
            try
            {
                sqlCmd.CommandText = "delete from tree;";
                sqlCmd.ExecuteNonQuery();
                sqlCmd.CommandText = "INSERT INTO tree (id, parent, name, discount, dependency)" +
                                       "values (1, null, 'Миасс', 4, false);";
                sqlCmd.ExecuteNonQuery();
                sqlCmd.CommandText = "INSERT INTO tree (id, parent, name, discount, dependency)" +
                                       "values (2, 1, 'Амелия', 5, true);";
                sqlCmd.ExecuteNonQuery();
                sqlCmd.CommandText = "INSERT INTO tree (id, parent, name, discount, dependency)" +
                                       "values (3, 2, 'Тест1', 2, true);";
                sqlCmd.ExecuteNonQuery();
                sqlCmd.CommandText = "INSERT INTO tree (id, parent, name, discount, dependency)" +
                                       "values (4, 1, 'Тест2', 0, true);";
                sqlCmd.ExecuteNonQuery();
                sqlCmd.CommandText = "INSERT INTO tree (id, parent, name, discount, dependency)" +
                                       "values (5, null, 'Курган', 11, false);";
                sqlCmd.ExecuteNonQuery();
            }
            catch (SQLiteException exc)
            {
                MessageBox.Show("Error:" + exc.Message);
            }
            
            // заполнение таблицы с конечными скидками
            sqlCmd.CommandText = "delete from totaldiscount;";
            sqlCmd.ExecuteNonQuery();
            sqlCmd.CommandText = "with recursive recurs  " +
                                "as (select id, parent, discount, '/' || name as name " +
                                "from tree " +
                                "where parent is null " +
                                "union all " +
                                "select tree.id, tree.parent, recurs.discount + tree.discount, recurs.name || '/' || tree.name " +
                                "from tree " +
                                "inner join recurs on recurs.id = tree.parent) " +
                                "insert into totaldiscount(id, parent, discount, parentNames ) select id, parent, discount, name from recurs ";
            sqlCmd.ExecuteNonQuery();

            // заполнение таблицы со списком скидок и родителей
            sqlCmd.CommandText = "delete from totaldiscounts;";
            sqlCmd.ExecuteNonQuery();
            sqlCmd.CommandText = "with recursive recurs  " +
                                "as (select id, parent, discount, discount as name, '/' || name as pname " +
                                "from tree " +
                                "where parent is null " +
                                "union all " +
                                "select tree.id, tree.parent, tree.discount, recurs.name || '+' || tree.discount, recurs.pname || '/' || tree.name " +
                                "from tree " +
                                "inner join recurs on recurs.id = tree.parent) " +
                                "insert into totaldiscounts(id, parent, discount, totaldisc, pnames ) select id, parent, discount, name, pname from recurs ";
            sqlCmd.ExecuteNonQuery();
            // заполнение промежуточной таблицы
            DataTable dTable = new DataTable();
            SQLiteDataAdapter adapter = new SQLiteDataAdapter("select id, totaldisc, pnames from totaldiscounts", db);
            adapter.Fill(dTable);
            dataGridView2.Rows.Clear();
            for (int i = 0; i < dTable.Rows.Count; i++)
                dataGridView2.Rows.Add(dTable.Rows[i].ItemArray);
            db.Close();

            // получение древовидной структуры
            GetNodes(TreeView1.Nodes, 0);

            dataGridView1[0,0].Selected = true;
        }

        // рекурсивная функция, 
        // изначально i=0, 
        // при совпадении уровня вложенности level и предка добавляется дочерний узел
        private void GetNodes(TreeNodeCollection nodes, int level)
        {
            while (i < dataGridView2.Rows.Count-1)
            {
                string node = (string)dataGridView2[2, i].Value;
                if (level == GetLevel(node))
                {
                    nodes.Add(GetNodeName(node, level));
                    i++;
                    continue;
                }
                if (level > GetLevel(node)) return;
                if (level < GetLevel(node))
                {
                    foreach (TreeNode item in nodes)
                    {
                        if (item.Text == GetParent(node))
                            GetNodes(item.Nodes, level + 1);
                    }
                }
            }
        }

        // получение имени предка
        private string GetParent(string s)
        {
            int ind = s.LastIndexOf('/');
            s = s.Substring(0, ind);
            ind = s.LastIndexOf('/');
            s = s.Substring(ind+1);
            return s;
        }
        // получение уровня узла
        private int GetLevel(string s)
        {
            int level = -1;
            for (int i = 0; i < s.Length; i++)
                if (s[i] == '/')
                    level++;
            return level;
        }
        // получение имени узла
        private string GetNodeName (string s, int level)
        {
            for (int i = -1; i < level; i++)
            {
                int index = s.IndexOf('/');
                s = s.Substring(index+1);
            }
            return s;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        // событие кнопки "Расчет"
        private void button1_Click_1(object sender, EventArgs e)
        {
            // подключение к бд
            db = new SQLiteConnection("Data Source=" + dbFileName + ";");
            sqlCmd = new SQLiteCommand();
            if (!File.Exists(dbFileName))
                SQLiteConnection.CreateFile(dbFileName);
            db.Open();
            sqlCmd.Connection = db;
            // чистка таблица расчетов
            dataGridView3.Rows.Clear();
            for (int i = 0; i < dataGridView1.Rows.Count-1; i++)
            {
                // получение конечной скидки по имени
                sqlCmd.CommandText = "select id from tree where name = @name";
                sqlCmd.Parameters.AddWithValue("@name", (string)dataGridView1[0, i].Value);
                SQLiteDataReader reader = sqlCmd.ExecuteReader();
                try
                {
                    reader.Read();
                    int id = Convert.ToInt32(reader["id"]);
                    reader.Close();
                    sqlCmd.CommandText = "select discount from totaldiscount where id = @id";
                    sqlCmd.Parameters.AddWithValue("@id", id);
                    reader = sqlCmd.ExecuteReader();
                    reader.Read();
                    double totaldiscount = Convert.ToDouble(reader["discount"]);
                    reader.Close();
                    // получение цены
                    double price = Convert.ToDouble(dataGridView1[1, i].Value);
                    // расчет
                    double res = price - price * totaldiscount / 100;
                    // получение списка скидок
                    string totalDiscountList ="";
                    for (int j = 0; j < dataGridView2.Rows.Count; j++)
                    {
                        if (id == Convert.ToInt32(dataGridView2[0, j].Value))
                        {
                            totalDiscountList = (string)dataGridView2[1, j].Value;
                            break;
                        }
                    }
                    // список скидок родителей
                    string discountParent = "";
                    // скидка в выбраном магазине
                    string discountMarket = totalDiscountList;
                    if (totalDiscountList.LastIndexOf('+') > 0)
                    {
                        discountParent = totalDiscountList.Substring(0, totalDiscountList.LastIndexOf('+'));
                        discountMarket = totalDiscountList.Remove(0, totalDiscountList.LastIndexOf('+') + 1);
                    }
                    // заполнение таблицы расчетов
                    dataGridView3.Rows.Add(price, discountMarket, discountParent, res);
                }
                catch (SQLiteException exc)
                {
                    MessageBox.Show("Error: " + exc);
                }
                catch (SystemException sexc)
                {
                    MessageBox.Show("Error: Выберите город перед расчетом");
                }
            }
            db.Close();
        }
        // событие мыши, при выборе узла, имя магазина заполняется в таблице
        private void TreeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            dataGridView1[0, dataGridView1.SelectedCells[0].RowIndex].Value = e.Node.Text;
        }
    }
}
