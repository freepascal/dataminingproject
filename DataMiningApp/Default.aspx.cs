﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.DataVisualization.Charting;
using MathNet.Numerics.LinearAlgebra;

namespace DataMiningApp
{
    public partial class _Default : System.Web.UI.Page
    {
        // Public variables necessary for data write on "Next" click
        string[, ,] controlarray;
        int max_layout_cols;
        int max_layout_rows;
        
        // Core variables
        int jobid = 1;
        int algorithmid = 1;
        int stepid;

        // Define database connection objects
        SqlConnection connection;
        SqlCommand command;
        SqlDataReader reader;

        protected void Page_Load(object sender, EventArgs e)
        {
            
            // Retrieve session id


            stepid = (int)Session["stepid"];

            // Specify connection string to database
            
            // Microsoft Access
            //connection = new SqlConnection("Driver={Microsoft Access Driver (*.mdb)};DBQ=" + Server.MapPath("/App_Data/database.mdb") + ";UID=;PWD=;");

            // Microsoft SQL Server
            connection = new SqlConnection("Data Source=localhost;Initial Catalog=DMP;UId=webapp;Password=123;");

            // Create SQL query to find out table size
            string tablesize_query = "WEBAPP_TABLESIZE " + algorithmid + "," + stepid;
            
            // Establish connection
            reader = openconnection(tablesize_query, connection);

            // Read table size
            reader.Read();
                max_layout_cols = (int)reader[0] + 1;
                max_layout_rows = (int)reader[1] + 1;
            closeconnection(reader, connection);

            // Create SQL query to pull table layout information for this job and step
            string layout_query = "WEBAPP_GETLAYOUT " + algorithmid + "," + stepid;
            command = new SqlCommand(layout_query, connection);

            // Open connection and execute query using SQL Reader
            connection.Open();
            reader = command.ExecuteReader();

            // Control array - last index is for control type (0), fill data name (1), and output data name (2)
            controlarray = new string[max_layout_cols, max_layout_rows, 3];
            
            // Span array control row and column spans
            int[, ,] spanarray = new int[max_layout_cols, max_layout_rows, 2];
            int layout_x, layout_y;

            // Read through layout table for this step and algorithm
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    // Populate control array
                    layout_x = (int)reader[0];                                  // Table x index
                    layout_y = (int)reader[1];                                  // Table y index

                    controlarray[layout_x, layout_y, 0] = (string)reader[4];    // Control type
                    controlarray[layout_x, layout_y, 1] = (string)reader[5];    // Fill data name
                    controlarray[layout_x, layout_y, 2] = (string)reader[6];    // Output data name

                    spanarray[layout_x, layout_y, 0] = (int)reader[2];          // Rowspan
                    spanarray[layout_x, layout_y, 1] = (int)reader[3];          // Colspan
                }
            }
            connection.Close();

            // Build interface

            // Evenly distribute width and height of cells to conform to panel
            // Panel is designed to show scroll bars in case cell contents force size larger than specified here
            string html_cellwidth = Convert.ToString((Convert.ToInt16(mainpanel.Width.ToString().Substring(0, mainpanel.Width.ToString().Length - 2)) - ((max_layout_cols)*layouttable.Border) - layouttable.CellPadding) / max_layout_cols) + "px";
            string html_cellheight = Convert.ToString((Convert.ToInt16(mainpanel.Height.ToString().Substring(0, mainpanel.Height.ToString().Length - 2)) - ((max_layout_rows)*layouttable.Border) - layouttable.CellPadding) / max_layout_rows) + "px";

            // Run through rows
            for (int row_traverse = 0; row_traverse < max_layout_rows; row_traverse++)
            {
                // Add row
                HtmlTableRow newrow = new HtmlTableRow();
                newrow.Height = html_cellheight;
                layouttable.Rows.Add(newrow);
                
                // Run through columns
                for (int col_traverse = 0; col_traverse < max_layout_cols; col_traverse++)
                {
                    // Check if this is a valid cell
                    if (spanarray[col_traverse, row_traverse, 0] > 0 && spanarray[col_traverse, row_traverse, 1] > 0)
                    {
                        // Create new cell object
                        HtmlTableCell newcell = new HtmlTableCell();

                        // Set column and row span properties (merge cells)
                        newcell.RowSpan = spanarray[col_traverse, row_traverse, 0];
                        newcell.ColSpan = spanarray[col_traverse, row_traverse, 1];

                        // Set cell width and height based on prior calculation
                        newcell.Width = html_cellwidth;
                        newcell.VAlign = "top";

                        // Add cell to table
                        layouttable.Rows[row_traverse].Cells.Add(newcell);
                    
                        // Add control, if applicable
                        Control newcontrol = addcontrol(controlarray, newcell, newrow, col_traverse, row_traverse);
                        
                        // Fill data into control
                        fillcontrol(newcontrol, controlarray, col_traverse, row_traverse, jobid, algorithmid, stepid, reader, connection);
                        
                    }
                }
            }
        }

        // CONTROL ADDITION -------------------------------------------------------------------------------------------------------------

        Control addcontrol(string[, ,] controlarray, HtmlTableCell cell, HtmlTableRow row, int col_traverse, int row_traverse)
        {
            // Generic return object
            Control returncontrol = new Control();

            // Specific object generation methods
            switch(controlarray[col_traverse, row_traverse, 0])
            {
                case "LABEL":   // Label control
                    {
                        // Create new control
                        Label newlabel = new Label();

                        // Set control properties
                        newlabel.Font.Name = "Arial"; newlabel.Font.Size = 11;
                        newlabel.ID = "control_" + col_traverse + "_" + row_traverse;

                        // Add control
                        cell.Controls.Add(newlabel);
                        returncontrol = newlabel;

                        break;
                    }
                case "TEXTBOX":
                    {
                        // Create new control
                        TextBox newtextbox = new TextBox();
                        Label newlabel = new Label();

                        // Set textbox control properties
                        newtextbox.Font.Name = "Arial"; newtextbox.Font.Size = 11;
                        newtextbox.ID = "control_" + col_traverse + "_" + row_traverse;
                        newtextbox.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0,cell.Width.Length-2))*cell.ColSpan - 2*(layouttable.Border + layouttable.CellPadding));

                        // Set label control properties
                        newlabel.Font.Name = "Arial"; newlabel.Font.Size = 11;

                        // Add control
                        cell.Controls.Add(newlabel);
                        cell.Controls.Add(new LiteralControl("<br><br>"));
                        cell.Controls.Add(newtextbox);
                        
                        // Return label for text fill
                        //returncontrol = newtextbox;
                        returncontrol = newlabel;
                        break;
                    }
                case "MULTISELECT":
                    {
                        // Create new control
                        ListBox newlistbox = new ListBox();
                        Label newlabel = new Label();

                        // Set listbox control properties
                        newlistbox.Font.Name = "Arial"; newlistbox.Font.Size = 11;
                        newlistbox.ID = "control_" + col_traverse + "_" + row_traverse;
                        newlistbox.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0, cell.Width.Length - 2)) * cell.ColSpan - 2 * (layouttable.Border + layouttable.CellPadding));
                        newlistbox.SelectionMode = ListSelectionMode.Multiple;

                        // Set label control properties
                        newlabel.Font.Name = "Arial"; newlabel.Font.Size = 11;
                        newlabel.ID = "control_" + col_traverse + "_" + row_traverse + "_label";

                        // Add control
                        cell.Controls.Add(newlabel);
                        cell.Controls.Add(new LiteralControl("<br><br>"));
                        cell.Controls.Add(newlistbox);

                        // Return label for text fill
                        //returncontrol = newtextbox;
                        returncontrol = newlistbox;
                        break;
                    }
                case "IMAGE":
                    {
                        // Create new control
                        Image newimage = new Image();

                        // Set control properties
                        newimage.ID = "control_" + col_traverse + "_" + row_traverse;
                        newimage.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0, cell.Width.Length - 2)) * cell.ColSpan - 2 * (layouttable.Border + layouttable.CellPadding));
                        newimage.Height = Unit.Pixel(Convert.ToInt16(row.Height.Substring(0, row.Height.Length - 2)) * cell.RowSpan - 2 * (layouttable.Border + layouttable.CellPadding));

                        // Add control
                        cell.Controls.Add(newimage);
                        returncontrol = newimage;

                        break;
                    }
                case "TABLE":
                    {
                        // Enclose table in panel
                        Panel tablepanel = new Panel();
                        tablepanel.ScrollBars = ScrollBars.Both;
                        tablepanel.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0, cell.Width.Length - 2)) * cell.ColSpan - (layouttable.Border + layouttable.CellPadding));
                        tablepanel.Height = Unit.Pixel(Convert.ToInt16(row.Height.Substring(0, row.Height.Length - 2)) * cell.RowSpan - (layouttable.Border + layouttable.CellPadding));
                        
                        // Create new control
                        GridView newtable = new GridView();

                        // Set control properties
                        newtable.ID = "control_" + col_traverse + "_" + row_traverse;
                        newtable.Width = Unit.Pixel((int)(tablepanel.Width.Value - 17));
                        newtable.Height = Unit.Pixel((int)(tablepanel.Height.Value - 17));
                        newtable.Font.Name = "Arial"; newtable.Font.Size = 11;
                        newtable.HeaderStyle.BackColor = System.Drawing.Color.Silver;
                        newtable.RowStyle.BackColor = System.Drawing.Color.White;
                        newtable.RowStyle.HorizontalAlign = HorizontalAlign.Center;

                        // Add control
                        tablepanel.Controls.Add(newtable);
                        cell.Controls.Add(tablepanel);
                        returncontrol = tablepanel;

                        break;
                    }
                case "SCATTERPLOT":
                    {
                        Chart Projection = new Chart();
                        Series newseries = new Series();
                        newseries.ChartType = SeriesChartType.Point;
                        Projection.ChartAreas.Add(new ChartArea());
                        Projection.ChartAreas[0].AxisY.Title = "Second Principal Component";
                        Projection.ChartAreas[0].AxisX.Title = "First Principal Component";

                        Projection.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0, cell.Width.Length - 2)) * cell.ColSpan - 2 * (layouttable.Border + layouttable.CellPadding));
                        Projection.Height = Unit.Pixel(Convert.ToInt16(row.Height.Substring(0, row.Height.Length - 2)) * cell.RowSpan - 2 * (layouttable.Border + layouttable.CellPadding));

                        DataMiningApp.Analysis.ParameterStream stream;
                        Registry.Registry registry;

                        stream = DataMiningApp.Analysis.ParameterStream.getStream(Session);
                        registry = Registry.Registry.getRegistry(Session);

                        Matrix PCmatrix = (Matrix)stream.get("PCmatrix");
                        String[] features = (String[])stream.get("selectedFeatures");

                        System.Data.DataSet ds = (System.Data.DataSet)registry.GetDataset((string)stream.get("dataSetName"));

                        //retrieve dataset table (assume one for now)
                        System.Data.DataTable dt = ds.Tables[0];

                        //raw data
                        double[,] rawData = new double[dt.Rows.Count, features.Count()];
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            for (int j = 0; j < features.Count(); j++)
                                rawData[i, j] = (double)dt.Rows[i].ItemArray.ElementAt(dt.Columns[features[j]].Ordinal);
                        }

                        //Create matrix to hold data for PCA
                        Matrix X = new Matrix(rawData);

                        //Remove mean
                        Vector columnVector;
                        for (int i = 0; i < X.ColumnCount; i++)
                        {
                            columnVector = X.GetColumnVector(i);
                            X.SetColumnVector(columnVector.Subtract(columnVector.Average()), i);
                        }

                        //get first two PCs
                        Matrix xy = new Matrix(PCmatrix.RowCount, 2);
                        xy.SetColumnVector(PCmatrix.GetColumnVector(0), 0);
                        xy.SetColumnVector(PCmatrix.GetColumnVector(1), 1);

                        //project
                        Matrix projected = X.Multiply(xy);

                        DataPoint point;
                        Projection.Series.Clear();
                        Projection.Legends.Clear();


                        //if a label column is selected
                        String LabelColumnName = "Species";

                        if (!LabelColumnName.Equals(""))
                        {

                            //get labels
                            int labelColumnIndex = dt.Columns[LabelColumnName].Ordinal;
                            List<String> labels = new List<String>();
                            String item;

                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                item = (String)dt.Rows[i].ItemArray.ElementAt(labelColumnIndex);
                                if (!labels.Contains(item))
                                    labels.Add(item);
                            }
                            Legend mylegend = Projection.Legends.Add(LabelColumnName);
                            mylegend.TableStyle = LegendTableStyle.Wide;

                            Projection.Legends[0].Docking = Docking.Bottom;
                            System.Drawing.Font font = Projection.Legends[LabelColumnName].Font = new System.Drawing.Font(Projection.Legends[LabelColumnName].Font.Name, 14);

                            //Configure series
                            foreach (String label in labels)
                            {
                                Projection.Series.Add(label);
                                Projection.Series[label].LegendText = label;
                                Projection.Series[label].IsXValueIndexed = false;
                                Projection.Series[label].ChartType = SeriesChartType.Point;
                                Projection.Series[label].MarkerSize = 8;
                            }

                            //Add points
                            for (int i = 0; i < projected.RowCount; i++)
                            {
                                point = new DataPoint(projected[i, 0], projected[i, 1]);
                                String label = dt.Rows[i].ItemArray[labelColumnIndex].ToString();
                                Projection.Series[label].Points.Add(point);
                            }

                        }
                        else
                        {
                            //Single plot graph
                            Projection.Series.Add("series1");
                            Projection.Series[0].IsXValueIndexed = false;
                            Projection.Series[0].ChartType = SeriesChartType.Point;
                            Projection.Series[0].MarkerSize = 8;

                            for (int i = 0; i < projected.RowCount; i++)
                            {
                                point = new DataPoint(projected[i, 0], projected[i, 1]);
                                Projection.Series[0].Points.Add(point);
                            }
                        }
                        cell.Controls.Add(Projection);
                        returncontrol = Projection;

                        /*
                        // Create new control
                        Chart chartcontrol = new Chart();

                        // Set chart width and height
                        chartcontrol.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0, cell.Width.Length - 2)) * cell.ColSpan - 2 * (layouttable.Border + layouttable.CellPadding));
                        chartcontrol.Height = Unit.Pixel(Convert.ToInt16(row.Height.Substring(0, row.Height.Length - 2)) * cell.RowSpan - 2 * (layouttable.Border + layouttable.CellPadding));

                        // Needed so server knows where to store temporary image
                        chartcontrol.ImageStorageMode = ImageStorageMode.UseImageLocation;

                        ChartArea mychartarea = new ChartArea();
                        chartcontrol.ChartAreas.Add(mychartarea);

                        Series myseries = new Series();
                        myseries.Name = "Series";
                        chartcontrol.Series.Add(myseries);

                        chartcontrol.Series["Series"].ChartType = SeriesChartType.Point;

                        // Add control
                        cell.Controls.Add(chartcontrol);
                        returncontrol = chartcontrol;
                        */
                        break;
                    }
                case "LINEPLOT":
                    {                           
                        DataMiningApp.Analysis.ParameterStream stream = DataMiningApp.Analysis.ParameterStream.getStream(Session);
                        Vector Weights = (Vector)stream.get("Weights");

                        Chart VariancePlot = new Chart();

                        VariancePlot.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0, cell.Width.Length - 2)) * cell.ColSpan - 2 * (layouttable.Border + layouttable.CellPadding));
                        VariancePlot.Height = Unit.Pixel(Convert.ToInt16(row.Height.Substring(0, row.Height.Length - 2)) * cell.RowSpan - 2 * (layouttable.Border + layouttable.CellPadding));
                        
                        VariancePlot.Palette = ChartColorPalette.EarthTones;
                        Series dataseries = new Series();
                        dataseries.ChartType = SeriesChartType.Line;
                        dataseries.MarkerColor = System.Drawing.Color.Black;
                        dataseries.MarkerBorderWidth = 3;
                        dataseries.MarkerBorderColor = System.Drawing.Color.Black;
                        VariancePlot.Series.Add(dataseries);
                        VariancePlot.ChartAreas.Add(new ChartArea());
                        VariancePlot.ChartAreas[0].AxisY.Title = "Variance Explained";
                        VariancePlot.ChartAreas[0].AxisX.Title = "Principal Component";

                        for (int i = 0; i < Weights.Length; i++)
                            VariancePlot.Series[0].Points.InsertY(i, Weights[i]);

                        cell.Controls.Add(VariancePlot);
                        returncontrol = VariancePlot;

                        break;
                    }
                case "UPLOAD":
                    {
                        // Create new controls
                        Label uploadlabel = new Label();
                        FileUpload uploadcontrol = new FileUpload();
                        HiddenField savedfile = new HiddenField(); HiddenField savedpath = new HiddenField();
                        Button uploadbutton = new Button();
                        GridView uploadtable = new GridView();

                        // Create panel to enclose table to it can scroll without having to scroll entire window
                        Panel tablepanel = new Panel();
                        tablepanel.ScrollBars = ScrollBars.Both;
                        tablepanel.Width = Unit.Pixel(Convert.ToInt16(cell.Width.Substring(0, cell.Width.Length - 2)) * cell.ColSpan - (layouttable.Border + layouttable.CellPadding));
                        tablepanel.Height = Unit.Pixel(Convert.ToInt16(row.Height.Substring(0, row.Height.Length - 2)) * cell.RowSpan - (layouttable.Border + layouttable.CellPadding));

                        // Set IDs for all controls (necessary to get information after postback on upload)
                        uploadlabel.ID = "control_" + col_traverse + "_" + row_traverse + "_label";
                        savedfile.ID = "control_" + col_traverse + "_" + row_traverse + "_savedfile";
                        savedpath.ID = "control_" + col_traverse + "_" + row_traverse + "_savedpath";
                        uploadcontrol.ID = "control_" + col_traverse + "_" + row_traverse;
                        uploadtable.ID = "control_" + col_traverse + "_" + row_traverse + "_table";
                        uploadbutton.ID = "control_" + col_traverse + "_" + row_traverse + "_button";

                        // Set control properties
                        uploadbutton.Text = "Load File";
                        uploadbutton.Font.Name = "Arial"; uploadbutton.Font.Size = 10;
                        uploadbutton.Width = 100;
                        uploadbutton.Click += new System.EventHandler(uploadbutton_Click);
                        uploadlabel.Font.Name = "Arial"; uploadlabel.Font.Size = 11;
                        uploadlabel.ForeColor = System.Drawing.Color.Black;
                        uploadcontrol.Width = Unit.Pixel((int)(tablepanel.Width.Value - 17) - (int)uploadbutton.Width.Value);
                        uploadtable.Width = Unit.Pixel((int)(tablepanel.Width.Value - 17));
                        uploadtable.Height = Unit.Pixel((int)(tablepanel.Height.Value - 17));
                        uploadtable.Font.Name = "Arial"; uploadtable.Font.Size = 11;
                        uploadtable.HeaderStyle.BackColor = System.Drawing.Color.Silver;
                        uploadtable.RowStyle.BackColor = System.Drawing.Color.White;
                        uploadtable.RowStyle.HorizontalAlign = HorizontalAlign.Center;

                        // Add controls to form and format
                        tablepanel.Controls.Add(uploadlabel);
                        tablepanel.Controls.Add(new LiteralControl("<br><br>"));
                        tablepanel.Controls.Add(uploadcontrol);
                        tablepanel.Controls.Add(uploadbutton);
                        tablepanel.Controls.Add(new LiteralControl("<br><br>"));
                        tablepanel.Controls.Add(uploadtable);

                        // Add controls to scrollable panel
                        cell.Controls.Add(tablepanel);
                        
                        // Return uploadcontrol, even though this control itself does not need to be filled (need control type)
                        returncontrol = uploadcontrol;

                        break;
                    }

            }
            return returncontrol;

        }

        // CONTROL DATA FILL ------------------------------------------------------------------------------------------------------------

        void fillcontrol(Control fillcontrol, string[,,] controlarray, int col_traverse, int row_traverse, int jobid, int algorithmid, int stepid, SqlDataReader reader, SqlConnection connection)
        {
            // Fill data

            // Get the data name from controlarray
            string dataname = controlarray[col_traverse, row_traverse, 1];
            // String to store SQL query
            string control_query;

            // Check if fill query is specified
            if (dataname != "NONE" && dataname != "")
            {
                // Add Job ID
                if (dataname != "CONST")
                {
                    control_query = "WEBAPP_READ " + " " + jobid + ",'" + dataname + "'";
                }
                else
                {
                    control_query = "WEBAPP_SELECTCONST " + algorithmid + "," + stepid + "," + col_traverse + "," + row_traverse;
                }

                // Initialize reader and get data
                reader = openconnection(control_query, connection);

                // Fill details are specific to control type
                switch(fillcontrol.GetType().ToString())
                {
                    // Label control type
                    case "System.Web.UI.WebControls.Label":
                        {
                            string datavalue;

                            // Load label text into string and set control value
                            reader.Read();

                            if (reader.HasRows)
                            {
                                datavalue = reader["value"].ToString();
                            }
                            else
                            {
                                datavalue = null;
                            }
                            // Create label control that points to fillcontrol object
                            Label labelcontrol = (Label)fillcontrol;

                            // Add label text
                            labelcontrol.Text = datavalue;
                            
                            break;
                        }
                    case "System.Web.UI.WebControls.Image":
                        {
                            // Load image into string and set control value
                            reader.Read();
                            string imagepath = reader["value"].ToString();

                            // Create image control that points to fillcontrol object
                            Image imagecontrol = (Image)fillcontrol;

                            // Add image path
                            imagecontrol.ImageUrl = imagepath;

                            break;
                        }
                    case "System.Web.UI.WebControls.Panel":
                        {
                            // Convert reader data to dataset
                            //DataTable retrieveddataset;
                            //retrieveddataset = db_dataretrieve(reader);

                            // Create GridView control that points to fillcontrol object
                            Panel tablecontainer = (Panel)fillcontrol;
                            GridView gridviewcontrol = (GridView)tablecontainer.Controls[0];

                            //gridviewcontrol.DataSource = retrieveddataset;
                            //gridviewcontrol.DataBind();

                            // SESSION MODIFICATIONS //

                            DataMiningApp.Analysis.ParameterStream stream = DataMiningApp.Analysis.ParameterStream.getStream(Session);

                            Matrix PCmatrix = (Matrix)stream.get("PCmatrix");
                            Vector Weights = (Vector)stream.get("Weights");
                            String[] features = (String[])stream.get("selectedFeatures");

                            //for (int i = 0; i < Weights.Length; i++)
                            //    VariancePlot.Series[0].Points.InsertY(i, Weights[i]);

                            DataSet ds = new DataSet("temp");
                            DataTable dt = new DataTable();
                            // Declare your Columns

                            //PC Weight
                            DataColumn dc = new DataColumn("Weight", Type.GetType("System.Double"));
                            dt.Columns.Add(dc);

                            //PC Coefficients
                            foreach (String feature in features)
                            {
                                dc = new DataColumn(feature, Type.GetType("System.Double"));
                                dt.Columns.Add(dc);
                            }

                            // Add the DataTable to your DataSet
                            ds.Tables.Add(dt);

                            DataRow dr;
                            for (int i = 0; i < PCmatrix.ColumnCount; i++)
                            {
                                dr = ds.Tables[0].NewRow();
                                dt.Rows.Add(dr);
                                dt.Rows[i][0] = Math.Round(Weights[i], 3);
                                for (int j = 0; j < PCmatrix.RowCount; j++)
                                    dt.Rows[i][j + 1] = Math.Round(PCmatrix[j, i],3);

                            }
                            /*
                            dr = ds.Tables[0].NewRow();
                            dt.Rows.Add(dr);
                            double[] dataArray = new double[Weights.Length];
                            dataArray[0] = 1232.21321;
                            dr[0] = 321.12321;

                            //dt.Rows[0].ItemArray[1] = 333.32;
                            */
                            gridviewcontrol.DataSource = dt;
                            gridviewcontrol.DataBind();

                            //-------------------------------//

                            break;      
                        }
                    case "System.Web.UI.DataVisualization.Charting.Chart":
                        {

                            // Convert reader data to dataset
                            /*
                            DataTable retrieveddataset;
                            retrieveddataset = db_dataretrieve(reader);

                            // Set data plotted by returned column names
                            chartcontrol.Series["Series"].XValueMember = retrieveddataset.Columns[0].ColumnName;
                            chartcontrol.Series["Series"].YValueMembers = retrieveddataset.Columns[1].ColumnName;

                            chartcontrol.ChartAreas[0].AxisX.Title = chartcontrol.Series["Series"].XValueMember;
                            chartcontrol.ChartAreas[0].AxisY.Title = chartcontrol.Series["Series"].YValueMembers;

                            chartcontrol.DataSource = retrieveddataset;
                            chartcontrol.DataBind();
                            */

                            break;
                        }
                    case "System.Web.UI.WebControls.FileUpload":
                        {
                            FileUpload uploadcontrol = (FileUpload)fillcontrol;
                            string id = uploadcontrol.ID;
                            string datavalue;

                            // Fill label
                            reader.Read();
                            if (reader.HasRows)
                            {
                                datavalue = reader["value"].ToString();
                            }
                            else
                            {
                                datavalue = null;
                            }
                            Label uploadlabel = (Label)Form.FindControl(id + "_label");
                            uploadlabel.Text = datavalue;

                            break;
                        }
                    case "System.Web.UI.WebControls.ListBox":
                        {
                            ListBox listboxcontrol = (ListBox)fillcontrol;

                            string id = listboxcontrol.ID;
                            string datavalue;

                            // Fill label
                            reader.Read();
                            if (reader.HasRows)
                            {
                                datavalue = reader["value"].ToString();
                            }
                            else
                            {
                                datavalue = null;
                            }
                            Label uploadlabel = (Label)Form.FindControl(id + "_label");
                            uploadlabel.Text = datavalue;

                            String dataSetParameterName = "dataSetName";
                            DataMiningApp.Analysis.ParameterStream stream = DataMiningApp.Analysis.ParameterStream.getStream(Session);
                            if (stream.contains(dataSetParameterName))
                            {
                                Registry.Registry appRegistry = Registry.Registry.getRegistry(Session);
                                DataSet ds = appRegistry.GetDataset((String)stream.get(dataSetParameterName));

                                foreach (DataColumn dc in ds.Tables[0].Columns)
                                    listboxcontrol.Items.Add(dc.ColumnName);
                            }

                            break;
                        }
                }

                // Close reader and connection
                closeconnection(reader, connection);
            }
        }

        // DATABASE SUPPORT -------------------------------------------------------------------------------------------------------------
        
        // Reusable function to open data connection and execute reader given query string and SqlConnection object
        
        SqlDataReader openconnection(string query, SqlConnection connection)
        {
            SqlDataReader reader;
            SqlCommand command = new SqlCommand(query, connection);

            connection.Open();
            reader = command.ExecuteReader();

            return reader;
        }

        // Reusable function to close data connection and reader
        void closeconnection(SqlDataReader reader, SqlConnection connection)
        {
            reader.Close();
            connection.Close();
        }

        // DB DATA RETRIEVE AND CONVERT ------------------------------------------------------------------------------------------------

        DataTable db_dataretrieve(SqlDataReader reader)
        {
            // Create temporary data table to store data for return
            DataTable returndata = new DataTable();

            // Initialize row object, and add first row to data table
            int rowid = 0;
            DataRow currentrow = returndata.NewRow();
            //returndata.Rows.Add(currentrow);
            
            // Initialize column object
            DataColumn currentcol;
            
            // Loop through row, col, value records
            while (reader.Read())
            {  
                // If new row in source data, add row to data table
                if (rowid != (int)reader["row_id"])
                {
                    // Add row
                    currentrow = returndata.NewRow();
                    returndata.Rows.Add(currentrow);
                    
                    // Set row counter to new row
                    rowid = (int)reader[0];
                }

                // If still the first row, any new record will be an additional column
                if ((int)reader["row_id"] == 0)
                {
                    // Create new column
                    currentcol = new DataColumn();
                    currentcol.ColumnName = (string)reader[2];
                    returndata.Columns.Add(currentcol);
                }
                else
                {
                    // In any case, add value to current rol, col
                    currentrow[(int)reader[1]] = reader[2];
                }
            }

            // After loop through records, return temporary datatable
            return returndata;
        }

        // CONTROL DATA RETRIEVE -------------------------------------------------------------------------------------------------------

        string[,,] control_dataretrieve(Control outputcontrol)
        {          
            // Data to write - row, col, value
            string[, ,] datatowrite;

            // Dimensions of data - will come from specific control write implementations
            int max_rows;
            int max_cols;
            
            switch(outputcontrol.GetType().ToString())
            {
                case "System.Web.UI.WebControls.TextBox":
                {
                    max_rows = 1; max_cols = 1;
                    datatowrite = new string[max_rows, max_cols, 1];

                    // Create temporary text box object to retrieve value from generic control
                    TextBox datapull = new TextBox();
                    datapull = (TextBox)outputcontrol;

                    // Get value from text box
                    datatowrite[0,0,0] = datapull.Text;

                    // SESSION MODIFICATIONS //
                    DataMiningApp.Analysis.ParameterStream stream = DataMiningApp.Analysis.ParameterStream.getStream(Session);
                    stream.set("numberOfPCs", int.Parse(datapull.Text));

                    //-----------------------//

                    break;
                }
                case "System.Web.UI.WebControls.FileUpload":
                {
                    FileUpload uploadcontrol = (FileUpload)outputcontrol;
                    string id = outputcontrol.ID;

                    // Create GridView object
                    GridView datatable = (GridView)Form.FindControl(id + "_table");

                    // Find number of rows in GridView
                    max_rows = datatable.Rows.Count;

                    // If data exists, then fill datawrite array in row, col, val format with data
                    if (max_rows > 0)
                    {
                        max_cols = datatable.Rows[0].Cells.Count;

                        datatowrite = new string[max_rows + 1, max_cols, 1];

                        for (int i = 1; i <= max_cols; i++)
                        {
                            datatowrite[0, i - 1, 0] = datatable.HeaderRow.Cells[i - 1].Text;
                        }

                        for(int j = 0; j < max_rows; j++)
                        {
                            for (int k = 0; k < max_cols; k++)
                            {
                                datatowrite[j+1,k,0] = datatable.Rows[j].Cells[k].Text;
                            }
                        }

                    }
                    // If no data, fill with garbage
                    else
                    {
                        datatowrite = new string[1, 1, 1];
                        datatowrite[0, 0, 0] = null;
                    }

                    break;
                }
                case "System.Web.UI.WebControls.ListBox":
                {
                    ListBox listboxcontrol = (ListBox)outputcontrol;

                    DataMiningApp.Analysis.Analysis analysis = (DataMiningApp.Analysis.Analysis)Session["analysis"];
                    DataMiningApp.Analysis.ParameterStream stream = DataMiningApp.Analysis.ParameterStream.getStream(Session);
                    String[] features = new String[listboxcontrol.GetSelectedIndices().Count()];
                    for (int i = 0; i < listboxcontrol.GetSelectedIndices().Count(); i++)
                    {
                        features[i] = listboxcontrol.Items[listboxcontrol.GetSelectedIndices()[i]].Text;
                    }
                    stream.set("selectedFeatures", features);

                    datatowrite = new string[1, 1, 1];
                    datatowrite[0, 0, 0] = null;

                    break;
                }
                // Non-writing control, just fill with garbage
                default:
                {
                    datatowrite = new string[1, 1, 1];
                    datatowrite[0, 0, 0] = null;
                    break;
                }

            }

            return datatowrite;
        }

        // INSERT DATA IN DATABASE -----------------------------------------------------------------------------------------------------

        void datawrite(string[, ,] datatowrite, string dataname, string control_id)
        {
            int row_counter; int col_counter;
            string execute_query;
            int total_rows = datatowrite.GetLength(0);
            int total_cols = datatowrite.GetLength(1);

            // Check if fill query is specified
            if (dataname != "NONE" && dataname != "")
            {
                // Add critical keys for data write to ALGORITHM_DATASTORE
                // JobID, StepID, Data_Name, Row_ID, Column_ID, Value

                for (row_counter = 0; row_counter < total_rows; row_counter++)
                {
                    for (col_counter = 0; col_counter < total_cols; col_counter++)
                    {
                        // Construct query
                        execute_query = "WEBAPP_WRITE " + jobid + ",'" + dataname + "'," + row_counter + "," + col_counter + ",'" + datatowrite[row_counter, col_counter, 0] + "'";
                        
                        // Initialize reader and get data
                        reader = openconnection(execute_query, connection);
                        reader.Read();
                        closeconnection(reader, connection);
                    }
                }    
            }
        }

        // UPLOAD BUTTON HANDLER -------------------------------------------------------------------------------------------------------

        protected void uploadbutton_Click(object sender, EventArgs e)
        {
            // Get button ID
            Button getbuttonID = (Button)sender;
            string id = getbuttonID.ID.Replace("_button","");

            // Use button ID to find similarly named upload control ID
            FileUpload uploadcontrol = (FileUpload)Form.FindControl(id);

            // Only upload if control has file selected
            if (uploadcontrol.HasFile)
            {
                // Add upload path
                String savePath = @"c:\temp\";

                // Retrieve filename from upload control
                String fileName = uploadcontrol.FileName;

                // Save data to web server
                uploadcontrol.SaveAs(savePath + fileName);

                // Fill GridView

                // Establish text driver connection
                System.Data.Odbc.OdbcConnection csv_connection;
                System.Data.Odbc.OdbcDataAdapter csv_adapter;

                // Create temporary data table to store CSV data
                DataTable csv_data = new DataTable();

                // Create connection string and execute connection to CSV
                string csv_connectionString = @"Driver={Microsoft Text Driver (*.txt; *.csv)};Dbq=" + savePath + ";";
                csv_connection = new System.Data.Odbc.OdbcConnection(csv_connectionString);

                // Fill adapter with SELECT * query from CSV
                csv_adapter = new System.Data.Odbc.OdbcDataAdapter("select * from [" + fileName + "]", csv_connection);
                csv_adapter.Fill(csv_data);

                // Close CSV connection
                csv_connection.Close();

                // Find GridView and fill
                GridView filedata = (GridView)Form.FindControl(id + "_table");
                filedata.DataSource = csv_data;
                filedata.DataBind();

                // SESSION MODIFICATION //

                DataSet session_datanew = new DataSet();
                session_datanew.Tables.Add(csv_data);
                session_datanew.DataSetName = "PCADATA";

                Registry.Registry registry = Registry.Registry.getRegistry(Session);
                registry.registerDataset(session_datanew);
                Analysis.ParameterStream stream = Analysis.ParameterStream.getStream(Session);
                stream.set("dataSetName", "PCADATA");

                //----------------------//
            }
        }
        
        // NEXT BUTTON HANDLER ---------------------------------------------------------------------------------------------------------

        protected void next_button_Click(object sender, EventArgs e)
        {
            // Create template control to operate on
            Control testcontrol;

            // Data storage set
            string[, ,] datatowrite;

            // Loop through cells in layout table looking for controls
            for (int row_traverse = 0; row_traverse < max_layout_rows; row_traverse++)
            {
                for (int col_traverse = 0; col_traverse < max_layout_cols; col_traverse++)
                {
                    // Check if cell has a control
                    testcontrol = (Control)Form.FindControl("control_" + col_traverse + "_" + row_traverse);

                    // If so, call data write function
                    if (testcontrol != null)
                    {
                        datatowrite = control_dataretrieve(testcontrol);
                        if (datatowrite[0,0,0] != null)
                        {
                            //datawrite(datatowrite, controlarray[col_traverse, row_traverse, 2], testcontrol.ID);
                        }                 
                    }
                }
            }

            // ALGORITHM STEP RUN -------------------------------------


            Analysis.Analysis analysis = (Analysis.Analysis) Session["analysis"];
            analysis.next(Response, Session);
    
            /*

            // Create instance of algorithm class (deleted on postback)
            testalgorithm myalg = new testalgorithm();

            // Call algorithm method with current step id
            myalg.supermethod(algorithmid, stepid, jobid);
            
            // Move to next step
            Session["stepid"] = (int)Session["stepid"] + 1;
            Response.Redirect("Default.aspx", false);
             * 
             */
        }

    }
}
