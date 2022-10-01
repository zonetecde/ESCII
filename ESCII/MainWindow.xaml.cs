using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using Image = System.Windows.Controls.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;

namespace ESCII
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Random rdn = new Random();

        internal static BackgroundWorker bgWorker_decryptage = new BackgroundWorker();
        internal static BackgroundWorker bgWorker_cryptage = new BackgroundWorker();

        internal static Bitmap output;

        public MainWindow()
        {
            InitializeComponent();

            bgWorker_decryptage.WorkerReportsProgress = true;
            bgWorker_decryptage.ProgressChanged += new ProgressChangedEventHandler(bgWorker_decrypt_progressChanged);
            bgWorker_decryptage.DoWork += new DoWorkEventHandler(bgWorker_decrypt_doWork);
           
        }

        private void bgWorker_crypt_progressChanged(object? sender, ProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar_cryptage.Value = e.ProgressPercentage;

                double p = Math.Round(((e.ProgressPercentage / progressBar_cryptage.Maximum) * 100), 2);
                button_crypt.Content = p.ToString().PadLeft(3) + "%";
            });
        }

        private void bgWorker_crypt_doWork(object? sender, DoWorkEventArgs e)
        {
            string text= "";
            bool? isChecked = false;
            string key = "";


            Bitmap r = (e.Argument as Bitmap);

            Dispatcher.Invoke(() =>
            {
                text = new TextRange(richTextBox_code.Document.ContentStart, // texte
                richTextBox_code.Document.ContentEnd).Text;

                isChecked = cb_useKey.IsChecked;
                key = txtBox_Key.Text;
            });



            Encryption.Encrypt(r,
                text, isChecked == true ? key : String.Empty);

        }

        private void Button_Encrypter_Click(object sender, RoutedEventArgs e)
        {
            string text = new TextRange(richTextBox_code.Document.ContentStart, // texte
                richTextBox_code.Document.ContentEnd).Text;

            if (text != "\r\n") // = vide
                foreach (Border border in wrapPanel_images.Children)
                {
                    if (border.BorderThickness.Left == 2)
                    {
                        var b = ConvertToBitmap(
                                (BitmapSource)(border.Child as Image).Source);
                    
                        if (cb_useKey.IsChecked == true)
                            if (txtBox_Key.Text.Length < 20)
                            {
                                MessageBox.Show("La clé doit faire 20 caractères.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        bgWorker_cryptage = new BackgroundWorker();
                        bgWorker_cryptage.WorkerReportsProgress = true;
                        bgWorker_cryptage.ProgressChanged += new ProgressChangedEventHandler(bgWorker_crypt_progressChanged);
                        bgWorker_cryptage.DoWork += new DoWorkEventHandler(bgWorker_crypt_doWork);

                        progressBar_cryptage.Maximum = b.Width;
                        bgWorker_cryptage.RunWorkerAsync(b);

                        bgWorker_cryptage.RunWorkerCompleted += new RunWorkerCompletedEventHandler((sender, e) =>
                        {
                            progressBar_cryptage.Value = 0;
                            button_crypt.Content = "Encrypter";

                            if (output != null)
                            {
                                var dialog = new CommonOpenFileDialog();
                                dialog.IsFolderPicker = true;
                                CommonFileDialogResult result = dialog.ShowDialog();

                                if (result == CommonFileDialogResult.Ok)
                                {
                                    output.Save(dialog.FileName + @"\output " + DateTime.Now.ToString("dd_MM_yyyy HH_mm_ss") + ".png", ImageFormat.Png);
                                    output.Dispose();
                                }
                            }
                            else
                            {
                                // réessaie avec une autre clé
                                if (cb_useKey.IsChecked == true)
                                {
                                    for (int i = 0; i < 20; i++)
                                    {
                                        string newK = GénérerClé();
                                        Bitmap btm = Encryption.Encrypt(b,
                                            text, newK, false);

                                        if (btm != null)
                                        {
                                            if (MessageBox.Show("La clé actuelle ne permet pas d'encoder cette image.\nVoulez-vous utiliser la clé \"" + newK + "\" ? \nElle sera enregistrer dans le presse papier.", "Information", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                                            {
                                                Clipboard.SetText(newK);

                                                var dialog = new CommonOpenFileDialog();
                                                dialog.IsFolderPicker = true;
                                                CommonFileDialogResult result = dialog.ShowDialog();

                                                if (result == CommonFileDialogResult.Ok)
                                                {
                                                    btm.Save(dialog.FileName + @"\output " + DateTime.Now.ToString("dd_MM_yyyy HH_mm_ss") + ".png", ImageFormat.Png);
                                                    btm.Dispose();
                                                }
                                            }

                                            return;
                                        }

                                    }
                                }
                                MessageBox.Show("L'image est trop petite pour contenir votre message secret. Veuillez en choisir une autre.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        });


                       
                    
                    }
                }
            else
                MessageBox.Show("Veuillez écrire la chose à encrypter dans l'image.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            //MessageBox.Show(Encryption.Decryption(new Bitmap("output474.png")));

        }

        private void Import_Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.DefaultExt = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;";
            dlg.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;

                Border b = new Border();
                var img = new System.Windows.Controls.Image()
                {
                    Source = new BitmapImage(new Uri(filename)),
                    Margin = new Thickness(10),
                    Cursor = Cursors.Hand,
                };
                b.Child = img;

                // sélectionne
                b.BorderBrush = Brushes.Yellow;
                b.BorderThickness = new Thickness(2);

                b.MouseDown += new MouseButtonEventHandler(ImageSelected);

                // enlève effet sélectionner des autres
                foreach (Border border in wrapPanel_images.Children)              
                    border.BorderThickness = new Thickness(0);
                

                wrapPanel_images.Children.Insert(1, b);
            }
        }

        private void ImageSelected(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
                foreach(Border border in wrapPanel_images.Children)
                {
                    border.BorderThickness = new Thickness(0);

                    if (border == (sender as Border))
                        border.BorderThickness = new Thickness(2);
                }
        }

        private Bitmap ConvertToBitmap(BitmapSource bitmapSource)
        {
            var width = bitmapSource.PixelWidth;
            var height = bitmapSource.PixelHeight;
            var stride = width * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
            var memoryBlockPointer = Marshal.AllocHGlobal(height * stride);
            bitmapSource.CopyPixels(new Int32Rect(0, 0, width, height), memoryBlockPointer, height * stride, stride);
            var bitmap = new Bitmap(width, height, stride, PixelFormat.Format32bppPArgb, memoryBlockPointer);
            return bitmap;
        }

        private void Button_GénérerClé_Click(object sender, RoutedEventArgs e)
        {
            string password = GénérerClé();
            txtBox_Key.Text = password;
        }

        private string GénérerClé()
        {
            string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890&[]}{)(@*$%/\\+-<>&[]}{)(@*$%/\\+-<>";
            string password = string.Empty;
            for (int i = 0; i < 20; i++)
            {
                password += valid[rdn.Next(0, valid.Length)];
            }

            return password;
        }

        private void txtBox_Key_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                grid_usekey.Visibility = Visibility.Hidden;
        }

        private void txtBox_Key_TextChanged(object sender, TextChangedEventArgs e)
        {
            label_nbChar.Content = txtBox_Key.Text.Length.ToString().PadLeft(2) + "/20";
        }

        private void grid_usekey_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                grid_usekey.Visibility = Visibility.Hidden;
        }

        private void Button_CloseGridKey_Click(object sender, RoutedEventArgs e)
        {
            grid_usekey.Visibility = Visibility.Hidden;
        }

        private void cb_useKey_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void Button_ChoisirImageDécrypter_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.DefaultExt = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;";
            dlg.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;
                (sender as Button).Content = filename;
            }
        }

        internal static string SecretMessage;

        private void Button_Décrypter_Click(object sender, RoutedEventArgs e)
        {
            richTextBox_output.Document.Blocks.Clear();

            if(!button_decrypter.Content.ToString().Contains("%"))
            if(!button_imageFile.Content.ToString().Contains("Choisir une image") )
            {
                if (txtBox_decryptKey.Text.Length == 0 || txtBox_decryptKey.Text.Length == 20)
                {
                    bgWorker_decryptage.RunWorkerAsync(new Bitmap(button_imageFile.Content.ToString()));

                    bgWorker_decryptage.RunWorkerCompleted += new RunWorkerCompletedEventHandler((sender, e) =>
                    {
                        progressBar_decryptage.Value = 0;
                        button_decrypter.Content = "Décrypter";
                        richTextBox_output.AppendText(SecretMessage);
                    });
                }
                else
                    MessageBox.Show("Clé invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
                MessageBox.Show("Veuillez choisir une image", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Décryptage worker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void bgWorker_decrypt_doWork(object? sender, DoWorkEventArgs e)
        {
            string key = "";
            Dispatcher.Invoke(() =>
            {
                key = txtBox_decryptKey.Text;
                progressBar_decryptage.Maximum = (e.Argument as Bitmap).Width - 2;
            });
            Encryption.Decryption(e.Argument as Bitmap, key);
            
        }

        /// <summary>
        /// Décryptage progressChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void bgWorker_decrypt_progressChanged(object? sender, ProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar_decryptage.Value = e.ProgressPercentage;

                double p = Math.Round(((e.ProgressPercentage / progressBar_decryptage.Maximum) * 100), 2);
                button_decrypter.Content = p.ToString().PadLeft(3) + "%";
            });
        }

        private void cb_useKey_Checked(object sender, RoutedEventArgs e)
        {
            if (cb_useKey.IsChecked == true)
            {
                grid_usekey.Visibility = Visibility.Visible;
            }
        }
    }
}
