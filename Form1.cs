using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace BgWorkerTesting
{
    public partial class Form1 : Form
    {
        // The FTP2 component 
        private bool m_bgRunning = false;
        private bool m_abort = false;
        private bool m_isUpload = false;
        private string m_ftpLastError = "";
        private string m_ftpSessionLog = "";
        private Chilkat.Ftp2 m_ftp = new Chilkat.Ftp2();

        private BackgroundWorker m_bgWorker = new BackgroundWorker();

        public Form1()
        {
            InitializeComponent();
        }

        void m_bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Display the error information and session log.
            // If the operation succeeded, the last error text will provide information about
            // the operation -- the presence of content does not indicate an error.
            txtLastError.Text = m_ftpLastError;
            txtSessionLog.Text = m_ftpSessionLog;

            bool success = (bool)e.Result;
            if (success)
            {
                MessageBox.Show("Success!");
            }
            else
            {
                MessageBox.Show("Failure!");
            }

            // Enable the buttons...
            btnUpload.Enabled = true;
            btnDownload.Enabled = true;
            btnDisconnect.Enabled = true;
        }

        void m_bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 0)
            {
                // This is an AbortCheck callback...
                int value = progressBar2.Value;
                if (value > 90)
                {
                    progressBar2.Value = 0;
                }
                else
                {
                    progressBar2.Value += 10;
                }
            }
            else
            {
                // This is called from an OnFtpPercentDone event callback.
                progressBar1.Value = e.ProgressPercentage;
            }

        }

        void m_bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Indicate that we are in the background thread
            // The FTP object's event callback will need this.
            m_bgRunning = true;

            // We passed the filename as an argument.
            // However, we could've just as well accessed the filename
            // via the txtFilename textbox control.
            string filename = e.Argument as string;

            // Is this an upload or download?
            if (m_isUpload)
            {
                // This causes both AbortCheck and FtpPercentDone events to
                // fire.  The callback methods are m_ftp_OnAbortCheck and
                // m_ftp_OnFtpPercentDone.  These methods in turn call the
                // m_bgWorker.ReportProgress method, causing the
                // m_bgWorker_ProgressChanged to be called.  The progress
                // bars can only be updated from a background thread
                // in the m_bgWorker_ProgressChanged event.  The same applies
                // to any other Form controls.  
                bool success = m_ftp.PutFile(filename, filename);
                e.Result = success;
            }
            else
            {
                bool success = m_ftp.GetFile(filename, filename);
                e.Result = success;
            }

            // Display the last-error and session log regardless of success/failure...
            m_ftpLastError = m_ftp.LastErrorText;
            m_ftpSessionLog = m_ftp.SessionLog;

            m_bgRunning = false;

            return;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            m_bgWorker.WorkerReportsProgress = true;
            m_bgWorker.WorkerSupportsCancellation = true;

            m_bgWorker.DoWork += new DoWorkEventHandler(m_bgWorker_DoWork);
            m_bgWorker.ProgressChanged += new ProgressChangedEventHandler(m_bgWorker_ProgressChanged);
            m_bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_bgWorker_RunWorkerCompleted);

            // Unlocking is only required once.
            bool success = m_ftp.UnlockComponent("Anything for 30-day trial");
            if (!success)
            {
                txtLastError.Text = m_ftp.LastErrorText;
            }

            // Make sure events are enabled.  Get heartbeat events every 100 millisec.
            m_ftp.EnableEvents = true;
            m_ftp.HeartbeatMs = 100;
            m_ftp.KeepSessionLog = true;

            btnUpload.Enabled = false;
            btnDownload.Enabled = false;
            btnDisconnect.Enabled = false;

            // Add event callbacks for m_ftp
            m_ftp.OnAbortCheck += new Chilkat.Ftp2.AbortCheckEventHandler(m_ftp_OnAbortCheck);
            m_ftp.OnPercentDone += new Chilkat.Ftp2.PercentDoneEventHandler(m_ftp_OnFtpPercentDone);
        }

        void m_ftp_OnFtpPercentDone(object sender, Chilkat.PercentDoneEventArgs args)
        {
            // We need to update progress bars from the BackgroundWorker's
            // event callback.
            m_bgWorker.ReportProgress(args.PercentDone);
            args.Abort = m_abort;
        }

        void m_ftp_OnAbortCheck(object sender, Chilkat.AbortCheckEventArgs args)
        {
            if (m_bgRunning)
            {
                // Passing a 0 will indicat that this is an AbortCheck call...
                // We need to update progress bars from the BackgroundWorker's
                // event callback.
                m_bgWorker.ReportProgress(0);
                args.Abort = m_abort;
            }
            else
            {
                int value = progressBar2.Value;
                if (value > 90)
                {
                    progressBar2.Value = 0;
                }
                else
                {
                    progressBar2.Value += 10;
                }

                // If this is in the foreground, we want to keep the UI responsive.
                Application.DoEvents();

                // m_abort gets set to true if the Abort button is pressed.
                args.Abort = m_abort;
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            // Reset progress bars, the abort flag, and give the abort button focus.
            progressBar2.Value = 0;
            progressBar1.Value = 0;
            m_abort = false;
            btnAbort.Focus();

            // Disable the upload/download buttons until the async
            // operation is complete.
            btnUpload.Enabled = false;
            btnDownload.Enabled = false;
            btnDisconnect.Enabled = false;

            // Do the asynchronous upload.
            // The worker thread gets the filename from the passed-in argument,
            // it can also get other information from the class members.
            m_isUpload = true;
            // This causes the m_bgWorker_DoWork method to be run in a
            // background thread.
            m_bgWorker.RunWorkerAsync(txtFilename.Text);

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            // Reset progress bars, the abort flag, and give the abort button focus.
            progressBar2.Value = 0;
            progressBar1.Value = 0;
            m_abort = false;
            btnAbort.Focus();

            m_ftp.Hostname = txtHostname.Text;
            m_ftp.Username = txtLogin.Text;
            m_ftp.Password = txtPassword.Text;

            // We'll run the connect in the foreground thread, but with
            // heartbeat events providing abort capability and UI responsiveness.
            bool success = m_ftp.Connect();
            if (!success)
            {
                txtLastError.Text = m_ftp.LastErrorText;
                txtSessionLog.Text = m_ftp.SessionLog;
                MessageBox.Show("Failed to connect / login");
                return;
            }

            // Change to the temp directory.
            success = m_ftp.ChangeRemoteDir(txtRemoteDir.Text);
            if (!success)
            {
                txtLastError.Text = m_ftp.LastErrorText;
                txtSessionLog.Text = m_ftp.SessionLog;
                MessageBox.Show("Failed to change remote dir");
                return;
            }

            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            btnUpload.Enabled = true;
            btnDownload.Enabled = true;

            // Show this even on success...
            txtLastError.Text = m_ftp.LastErrorText;
            txtSessionLog.Text = m_ftp.SessionLog;

            MessageBox.Show("Connected!");
        }

        private void btnAbort_Click(object sender, EventArgs e)
        {
            // Set the abort flag.  The next AbortCheck event callback will 
            // examine m_abort's value and if true, will abort the operation.
            // The HeartbeatMs property is set to 100, causing AbortCheck events
            // to fire every .1 seconds while an FTP2 operation is in progress.
            m_abort = true;
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            // Reset progress bars, the abort flag, and give the abort button focus.
            progressBar2.Value = 0;
            progressBar1.Value = 0;
            m_abort = false;
            btnAbort.Focus();

            bool success = m_ftp.Disconnect();
            if (!success)
            {
                txtLastError.Text = m_ftp.LastErrorText;
                txtSessionLog.Text = m_ftp.SessionLog;
                MessageBox.Show("Failed to disconnect");
                return;
            }

            // Show this even on success...
            txtLastError.Text = m_ftp.LastErrorText;
            txtSessionLog.Text = m_ftp.SessionLog;

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            btnUpload.Enabled = false;
            btnDownload.Enabled = false;

            MessageBox.Show("Disconnected!");
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            // Reset progress bars, the abort flag, and give the abort button focus.
            progressBar2.Value = 0;
            progressBar1.Value = 0;
            m_abort = false;
            btnAbort.Focus();

            // Disable the upload/download buttons until the async
            // operation is complete.
            btnUpload.Enabled = false;
            btnDownload.Enabled = false;
            btnDisconnect.Enabled = false;

            // Do the asynchronous download.
            // The worker thread gets the filename from the passed-in argument,
            // it can also get other information from the class members.
            m_isUpload = false;
            // This causes the m_bgWorker_DoWork method to be run in a
            // background thread.
            m_bgWorker.RunWorkerAsync(txtFilename.Text);

        }
    }
}