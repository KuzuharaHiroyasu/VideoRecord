using System;
using System.ComponentModel;
using System.Windows.Forms;
using OpenCvSharp;

namespace VideoRecord
{
    public partial class Form1 : Form
    {
        private double fps = 30;
        private int width = 430;
        private int height = 350;
        BackgroundWorker worker = null;
        CvVideoWriter video = null;

        public Form1()
        {
            InitializeComponent();

            worker = new BackgroundWorker();

            // 非同期をキャンセルさせる
            worker.WorkerSupportsCancellation = true;

            // ProgressChangedイベントを発生させるようにする
            worker.WorkerReportsProgress = true;

            // ReportProgressメソッドで呼ばれるProgressChangedのイベントハンドラを追加
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);

            // RunWorkerAsyncメソッドで呼ばれるDoWorkに、
            // 別スレッドでUSBカメラの画像を取得し続けるイベントハンドラを追加
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);

            button_stop.Enabled = false;
        }

        /************************************************************************/
        /* 関数名   : button_start_Click  		                                */
        /* 機能     : 録画開始ボタン押下事のイベント                            */
        /* 引数     : なし                                                      */
        /* 戻り値   : なし														*/
        /************************************************************************/
        private void button_start_Click(object sender, EventArgs e)
        {
            // .aviファイルを開く
            video = new CvVideoWriter("video.avi", FourCC.MJPG, fps, new CvSize(width, height));

            // DoWorkイベントハンドラの実行を開始
            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
            }

            button_start.Enabled = false;
            button_stop.Enabled = true;
        }

        /************************************************************************/
        /* 関数名   : button_stop_Click  		                                */
        /* 機能     : 停止ボタン押下事のイベント                                */
        /* 引数     : なし                                                      */
        /* 戻り値   : なし														*/
        /************************************************************************/
        private void button_stop_Click(object sender, EventArgs e)
        {
            // 動画ファイルを閉じる
            if (video != null)
            {
                video.Dispose();
            }

            button_start.Enabled = true;
            button_stop.Enabled = false;
        }

        /************************************************************************/
        /* 関数名   : worker_DoWork          		                            */
        /* 機能     : カメラからの映像を受け取る                                */
        /* 引数     : なし                                                      */
        /* 戻り値   : なし														*/
        /************************************************************************/
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // カメラからの映像を受け取る
            try
            {
                using (var capture = Cv.CreateCameraCapture(CaptureDevice.Any))
                {
                    IplImage frame;
                    while (true)
                    {
                        frame = Cv.QueryFrame(capture);

                        // 新しい画像を取得したので、
                        // ReportProgressメソッドを使って、ProgressChangedイベントを発生させる
                        worker.ReportProgress(0, frame);
                    }
                }
            }
            catch (Exception ex)
            {
                if (video != null)
                {
                    video.Dispose();
                }
                button_start.Invoke(new D_Button_Cnt(button_Cnt));
            }
        }

        /************************************************************************/
        /* 関数名   : worker_ProgressChanged  		                            */
        /* 機能     : 動画ファイルに書き込み                                    */
        /* 引数     : なし                                                      */
        /* 戻り値   : なし														*/
        /************************************************************************/
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //  frameがe.UserStateプロパティにセットされて渡されてくる
            IplImage image = (IplImage)e.UserState;

            CvSize size = new CvSize(width, height);
            IplImage reImage = new IplImage(size, image.Depth, image.NChannels);

            Cv.Resize(image, reImage, Interpolation.NearestNeighbor);

            // 動画ファイルに書き込み
            if (!video.IsDisposed)
            {
                video.WriteFrame(reImage);
            }

            pictureBoxIpl_video.ImageIpl = reImage;
        }

        private delegate void D_Button_Cnt();
        private void button_Cnt()
        {
            // メッセージを最前面に表示
            using (Form f = new Form())
            {
                f.TopMost = true;
                MessageBox.Show(f, "カメラを検出できませんでした。");
                f.TopMost = false;
            }

            button_start.Enabled = true;
            button_stop.Enabled = false;
        }
    }
}
