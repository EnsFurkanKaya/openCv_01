using OpenCvSharp;
using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using OpenCvSharp.Extensions; // <-- BU SATIRI EKLEYİN

namespace openCv_01
{
    public partial class Form1 : Form
    {

        private CascadeClassifier _faceCascade;
        private Mat _currentImage;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            // Model dosyasının yolu (Copy if newer yaptığımız için exe'nin yanında olacak)
            string cascadePath = Path.Combine(Application.StartupPath, "haarcascade_frontalface_default.xml");

            // Model dosyasının varlığını kontrol et
            if (!File.Exists(cascadePath))
            {
                MessageBox.Show("Gerekli model dosyası bulunamadı: " + cascadePath, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnLoadImage.Enabled = false; // Model yoksa resim yüklemeyi de kapat
                return;
            }

            // Sınıflandırıcıyı yükle
            _faceCascade = new CascadeClassifier(cascadePath);

        }

        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Resim Dosyaları (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                ofd.Title = "Bir Resim Seçin";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Varsa bir önceki resmi bellekten temizle
                        _currentImage?.Dispose();

                        // Resmi OpenCvSharp'ın Mat formatında yükle
                        _currentImage = new Mat(ofd.FileName, ImreadModes.Color);

                        // Resmi PictureBox'ta göstermek için Bitmap'e dönüştür
                        pbResult.Image = BitmapConverter.ToBitmap(_currentImage);

                        // Resim yüklendi, artık yüz bulma butonunu aktif edebiliriz
                        btnDetectFaces.Enabled = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Resim yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnDetectFaces.Enabled = false;
                    }
                }
            }
        }

        private void btnDetectFaces_Click(object sender, EventArgs e)
        {
            if (_currentImage == null || _currentImage.Empty())
            {
                MessageBox.Show("Lütfen önce bir resim yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Orijinal resim üzerinde değişiklik yapmamak için bir kopyasını alıyoruz
            // 'using' bloğu, işlem bittiğinde 'processingImage' matrisini bellekten atar
            using (Mat processingImage = _currentImage.Clone())
            {
                // Tespit işlemi gri tonlamalı resimlerde daha hızlı ve stabildir
                using (Mat grayImage = new Mat())
                {
                    // Resmi gri tona çevir
                    Cv2.CvtColor(processingImage, grayImage, ColorConversionCodes.BGR2GRAY);

                    // Kontrastı artırmak (daha iyi tespit) için histogram eşitleme
                    Cv2.EqualizeHist(grayImage, grayImage);

                    // Yüzleri tespit et
                    // DetectMultiScale: Resimdeki farklı boyutlardaki yüzleri arar
                    Rect[] faces = _faceCascade.DetectMultiScale(
                        grayImage,
                        1.1,  // Scale Factor: Her ölçekte resmin ne kadar küçültüleceği
                        5,    // Min Neighbors: Bir yüzü onaylamak için gereken min. komşu sayısı
                        HaarDetectionTypes.ScaleImage, // Algılama tipi
                        new OpenCvSharp.Size(30, 30) // Min Size: Tespit edilecek en küçük yüz boyutu (30x30 piksel)
                    );

                    if (faces.Length == 0)
                    {
                        MessageBox.Show("Bu resimde yüz bulunamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // Yüz bulamasa bile orijinal resmi tekrar göstersin
                        pbResult.Image = BitmapConverter.ToBitmap(_currentImage);
                        return;
                    }

                    // Bulunan her yüz için dikdörtgen çiz
                    foreach (Rect face in faces)
                    {
                        // Renkli resmimizin (processingImage) üzerine yeşil bir dikdörtgen çiz
                        // new Scalar(0, 255, 0) -> BGR formatında Yeşil renktir.
                        // 2 -> Çizgi kalınlığı
                        Cv2.Rectangle(processingImage, face, new Scalar(0, 255, 0), 2);
                    }

                    // Sonucu (çizilmiş resmi) PictureBox'ta göster
                    pbResult.Image = BitmapConverter.ToBitmap(processingImage);
                }
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _faceCascade?.Dispose();
            _currentImage?.Dispose();
        }
    }
}
