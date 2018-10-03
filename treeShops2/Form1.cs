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
using System.Data;

namespace treeShops2
{
    public partial class Form1 : Form
    {
        private String dbFileName = "def.db";
        private SQLiteConnection db;
        private SQLiteCommand sqlCmd;

        private String dbStatus;

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
                                 "create table totaldiscount(id integer, discount integer, parentNames text);";
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
                                "as (select id, parent, discount, name || '/' as name " +
                                "from tree " +
                                "where parent is null " +
                                "union all " +
                                "select tree.id, tree.parent, recurs.discount + tree.discount, recurs.name || tree.name || '/' " +
                                "from tree " +
                                "inner join recurs on recurs.id = tree.parent) " +
                                "insert into totaldiscount(id, discount, parentNames ) select id, discount, name from recurs ";
            sqlCmd.ExecuteNonQuery();

            // вывод таблицы конечных скидок
            DataTable dTable = new DataTable();
            SQLiteDataAdapter adapter = new SQLiteDataAdapter("select * from totaldiscount", db);
            adapter.Fill(dTable);
            dataGridView2.Rows.Clear();
            label1.Text = dTable.Rows.Count.ToString();
            for (int i = 0; i < dTable.Rows.Count; i++)
                dataGridView2.Rows.Add(dTable.Rows[i].ItemArray);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            

            
        }
    }
}
