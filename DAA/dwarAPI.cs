﻿using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace DAA
{
    public static class DwarAPI
    {
        static CookieContainer cookie;
        public static string getMoney(HtmlNode node)
        {
            try
            {
                string money = "";
                string gold = HtmlEntity.DeEntitize(node.SelectSingleNode("descendant::span[@class='mgold']").InnerText).TrimStart();
                string silver = HtmlEntity.DeEntitize(node.SelectSingleNode("descendant::span[@class='msilver']").InnerText).TrimStart();
                string bronze = HtmlEntity.DeEntitize(node.SelectSingleNode("descendant::span[@class='mbronze']").InnerText).TrimStart();
                if (gold != "")
                    money += gold;
                if (silver != "")
                    if (silver.Length > 1)
                        money += silver;
                    else
                        if (gold == "")
                            money += silver;
                        else
                            money += '0' + silver;
                if (bronze.Length > 1)
                    money += bronze;
                else
                    money += '0' + bronze;
                return money;
            }
            catch (NullReferenceException e)
            {
                return "";
            }
        }

        public static int pageCount(HtmlAgilityPack.HtmlDocument doc)
        {
            try
            {
                return Convert.ToInt32(doc.DocumentNode.SelectSingleNode("(.//a[@class='pg-inact_lnk'])[last()]").InnerText);
            }
            catch (NullReferenceException)
            {
                return 0;
            }
        }

        public static void login()
        {
            try
            {
                cookie = new CookieContainer();
                string test = DwarRequest.postRequest("http://w1.dwar.ru/login.php", ref cookie, "email=zadisa2006@mail.ru&passwd=ee34nf3o&x=59&y=17");
                test = DwarRequest.getRequest("http://w1.dwar.ru/area_auction.php", ref cookie);

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(test);
                HtmlNode filter = doc.DocumentNode.SelectSingleNode("//select[@name='_filter[kind]']");
                HtmlNodeCollection categories = filter.SelectNodes(".//option");

                MySqlConnection connection = new MySqlConnection(@"server=localhost;userid=root;password=1547;Database=fordwar;charset=utf8");
                string dbCommand = "CREATE TABLE IF NOT EXISTS categories (browserValue NVARCHAR(10) PRIMARY KEY, categoryName NVARCHAR(50));";
                MySqlCommand command = new MySqlCommand(dbCommand, connection);
                connection.Open();
                command.ExecuteNonQuery();

                for (var i = 0; i < categories.Count; i++)
                {
                    command.CommandText = "REPLACE INTO categories (browserValue, categoryName) VALUES(@browserValue,@categoryName)";
                    command.Parameters.AddWithValue("@browserValue", categories[i].GetAttributeValue("value", ""));
                    if (command.Parameters[0].Value.ToString().Length < 10 && command.Parameters[0].Value != "")
                    {
                        command.Parameters.AddWithValue("@categoryName", HtmlAgilityPack.HtmlEntity.DeEntitize(categories[i].NextSibling.InnerText).Trim());
                        command.ExecuteNonQuery();
                    }
                    command.Parameters.Clear();
                }
                connection.Close();
                MessageBox.Show("Категории получены");
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }
        public static void addItems(List<HtmlNode> nodes, MySqlCommand command)
        {
            foreach (HtmlNode item in nodes)
            {
                try
                {
                    command.CommandText = command.CommandText + "('" + item.SelectSingleNode("td[7]/descendant::input[2]").GetAttributeValue("aid", "") + "','" + item.SelectSingleNode("td[2]/a").InnerText + "','"
                        + item.SelectSingleNode("td[2]/span[1]").InnerText.TrimStart() + "','" + item.SelectSingleNode("td[2]/span[2]").InnerText + "','" + item.SelectSingleNode("td[5]").InnerText + "','"
                        + DwarAPI.getMoney(item.SelectSingleNode("td[6]")) + "','" + DwarAPI.getMoney(item.SelectSingleNode("td[7]")) + "','" + DwarAPI.getMoney(item.SelectSingleNode("td[8]")) + "'),";
                }
                catch (NullReferenceException)
                {
                    //Здесь будет сообщение для логгера
                }
            }
        }
        public static List<HtmlNode> getItemNodes(HtmlAgilityPack.HtmlDocument doc)
        {
            HtmlNode itemList = doc.DocumentNode.SelectSingleNode(".//table[@id='item_list']");
            if (itemList != null)
                return itemList.Descendants("tr").Where(d => d.Attributes.Contains("class") && (d.Attributes["class"].Value.Contains("brd2-top"))).ToList<HtmlNode>();
            return null;
        }
        public static void scanItems()
        {
            try
            {
                MessageBox.Show("Сканирование начато");
                MySqlConnection connection = new MySqlConnection(@"server=localhost;userid=root;password=1547;Database=fordwar;charset=utf8");
                MySqlConnection connection2 = new MySqlConnection(@"server=localhost;userid=root;password=1547;Database=fordwar;charset=utf8");
                MySqlCommand command = new MySqlCommand();
                MySqlCommand command2 = new MySqlCommand();

                string html;

                connection.Open();
                connection2.Open();
                command.Connection = connection;
                command2.Connection = connection2;
                command2.CommandText = "CREATE TABLE IF NOT EXISTS items (lotID NVARCHAR(30) PRIMARY KEY, itemName NVARCHAR(50), itemCategory NVARCHAR(50), itemStrength NVARCHAR(10), itemCount NVARCHAR(10), pricePerPiece NVARCHAR(50), itemBid NVARCHAR(50), itemBuyOut NVARCHAR(50));";
                command2.ExecuteNonQuery();
                command.CommandText = "SELECT browserValue FROM categories";
                MySqlDataReader reader = command.ExecuteReader();
                command2.CommandText = "REPLACE INTO items (lotID, itemName, itemCategory, itemStrength, itemCount, pricePerPiece, itemBid, itemBuyOut) VALUES";

                while (reader.Read())
                {
                    html = DwarRequest.getRequest("http://w1.dwar.ru/area_auction.php?&_filter%5Btitle%5D=&_filter%5Bcount_min%5D=&_filter%5Bcount_max%5D=&_filter%5Blevel_min%5D=&_filter%5Blevel_max%5D=&_filter%5Bkind%5D=" + reader[0] + "&_filter%5Bquality%5D=-1&_filterapply=%D0%9E%D0%BA&page=0", ref cookie);
                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(html);
                    List<HtmlNode> items = getItemNodes(doc);
                    if (items == null)
                        continue;
                    int number = DwarAPI.pageCount(doc);

                    addItems(items, command2);

                    for (int i = 1; i < number; i++)
                    {
                        html = DwarRequest.getRequest("http://w1.dwar.ru/area_auction.php?&_filter%5Btitle%5D=&_filter%5Bcount_min%5D=&_filter%5Bcount_max%5D=&_filter%5Blevel_min%5D=&_filter%5Blevel_max%5D=&_filter%5Bkind%5D=" + reader[0] + "&_filter%5Bquality%5D=-1&_filterapply=%D0%9E%D0%BA&page=" + i, ref cookie);
                        doc = new HtmlAgilityPack.HtmlDocument();
                        doc.LoadHtml(html);

                        items = getItemNodes(doc);
                        addItems(items, command2);
                    }
                }
                command2.CommandText = command2.CommandText.TrimEnd(',') + ";";
                command2.ExecuteNonQuery();

                connection.Close();
                connection2.Close();
                reader.Close();
                MessageBox.Show("Сканирование завершено");
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                MessageBox.Show(exception.Data.Values.ToString());
            }
        }
    }
}

