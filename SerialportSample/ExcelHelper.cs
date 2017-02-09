using HMIControls;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace SerialportSample
{
    class ExcelHelper
    {
        #region 导入
        public static List<DataTable> ImportExcelFile(string filePath, MyDataGridView dgv1, MyDataGridView dgv2, MyDataGridView dgv3, MyDataGridView dgv4, MyDataGridView dgv5)
        {
            List<DataTable> dts = new List<DataTable>();
            HSSFWorkbook hssfworkbook;
            #region//初始化信息
            try
            {
                using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    hssfworkbook = new HSSFWorkbook(file);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            #endregion

            NPOI.SS.UserModel.ISheet sheet = hssfworkbook.GetSheetAt(0);
            System.Collections.IEnumerator rows = sheet.GetRowEnumerator();
            DataTable dt1 = (DataTable)dgv1.DataSource;
            DataTable dt2 = (DataTable)dgv2.DataSource;
            DataTable dt3 = (DataTable)dgv3.DataSource;
            DataTable dt4 = (DataTable)dgv4.DataSource;
            DataTable dt5 = (DataTable)dgv5.DataSource;
            int cols = sheet.GetRow(0).LastCellNum;
            if (cols != 15 && cols != 13)
            {
                return dts;
            }
            dt1.Clear();
            dt2.Clear();
            dt3.Clear();
            dt4.Clear();
            dt5.Clear();
            //前两行为标题
            rows.MoveNext();
            rows.MoveNext();

            int count = 0;
            while (rows.MoveNext())
            {
                count++;
                if (count > 10) break;

                HSSFRow row = (HSSFRow)rows.Current;
                DataRow dr1 = dt1.NewRow();
                DataRow dr2 = dt2.NewRow();
                DataRow dr3 = dt3.NewRow();
                DataRow dr4 = dt4.NewRow();
                DataRow dr5 = dt5.NewRow();
                
                NPOI.SS.UserModel.ICell cell = null;
                createColumn(dt1, cell, row, dr1, 0, 1); //输入功率标定
                createColumn(dt2, cell, row, dr2, 3, 4); //输出功率标定
                createColumn(dt3, cell, row, dr3, 6, 7); //反射功率标定
                createColumn(dt4, cell, row, dr4, 9, 10); //ALC功率标定

                //衰减补偿
                if (count > 3) continue;
                createColumn(dt5, cell, row, dr5, 12, 14);

            }
            dts.Add(dt1);
            dts.Add(dt2);
            dts.Add(dt3);
            dts.Add(dt4);
            dts.Add(dt5);
            return dts;
        }

        private static void createColumn(DataTable dt, NPOI.SS.UserModel.ICell cell, HSSFRow row, DataRow dr1, int start, int end)
        {
            DataRow dr = dt.NewRow();
            int j = 0;
            for (int i = start; i <= end; i++)
            {
                cell = row.GetCell(i);
                if (cell == null)
                {
                    dr[j] = 0;
                }
                else
                {
                    dr[j] = stringToInt(cell.ToString());
                }
                j++;
            }
            dt.Rows.Add(dr);
        }
        #endregion

        private static int stringToInt(string value)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception e)
            {
                throw new Exception("Excel中存在非法数据");
            }
        }


        #region 导出
        public static void WriteExcel(List<DataTable> dts, string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath)) // && null != dt && dt.Rows.Count > 0)
                {
                    NPOI.HSSF.UserModel.HSSFWorkbook book = new NPOI.HSSF.UserModel.HSSFWorkbook();
                    NPOI.SS.UserModel.ISheet sheet = book.CreateSheet("sheet1");
                    createColumnName(book, sheet);

                    createCells(book, sheet, dts[0], 0, 1);
                    createCells(book, sheet, dts[1], 3, 4);
                    createCells(book, sheet, dts[2], 6, 7);
                    createCells(book, sheet, dts[3], 9, 10);
                    createCells(book, sheet, dts[4], 12, 14);

                    // 写入到客户端  
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {
                        book.Write(ms);
                        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            byte[] data = ms.ToArray();
                            fs.Write(data, 0, data.Length);
                            fs.Flush();
                        }
                        book = null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static void createCells(NPOI.HSSF.UserModel.HSSFWorkbook book, NPOI.SS.UserModel.ISheet sheet, DataTable dt, int start, int end)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int colIndex = 0;
                NPOI.SS.UserModel.IRow row = sheet.GetRow(i + 2);
                if (row == null)
                {
                    row = sheet.CreateRow(i + 2);
                }
                for (int j = start; j <= end; j++)
                {
                    row.CreateCell(j).SetCellValue(Convert.ToInt32(dt.Rows[i][colIndex]));
                    row.GetCell(j).CellStyle = GetCellStyle(book);
                    colIndex++;
                }
            }
        }

        private static void createColumnName(NPOI.HSSF.UserModel.HSSFWorkbook book, NPOI.SS.UserModel.ISheet sheet)
        {
            SetCellRangeAddress(sheet, 0, 0, 0, 1);
            SetCellRangeAddress(sheet, 0, 0, 3, 4);
            SetCellRangeAddress(sheet, 0, 0, 6, 7);
            SetCellRangeAddress(sheet, 0, 0, 9, 10);
            SetCellRangeAddress(sheet, 0, 0, 12, 14);

            NPOI.SS.UserModel.IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("输入功率标定");
            row.CreateCell(3).SetCellValue("输出功率标定");
            row.CreateCell(6).SetCellValue("反射功率标定");
            row.CreateCell(9).SetCellValue("ALC功率标定");
            row.CreateCell(12).SetCellValue("衰减补偿");
            row.GetCell(0).CellStyle = GetCellStyle(book);
            row.GetCell(3).CellStyle = GetCellStyle(book);
            row.GetCell(6).CellStyle = GetCellStyle(book);
            row.GetCell(9).CellStyle = GetCellStyle(book);
            row.GetCell(12).CellStyle = GetCellStyle(book);

            row = sheet.CreateRow(1);
            for (int i = 0; i < 4; i++)
            {
                row.CreateCell(3 * i, NPOI.SS.UserModel.CellType.Numeric).SetCellValue("采样电压");
                row.CreateCell(3 * i + 1, NPOI.SS.UserModel.CellType.Numeric).SetCellValue("定标点");
                row.GetCell(3 * i).CellStyle = GetCellStyle(book);
                row.GetCell(3 * i + 1).CellStyle = GetCellStyle(book);
            }
            row.CreateCell(12, NPOI.SS.UserModel.CellType.Numeric).SetCellValue("起始值");
            row.CreateCell(13, NPOI.SS.UserModel.CellType.Numeric).SetCellValue("结束值");
            row.CreateCell(14, NPOI.SS.UserModel.CellType.Numeric).SetCellValue("补偿值");
            row.GetCell(12).CellStyle = GetCellStyle(book);
            row.GetCell(13).CellStyle = GetCellStyle(book);
            row.GetCell(14).CellStyle = GetCellStyle(book);

        }

        /// <summary>
        /// 合并单元格
        /// </summary>
        /// <param name="sheet">要合并单元格所在的sheet</param>
        /// <param name="rowstart">开始行的索引</param>
        /// <param name="rowend">结束行的索引</param>
        /// <param name="colstart">开始列的索引</param>
        /// <param name="colend">结束列的索引</param>
        public static void SetCellRangeAddress(NPOI.SS.UserModel.ISheet sheet, int rowstart, int rowend, int colstart, int colend)
        {
            CellRangeAddress cellRangeAddress = new CellRangeAddress(rowstart, rowend, colstart, colend);
            sheet.AddMergedRegion(cellRangeAddress);
        }

        /// <summary>
        /// 获取单元格样式
        /// </summary>
        /// <param name="hssfworkbook">Excel操作类</param>
        /// <returns></returns>
        public static NPOI.SS.UserModel.ICellStyle GetCellStyle(HSSFWorkbook hssfworkbook)
        {
            NPOI.SS.UserModel.ICellStyle cellstyle = hssfworkbook.CreateCellStyle();
            //居中显示
            cellstyle.VerticalAlignment = VerticalAlignment.Center;
            cellstyle.Alignment = HorizontalAlignment.Center;
            //有边框
            cellstyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            cellstyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            cellstyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cellstyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            return cellstyle;
        }



        #endregion
    }
}
