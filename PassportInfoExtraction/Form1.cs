using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Yolov5Net.Scorer;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;
using Tesseract;

namespace PassportInfoExtraction
{
    public partial class Form1 : Form
    {
        public float scale_x, scale_y;
        public int image_width, image_height;
        TesseractEngine Ocr;
        public static Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
        {
            var destRect = new System.Drawing.Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public void Initialize()
        {
            scale_x = 1;
            scale_y = 1;

            image_width = 640;
            image_height = 640;

            Ocr = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            Ocr.SetVariable("tessedit_char_whitelist", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ,.-+< ");

            comboBox1.Items.Add("Full Mode");
            comboBox1.Items.Add("Half-Top Mode");
            comboBox1.Items.Add("Half-Bottom Mode");
            comboBox1.SelectedIndex = 0;
        }
        public Form1()
        {
            InitializeComponent();
            Initialize();
        }

        public YoloPrediction ScaleConvertion(YoloPrediction prediction)
        {
            YoloPrediction scaled_prediction = new YoloPrediction();

            float _x = prediction.Rectangle.X;
            float _y = prediction.Rectangle.Y;
            float _width = prediction.Rectangle.Width;
            float _height = prediction.Rectangle.Height;

            scaled_prediction.Label = prediction.Label;
            scaled_prediction.Score = prediction.Score;
            scaled_prediction.Rectangle = new RectangleF(_x * scale_x, _y * scale_y, _width * scale_x, _height * scale_y);

            return scaled_prediction;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // Bitmap bitmap_0 = new Bitmap(dialog.FileName);

                Mat original_mat = new Mat(dialog.FileName);

                var scorer = new YoloScorer<PassportModel>("best.onnx");

                List<YoloPrediction> predictions = scorer.Predict(original_mat.ToBitmap());

                int temp_width = original_mat.Width;
                int temp_height = original_mat.Height;

                comboBox1.SelectedIndex = 0;
                if (temp_width >= temp_height)
                {
                    comboBox1.SelectedIndex = 1;
                    foreach (var prediction in predictions)
                    {
                        if (prediction.Label.Name == "bottom" || prediction.Label.Name == "photo" || prediction.Label.Name == "sign")
                        {
                            comboBox1.SelectedIndex = 2;
                            break;
                        }
                    }
                }


                if (comboBox1.SelectedIndex == 1)
                {
                    Cv2.Resize(original_mat, original_mat, new Size(1200, 800));
                    Mat black_mat = Mat.Zeros(new Size(1200, 800), original_mat.Type());
                    Cv2.VConcat(original_mat, black_mat, original_mat);
                } 
                else if (comboBox1.SelectedIndex == 2)
                {
                    Cv2.Resize(original_mat, original_mat, new Size(1200, 800));
                    Mat black_mat = Mat.Zeros(new Size(1200, 800), original_mat.Type());
                    Cv2.VConcat(black_mat, original_mat, original_mat);
                }

                Cv2.Resize(original_mat, original_mat, new Size(1200, 1600));

                Bitmap bitmap_0 = original_mat.ToBitmap();

                pictureBox1.Image = bitmap_0;

                scale_x = (float)(bitmap_0.Width) / (float)(image_width);
                scale_y = (float)(bitmap_0.Height) / (float)(image_height);

                var resized_image = ResizeImage(bitmap_0, image_width, image_height);

                scorer = new YoloScorer<PassportModel>("best.onnx");

                predictions = scorer.Predict(resized_image);

                List<YoloPrediction> scaled_predictions = predictions.ConvertAll(new Converter<YoloPrediction, YoloPrediction>(ScaleConvertion));

                scaled_predictions.Sort(new DetectionScoreComparer());

                float STRETCH_SCALE = 1f;
                int DATA_MARGIN = 10;
                string[] ORDER_IDS = { "Name", "Father's Name", "Mother's Name", "Spouse's Name", "Permanent Address", "Emergency Contact Name", "Relationship", "Address", "Telephone No", "Type", "Country Code", "Passport Number", "Surname", "Given Name", "Nationality", "Personal No.", "Date of Birth", "Previous Passport", "Sex", "Place of Birth", "No.", "Date of issue", "Issuing Authority", "Date of Expiry" };
                int MIN_CHARACTER_HEIGHT = 12;
                int MAX_CHARACTER_HEIGHT = 25;

                YoloPrediction prv_data = null;
                int data_counter = 0;
                if (comboBox1.SelectedIndex == 2)
                {
                    data_counter = 9;
                }
                Dictionary<string, string> result_dictionary = new Dictionary<string, string>();
                foreach (var prediction in scaled_predictions) // iterate predictions to draw results
                {
                    int _x, _y, _width, _height;

                    _x = (int)(prediction.Rectangle.X);
                    _y = (int)(prediction.Rectangle.Y);
                    _width = (int)(prediction.Rectangle.Width);
                    _height = (int)(prediction.Rectangle.Height);

                    switch (prediction.Label.Name)
                    {
                        case "bottom":
                            Rect bottom_rect = new Rect(_x, _y, _width, _height);
                            Mat bottom_mat = original_mat[bottom_rect];

                            Mat bottom_gray_img = new Mat();
                            Cv2.CvtColor(bottom_mat, bottom_gray_img, ColorConversionCodes.BGR2GRAY);

                            Mat bottom_resized_img = new Mat();
                            Cv2.Resize(bottom_gray_img, bottom_resized_img, new OpenCvSharp.Size(bottom_gray_img.Width * STRETCH_SCALE, bottom_gray_img.Height * STRETCH_SCALE));

                            Mat bottom_binary_image = new Mat();
                            double bottom_minVal, bottom_maxVal;
                            bottom_resized_img.MinMaxIdx(out bottom_minVal, out bottom_maxVal);
                            Cv2.Threshold(bottom_resized_img, bottom_binary_image, bottom_minVal + (bottom_maxVal - bottom_minVal) / 2.1f, 255, ThresholdTypes.Binary);

                            using (var page = Ocr.Process(bottom_binary_image.ToBitmap()))
                            {
                                var Result = page.GetText().Split('\n');
                                string candidate_string = "";

                                foreach (var line in Result)
                                {
                                    if (line.Length > 0)
                                        candidate_string = line;
                                }

                                string surname, lastname;
                                surname = result_dictionary["Surname"];
                                lastname = result_dictionary["Given Name"];
                                string line01 = "P<BGD" + surname + "<<";
                                foreach (var name_piece in lastname.Split(' '))
                                {
                                    line01 += name_piece + "<";
                                }
                                line01 += String.Concat(Enumerable.Repeat("<", 45 - line01.Length));
                                result_dictionary.Add("Line 01", line01);

                                string passport_number = result_dictionary["Passport Number"].Replace(" ", String.Empty);

                                if (passport_number.Length == 9 && candidate_string.Length > 9)
                                {
                                    if (passport_number[0] == '4') passport_number = "A" + passport_number.Substring(1);
                                    candidate_string = passport_number + candidate_string.Substring(9);
                                    result_dictionary["Passport Number"] = passport_number;
                                }
                                if (passport_number.Length < 5 && candidate_string.Length > 9)
                                {
                                    if (candidate_string[0] == '4') candidate_string = "A" + candidate_string.Substring(1);
                                    result_dictionary["Passport Number"] = candidate_string.Substring(0, 9);
                                }
                                if (candidate_string.Length > 15) candidate_string = candidate_string.Substring(0, 10) + "BGD" + candidate_string.Substring(13);
                                result_dictionary.Add("Line 02", candidate_string);
                            }
                            try
                            {
                                if (result_dictionary["Name"].Length < 2 && result_dictionary["Surname"].Length > 1 && result_dictionary["Given Name"].Length > 1)
                                {
                                    result_dictionary["Name"] = result_dictionary["Given Name"] + result_dictionary["Surname"];
                                }
                            }
                            catch(Exception ex)
                            {

                            }
                            break;
                        case "photo":
                            Rect photo_rect = new Rect(_x, _y, _width,_height);
                            Mat photo_mat = original_mat[photo_rect];
                            pictureBox2.Image = photo_mat.ToBitmap();
                            break;
                        case "sign":
                            Rect sign_rect = new Rect(_x, _y, _width, _height);
                            Mat sign_mat = original_mat[sign_rect];
                            pictureBox3.Image = sign_mat.ToBitmap();
                            break;
                        case "data":
                            
                            Rect data_rect = new Rect(_x - DATA_MARGIN, _y - DATA_MARGIN, _width + 2 * DATA_MARGIN, _height + 2 * DATA_MARGIN);
                            Mat data_mat = original_mat[data_rect];

                            // Cv2.Rectangle(original_mat, data_rect, new Scalar(0));

                            Mat gray_img = new Mat();
                            Cv2.CvtColor(data_mat, gray_img, ColorConversionCodes.BGR2GRAY);
                            
                            Mat resized_img = new Mat();
                            Cv2.Resize(gray_img, resized_img, new OpenCvSharp.Size(gray_img.Width * STRETCH_SCALE, gray_img.Height * STRETCH_SCALE));

                            Mat binary_image = new Mat();
                            double minVal, maxVal;
                            resized_img.MinMaxIdx(out minVal, out maxVal);
                            Cv2.Threshold(resized_img, binary_image, minVal + (maxVal - minVal) / 2.1f, 255, ThresholdTypes.Binary);

                            OpenCvSharp.Point[][] contours;
                            HierarchyIndex[] hierarchyIndices;
                            Cv2.FindContours(binary_image, out contours, out hierarchyIndices, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

                            int _y_top = binary_image.Height;
                            int _y_bottom = 0;
                            foreach(var contour in contours)
                            {
                                Rect bound_box = Cv2.BoundingRect(contour);
                                
                                if (bound_box.Height > MIN_CHARACTER_HEIGHT && bound_box.Height < MAX_CHARACTER_HEIGHT)
                                {
                                    // Cv2.Rectangle(binary_image, bound_box, new Scalar(0));
                                    if (bound_box.Y < _y_top) _y_top = bound_box.Y;
                                    if(bound_box.Y + bound_box.Height > _y_bottom) _y_bottom = bound_box.Y + bound_box.Height;
                                }
                            }
                            Rect top_box = new Rect(0, 0, binary_image.Width, _y_top);
                            Rect bottom_box = new Rect(0, _y_bottom, binary_image.Width, binary_image.Height - _y_bottom);

                            // Debug.Write("data_counter : " + data_counter.ToString());
                            // Window.ShowImages(binary_image);
                            // Cv2.WaitKey(0);

                            try
                            {
                                binary_image[top_box].SetTo(new Scalar(255));
                            }
                            catch{ }
                            try
                            {
                                binary_image[bottom_box].SetTo(new Scalar(255));
                            }
                            catch { }

                            if (prv_data != null)
                            {
                                int compare_result = StaticMethods.CompareYoloPrediction(prediction, prv_data);
                                if (compare_result == 0)
                                {
                                    if (prediction.Score > prv_data.Score)
                                    {
                                        data_counter -= 1;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }

                            using (var page = Ocr.Process(binary_image.ToBitmap()))
                            {
                                var Result = page.GetText().Split('\n');
                                textBox1.Text = "";
                                string candidate_string = "";

                                foreach (var line in Result)
                                {
                                    candidate_string += line + " ";
                                }
                                textBox1.Text = candidate_string;

                                if((data_counter == 4 || data_counter == 7) && prediction.Rectangle.Y - prv_data.Rectangle.Y > 50)
                                {
                                    result_dictionary.Add(ORDER_IDS[data_counter], "");
                                    data_counter++;
                                }

                                if (data_counter == 17 && _x - DATA_MARGIN < bitmap_0.Width / 2.7f)
                                {
                                    result_dictionary.Add(ORDER_IDS[data_counter], "");
                                    data_counter++;
                                }
                                // Skip if there is no Spouse
                                if(data_counter == 3 && prediction.Rectangle.Y - prv_data.Rectangle.Y > 50)
                                {
                                    result_dictionary.Add(ORDER_IDS[data_counter], "");
                                    data_counter++;
                                }
                                // Skip if there is no Given name
                                if (data_counter == 13 && prediction.Rectangle.Y - prv_data.Rectangle.Y > 65)
                                {
                                    result_dictionary.Add(ORDER_IDS[data_counter], "");
                                    data_counter++;
                                }
                                // Set Gender
                                if (data_counter == 18 && candidate_string != "F") candidate_string = "M";
                                // Set Nationality
                                if (data_counter == 14) candidate_string = "BANGLADESHI";
                                // Set Country Code
                                if (data_counter == 10) candidate_string = "BGD";
                                // Set Type
                                if (data_counter == 9) candidate_string = "P";
                                // Set Issuing Authority
                                if (data_counter == 22) candidate_string = "DIP/DHAKA";

                                try
                                {
                                    result_dictionary.Add(ORDER_IDS[data_counter], candidate_string);
                                }
                                catch(Exception)
                                {

                                }

                                data_counter++;
                            }
                            prv_data = prediction;

                            // Window.ShowImages(binary_image);
                            // Cv2.WaitKey(0);

                            break;
                    }
                }

                // Export to Textbox as JSON Format
                textBox1.Text = "";
                foreach(var item in result_dictionary)
                {
                    textBox1.AppendText(item.Key + " : " + item.Value + Environment.NewLine);
                }

                pictureBox1.Image = original_mat.ToBitmap();

            }
        }
    }
}
