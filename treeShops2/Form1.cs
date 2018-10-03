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

        private String dbStatus;

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
            
            sqlCmd.CommandText = "drop table totaldiscount;" +
                                 "create table totaldiscount(id integer, parent integer, discount integer, parentNames text);";
            sqlCmd.ExecuteNonQuery();


            dbStatus = "Connected";
            label1.Text = dbStatus;

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

            // заполнение списка магазинов
            sqlCmd.CommandText = "select name, id, parent from tree";
            SQLiteDataReader reader2 = sqlCmd.ExecuteReader();
            
            List<TreeNode> nodes = new List<TreeNode>();
            int j = 0;
            while (reader2.Read())
            {
                name.Items.Add(Convert.ToString(reader2["name"]));

                nodes.Add(new TreeNode((string)reader2["name"]));
                nodes[j].Tag = Convert.ToInt32(reader2["id"]);
                j++;
            }

            reader2.Close();

            



            // вывод таблицы конечных скидок
            DataTable dTable = new DataTable();
            SQLiteDataAdapter adapter = new SQLiteDataAdapter("select id, discount, parentNames from totaldiscount", db);
            adapter.Fill(dTable);
            dataGridView2.Rows.Clear();
            label1.Text = dTable.Rows.Count.ToString();
            for (int i = 0; i < dTable.Rows.Count; i++)
                dataGridView2.Rows.Add(dTable.Rows[i].ItemArray);
            db.Close();


            GetNodes(TreeView1.Nodes, 0);
        }

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
                    //GetNodes(nodes[nodes.Count - 1].Nodes, level + 1);

                }
            }
        }

        private string GetParent(string s)
        {
            int ind = s.LastIndexOf('/');
            s = s.Substring(0, ind);
            ind = s.LastIndexOf('/');
            s = s.Substring(ind+1);
            return s;
        }
        private int GetLevel(string s)
        {
            int level = -1;
            for (int i = 0; i < s.Length; i++)
                if (s[i] == '/')
                    level++;
            return level;
        }

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
            TreeView1.AfterSelect += TreeView1_AfterSelect;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            

            
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows.Count-1; i++)
            {
                db = new SQLiteConnection("Data Source=" + dbFileName + ";");
                sqlCmd = new SQLiteCommand();

                if (!File.Exists(dbFileName))
                    SQLiteConnection.CreateFile(dbFileName);
                db.Open();
                sqlCmd.Connection = db;

                sqlCmd.CommandText = "select id from tree where name = @name";
                sqlCmd.Parameters.AddWithValue("@name", (string)dataGridView1[0, i].Value);
                SQLiteDataReader reader = sqlCmd.ExecuteReader();
                reader.Read();
                int id = Convert.ToInt32(reader["id"]);
                reader.Close();
                sqlCmd.CommandText = "select discount from totaldiscount where id = @id";
                sqlCmd.Parameters.AddWithValue("@id", id);
                reader = sqlCmd.ExecuteReader();
                reader.Read();
                double discount = Convert.ToDouble(reader["discount"]);
                reader.Close();
                double price = Convert.ToDouble(dataGridView1[1, i].Value);
                double res = price - price * discount / 100;
                
                dataGridView1.Rows[i].SetValues(dataGridView1[0, i].Value, price, res);
                db.Close();
            }
        }
        private void TreeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            dataGridView1[0, dataGridView1.Rows.Count - 1].Value = e.Node.Text;
        }
        private void TreeView1_AfterSelect(System.Object sender, TreeViewEventArgs e)
        {

            // Vary the response depending on which TreeViewAction
            // triggered the event. 
            switch ((e.Action))
            {
                case TreeViewAction.ByKeyboard:
                    MessageBox.Show("You like the keyboard!");
                    break;
                case TreeViewAction.ByMouse:
                    MessageBox.Show("You like the mouse!");
                    break;
            }
        }

        private void TreeView1_AfterSelect_1(object sender, TreeViewEventArgs e)
        {
            dataGridView1[0, dataGridView1.Rows.Count - 1].Value = e.Node.Text;
        }
    }
}
