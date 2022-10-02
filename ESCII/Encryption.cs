using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ESCII
{
    internal static class Encryption
    {
        /// <summary>
        /// Décrypte l'image
        /// </summary>
        /// <param name="bitmap_img"></param>
        /// <returns></returns>
        internal static string Decryption(Bitmap bitmap_img, string key)
        {
            string code = String.Empty;
            int compteur = 0;

            // si clé on prend ses infos
            int minX = 1;
            int minY = 1;
            int pasX = 2;
            int pasY = 2;
            if (!String.IsNullOrEmpty(key))
            {
                // min X : 5, 3, 8, 9, 0
                minX = Convert.ToInt32(Math.Abs((int)key[5] + (int)key[3] - (int)key[8] * (int)key[9] - (int)key[0]).ToString().Remove(2));

                // min Y : 1, 15, 19, 4
                minY = Convert.ToInt32(Math.Abs((int)key[1] - (int)key[15] - (int)key[19] * (int)key[4]).ToString().Remove(2));

                // pas X : 2, 14, 17,  2
                pasX = Convert.ToInt32(Math.Abs((int)key[2] - (int)key[14] * (int)key[17] * (int)key[2]).ToString().Remove(2));

                // pas Y : 6, 10, 7,  11
                pasX = Convert.ToInt32(Math.Abs((int)key[6] * (int)key[10] + (int)key[7] - (int)key[11]).ToString().Remove(2));

            }

            //bitmap_img bitmap_img = new bitmap_img(bitmap_img.Width, bitmap_img.Height, bitmap_img);

            for (int x = minX; x < bitmap_img.Width - 1; x+= pasX)
            {
                MainWindow.bgWorker_decryptage.ReportProgress(x, code);

                for (int y = minY; y < bitmap_img.Height - 1; y+= pasY)
                {
                    if (!MainWindow.bgWorker_decryptage.CancellationPending)
                    {
                        // ré init max char
                        if (compteur == 253)
                        {
                            compteur = 0;
                        }

                        // pixel haut bas gauche droite de la même couleur
                        Color centerPixel = bitmap_img.GetPixel(x, y);

                        if (centerPixel == bitmap_img.GetPixel(x - 1, y) &&
                           centerPixel == bitmap_img.GetPixel(x + 1, y) &&
                           centerPixel == bitmap_img.GetPixel(x, y + 1) &&
                           centerPixel == bitmap_img.GetPixel(x, y - 1)
                           )
                        {
                            char lettre = (char)compteur; ;


                            code += lettre;
                        }

                        compteur++;
                    }
                    else
                    {
                        MainWindow.didCancel = true;
                        return String.Empty;
                    }
                }
            }

            MainWindow.SecretMessage = code;
            return code;
        }

        /// <summary>
        /// Encrypte l'image
        /// </summary>
        /// <param name="bitmap_img">L'image a coder</param>
        /// <param name="code">Le message secret</param>
        internal static Bitmap Encrypt(Bitmap bitmap_img, string code, string key, bool report = true)
        {
            try
            {
                // variable temporaire pour compter
                int compteur = 0;

                // les pixels où le message sera encodé
                List<int> pixelsToReplace = new List<int>();
                // récupération des pixels où le message sera encodé
                // pour chaque lettre du code secret
                code.ToCharArray().ToList().ForEach(letter =>
                {
                    // on récupère sa valeur en nombre char
                    int letterCode = letter;

                    // le convertit en pixel a remplacé
                    pixelsToReplace.Add(compteur + letterCode);

                    // 253 = max char ASCII
                    compteur += 253;
                });

                // ré init le compteur
                compteur = 0;
                int letteurDone = 0;

                // si clé on prend ses infos
                int minX = 1;
                int minY = 1;
                int pasX = 2;
                int pasY = 2;

                if(!String.IsNullOrEmpty(key))
                {
                    // min X : 5, 3, 8, 9, 0
                    minX = Convert.ToInt32(Math.Abs((int)key[5] + (int)key[3] - (int)key[8] * (int)key[9] - (int)key[0]).ToString().Remove(2));
                    
                    // min Y : 1, 15, 19, 4
                    minY = Convert.ToInt32(Math.Abs((int)key[1] - (int)key[15] - (int)key[19] * (int)key[4]).ToString().Remove(2));

                    // pas X : 2, 14, 17,  2
                    pasX = Convert.ToInt32(Math.Abs((int)key[2] - (int)key[14] * (int)key[17] * (int)key[2]).ToString().Remove(2));
                                        
                    // pas Y : 6, 10, 7,  11
                    pasX = Convert.ToInt32(Math.Abs((int)key[6]* (int)key[10] + (int)key[7] - (int)key[11]).ToString().Remove(2));

                }

                //bitmap_img bitmap_img = new bitmap_img(bitmap_img.Width, bitmap_img.Height, bitmap_img);

                for (int x = minX; x < bitmap_img.Width - 1; x += pasX)
                {
                    if(report)
                    MainWindow.bgWorker_cryptage.ReportProgress(x);

                    for (int y = minY; y < bitmap_img.Height - 1; y += pasY)
                    {
                        if (!MainWindow.bgWorker_cryptage.CancellationPending)
                        {
                            // Si 5 pixels sont de la même couleur exactement et que le pixel ne fait pas partie de la liste secrète
                            Color centerPixel = bitmap_img.GetPixel(x, y);

                            if (centerPixel == bitmap_img.GetPixel(x - 1, y) &&
                                centerPixel == bitmap_img.GetPixel(x + 1, y) &&
                                centerPixel == bitmap_img.GetPixel(x, y + 1) &&
                                centerPixel == bitmap_img.GetPixel(x, y - 1)
                                && !pixelsToReplace.Contains(compteur))
                            {
                                // on modifie alors le pixel pour que il n'y est plus que 4 pixels de la même couleur
                                if (centerPixel.R < 255)
                                {
                                    bitmap_img.SetPixel(x, y, Color.FromArgb(255,
                                        bitmap_img.GetPixel(x, y).R + 1, bitmap_img.GetPixel(x, y).G, bitmap_img.GetPixel(x, y).B));
                                }
                                else
                                {
                                    bitmap_img.SetPixel(x, y, Color.FromArgb(255,
                                        bitmap_img.GetPixel(x, y).R - 1, bitmap_img.GetPixel(x, y).G, bitmap_img.GetPixel(x, y).B));
                                }
                            }
                            else if (pixelsToReplace.Contains(compteur)) // si le pixel fait partie du code secret
                            {
                                // les pixels haut bas gauche & droite doivent être de la même couleur
                                bitmap_img.SetPixel(x - 1, y, centerPixel);
                                bitmap_img.SetPixel(x + 1, y, centerPixel);
                                bitmap_img.SetPixel(x, y - 1, centerPixel);
                                bitmap_img.SetPixel(x, y + 1, centerPixel);
                            }

                            if (pixelsToReplace.Contains(compteur))
                                letteurDone++;

                            compteur++;
                        }
                        else
                        {
                            MainWindow.didCancel = true;
                            return null;
                        }
                    }
                }

                if (letteurDone == pixelsToReplace.Count)
                {
                    MainWindow.output = bitmap_img;
                    return bitmap_img;
                }
                else
                {
                    MainWindow.output = null;
                    return null; // image trop petite
                }
            }
            catch
            {
                MainWindow.output = null;
                return null;
            }
        }
    }
}
