﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Linq;
using MongoDB;
using MongoDB.Bson.Serialization.Attributes;
using Word = Microsoft.Office.Interop.Word;
using System.Globalization;
using System.Diagnostics;

namespace GH_IT_Project
{
    /// <summary>
    /// Download 的摘要描述
    /// </summary>
    public class Download : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            CloseWINWORD_Porcesses();
            DeleteDOC(context, @"C:\Generate_Temp\");
            //接參數的index
            string Filename = "派工單" + DateTime.Now.ToString("yyyy-MM-dd(HH點mm分)") + ".doc";
            var Parameter = context.Request.Params["Parameter"];
            //MongoDB搜尋
            string[] Split_Para = Parameter.Split(',');


            //StreamWriter sw = new StreamWriter(@"C:\Users\info\source\repos\GH_IT_Project\GH_IT_Project\File\text.txt", false);
            //第二個參數設定為true表示不覆蓋原本的內容，把新內容直接添加進去

            List<List<plumber_table>> entity = new List<List<plumber_table>>();
            List<List<plumber_table>> Sorting_Data = new List<List<plumber_table>>();
            List<CountNUnit> Count_Number = new List<CountNUnit>();
            plumber_table pt = new plumber_table();

            MongoDB_connection MDBC = new MongoDB_connection();
            var database = MDBC.MongoDB("plumber_table");
            var collection_out = database.GetCollection<plumber_table>("plumber_table");
            for (int i = 0; i < Split_Para.Length; i++)
            {
                List<plumber_table> List_Temp = new List<plumber_table>();
                var filter_id = Builders<plumber_table>.Filter.Eq("id", ObjectId.Parse(Split_Para[i]));
                entity.Add(collection_out.Find(filter_id).ToList().Select(s => new plumber_table
                {
                    Unit = s.Unit,
                    reporter = s.reporter,
                    fix_item = s.fix_item,
                    location = s.location,
                    remark = s.remark,
                    time = s.time,
                }).ToList());
            }

            List<string> Find_Unit = new List<string>();
            List<string> Find_Unit_Word = new List<string>();
            for (int i = 0; i < entity.Count; i++)//找出組室
            {
                for (int j = 0; j < entity[i].Count; j++)
                {
                    if (Find_Unit.Exists(x => x == entity[i][j].Unit) != true)
                    {
                        Find_Unit.Add(entity[i][j].Unit);
                        Find_Unit_Word.Add(entity[i][j].Unit);
                    }
                }
            }
            for (int i = 0; i < Find_Unit.Count; i++)//排序
            {
                int c = 0;
                CountNUnit CNU = new CountNUnit();
                //sw.WriteLine(Convert.ToString("-------------------------------"+Find_Unit[i]+"----------------------------------------"));
                for (int j = 0; j < entity.Count; j++)
                {

                    for (int k = 0; k < entity[j].Count; k++)
                    {
                        if (Find_Unit[i].Equals(entity[j][k].Unit))
                        {
                            c++;
                            //sw.WriteLine(Convert.ToString(entity[j][k].Unit + entity[j][k].reporter + entity[j][k].fix_item + entity[j][k].location + entity[j][k].remark + entity[j][k].time));
                            Find_Unit.Remove(i.ToString());
                            Sorting_Data.Add(entity[j]);

                        }
                    }

                }
                CNU.Count = c;
                CNU.Unit = Find_Unit[i].ToString();
                Count_Number.Add(CNU);
            }



            //sw.WriteLine(Convert.ToString("勾選數目: " + entity.Count + "各組室數目: " + Find_Unit.Count));
            //sw.WriteLine(Convert.ToString(""));
            //sw.Close();

            ConvertByteAndOutput(context, CreateWord(Sorting_Data, Count_Number, Filename));
            
        }
        private string CreateWord(List<List<plumber_table>> Sorting_Data, List<CountNUnit> Count_Number, String SavePath)
        {
            int be = 0;
            int end = 0;

            object oEndOfDoc = "\\endofdoc";
            Word.Document oDoc = new Word.Document();
            Object Nothing = System.Reflection.Missing.Value;
            object filepath = @"C:\Generate_Temp\" + SavePath; //IIS

            Word.Application WordApp = new Word.Application();
            for (int i = 0; i < Count_Number.Count; i++)
            {
                int count = 1;
                oDoc.Application.Visible = false;//不顯示word視窗
                oDoc.PageSetup.Orientation = Word.WdOrientation.wdOrientLandscape;//橫向
                dynamic oRange = oDoc.Content.Application.Selection.Range;



                oRange = oDoc.Content.Paragraphs.Add(ref Nothing);
                oRange.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                oRange.Range.Text = "岡山榮家工作班預約派工(修繕)單 " + Count_Number[i].Unit;
                oRange.Range.Font.Name = "標楷體";
                oRange.Range.Font.Size = 17;
                oRange.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                


                Word.Table Tables;
                Word.Range wrdRng = oDoc.Bookmarks.get_Item(ref oEndOfDoc).Range;
                Tables = oDoc.Tables.Add(wrdRng, Count_Number[i].Count + 3, 8, ref Nothing, ref Nothing);
                Tables.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                Tables.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                Tables.AllowAutoFit = true;
                oRange.Range.InsertBreak(Word.WdBreakType.wdPageBreak);//換頁

                string[] title = new string[] { "項次", "申請事項", "地點", "報修日期","維修內容", "維修人員", "處理情形", "備註" };
                for (int j = 1; j <= title.Length; j++)
                {
                    Tables.Cell(1, j).Range.Text = title[j - 1];
                    Tables.Cell(1, j).Range.Font.Name = "標楷體";
                    Tables.Cell(1, j).Range.Font.Size = 16;
                    Tables.Cell(1, j).Range.Font.Bold = 1;
                    Tables.Cell(1, j).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                }
                if (i == 0)
                {
                    end = Count_Number[i].Count;
                }
                else
                {
                    be = end;
                    end = end + Count_Number[i].Count;
                }
                for (int j = be; j < end; j++)
                {

                    Tables.Cell(count + 1, 1).Range.Text = (count).ToString();
                    Tables.Cell(count + 1, 1).Range.Font.Name = "標楷體";
                    Tables.Cell(count + 1, 1).Range.Font.Size = 10;
                    Tables.Cell(count + 1, 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                    Tables.Cell(count + 1, 2).Range.Text = Sorting_Data[j][0].fix_item;
                    Tables.Cell(count + 1, 2).Range.Font.Name = "標楷體";
                    Tables.Cell(count + 1, 2).Range.Font.Size = 13;
                    Tables.Cell(count + 1, 2).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                    Tables.Cell(count + 1, 3).Range.Text = Sorting_Data[j][0].location;
                    Tables.Cell(count + 1, 3).Range.Font.Name = "標楷體";
                    Tables.Cell(count + 1, 3).Range.Font.Size = 13;
                    Tables.Cell(count + 1, 3).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                    
                    string[] ymd = Sorting_Data[j][0].time.Split(' ');
                    Tables.Cell(count + 1, 4).Range.Text = ymd[0].ToString();
                    Tables.Cell(count + 1, 4).Range.Font.Name = "標楷體";
                    Tables.Cell(count + 1, 4).Range.Font.Size = 13;
                    Tables.Cell(count + 1, 4).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                    Tables.Cell(count + 1, 5).Range.Text = Sorting_Data[j][0].remark;
                    Tables.Cell(count + 1, 5).Range.Font.Name = "標楷體";
                    Tables.Cell(count + 1, 5).Range.Font.Size = 13;
                    Tables.Cell(count + 1, 5).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                    Tables.Cell(count + 1, 7).Range.Text = "☐已修繕完成";
                    Tables.Cell(count + 1, 7).Range.Text += "☐堂隊自行修繕完成";
                    Tables.Cell(count + 1, 7).Range.Text += "☐請申請單位另填請購單備料";
                    Tables.Cell(count + 1, 7).Range.Text += "☐需送廠商維修";
                    Tables.Cell(count + 1, 7).Range.Font.Name = "標楷體";
                    Tables.Cell(count + 1, 7).Range.Font.Size = 13;
                    Tables.Cell(count + 1, 7).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;

                    count++;
                }

                Tables.Columns[1].AutoFit();//對某列進行Fitting
                Tables.Columns[2].AutoFit();
                //Tables.Columns[3].AutoFit();
                Tables.Columns[4].AutoFit();
                //Tables.Columns[5].AutoFit();
                Tables.Columns[6].AutoFit();
                Tables.Columns[7].AutoFit();

                Tables.Cell(Count_Number[i].Count + 2, 1).Merge(Tables.Cell(Count_Number[i].Count + 2, 2));
                Tables.Cell(Count_Number[i].Count + 2, 2).Merge(Tables.Cell(Count_Number[i].Count + 2, 3));
                Tables.Cell(Count_Number[i].Count + 2, 3).Merge(Tables.Cell(Count_Number[i].Count + 2, 4));

                Tables.Cell(Count_Number[i].Count + 3, 1).Merge(Tables.Cell(Count_Number[i].Count + 3, 2));
                Tables.Cell(Count_Number[i].Count + 3, 2).Merge(Tables.Cell(Count_Number[i].Count + 3, 3));
                Tables.Cell(Count_Number[i].Count + 3, 3).Merge(Tables.Cell(Count_Number[i].Count + 3, 4));


                Tables.Cell(Count_Number[i].Count + 2, 1).Range.Text = "管理人";
                Tables.Cell(Count_Number[i].Count + 2, 1).Range.Font.Name = "標楷體";
                Tables.Cell(Count_Number[i].Count + 2, 1).Range.Font.Size = 12;
                Tables.Cell(Count_Number[i].Count + 2, 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                Tables.Cell(Count_Number[i].Count + 2, 1).Range.Font.Bold = 1;

                Tables.Cell(Count_Number[i].Count + 2, 2).Range.Text = "核派日期";
                Tables.Cell(Count_Number[i].Count + 2, 2).Range.Font.Name = "標楷體";
                Tables.Cell(Count_Number[i].Count + 2, 2).Range.Font.Size = 12;
                Tables.Cell(Count_Number[i].Count + 2, 2).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                Tables.Cell(Count_Number[i].Count + 2, 2).Range.Font.Bold = 1;

                Tables.Cell(Count_Number[i].Count + 2, 3).Range.Text = "申請單位簽名";
                Tables.Cell(Count_Number[i].Count + 2, 3).Range.Font.Name = "標楷體";
                Tables.Cell(Count_Number[i].Count + 2, 3).Range.Font.Size = 12;
                Tables.Cell(Count_Number[i].Count + 2, 3).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                Tables.Cell(Count_Number[i].Count + 2, 3).Range.Font.Bold = 1;

                Tables.Cell(Count_Number[i].Count + 2, 4).Range.Text = "結案日期";
                Tables.Cell(Count_Number[i].Count + 2, 4).Range.Font.Name = "標楷體";
                Tables.Cell(Count_Number[i].Count + 2, 4).Range.Font.Size = 12;
                Tables.Cell(Count_Number[i].Count + 2, 4).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                Tables.Cell(Count_Number[i].Count + 2, 4).Range.Font.Bold = 1;

                Tables.Cell(Count_Number[i].Count + 2, 5).Range.Text = "主管";
                Tables.Cell(Count_Number[i].Count + 2, 5).Range.Font.Name = "標楷體";
                Tables.Cell(Count_Number[i].Count + 2, 5).Range.Font.Size = 12;
                Tables.Cell(Count_Number[i].Count + 2, 5).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                Tables.Cell(Count_Number[i].Count + 2, 5).Range.Font.Bold = 1;

                //Tables.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitContent);//全部自動Fitting
                Tables.Rows.Alignment = Word.WdRowAlignment.wdAlignRowCenter;


            }


            oDoc.SaveAs(ref filepath, ref Nothing, ref Nothing, ref Nothing, ref Nothing, ref Nothing, ref Nothing, ref Nothing, ref Nothing, ref Nothing, ref Nothing, ref Nothing, ref Nothing, ref Nothing, ref Nothing, ref Nothing);
            oDoc.Close(ref Nothing, ref Nothing, ref Nothing);
            WordApp.Quit(ref Nothing, ref Nothing, ref Nothing); //關閉porcess
            return SavePath;
        }
        private void ConvertByteAndOutput(HttpContext context, string fileName)
        {
            //讀取檔案並將檔案轉成二進制內容
            var output = new byte[0];
            using (var fs = new FileStream(@"C:\Generate_Temp\" + fileName,
                FileMode.Open, FileAccess.Read))
            {
                output = new byte[(int)fs.Length];
                fs.Read(output, 0, output.Length);
                fs.Close();
            }

            //將檔案輸出到瀏覽器
            context.Response.Clear();
            context.Response.AddHeader(
                "Content-Length", output.Length.ToString());
            context.Response.ContentType = "application/msword";//"application/octet-stream";
            context.Response.AddHeader(
                "content-disposition",
                "attachment; filename=" + fileName);
            context.Response.OutputStream.Write(output, 0, output.Length);
            context.Response.Flush();
            context.Response.End();

            CloseWINWORD_Porcesses();
            //File.Delete(@"C:\Generate_Temp\" + fileName);

        }
        private void CloseWINWORD_Porcesses()
        {
            Process[] aProcWrd = Process.GetProcessesByName("WINWORD");

            foreach (System.Diagnostics.Process oProc in aProcWrd)
            {

                oProc.CloseMainWindow();
            }
        }
        private void DeleteDOC(HttpContext context,string WordPath)
        {
            string[] files = Directory.GetFiles(WordPath, "*.doc");//找出目錄底下檔案
            //逐筆刪除
            foreach (string file in files)
            {
                File.Delete(file);
            }

        }
        public class CountNUnit
        {
            public int Count { get; set; }
            public string Unit { get; set; }
        }
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}