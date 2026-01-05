using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using AnotherMarkdown.Entities;

namespace AnotherMarkdown.Forms
{
  public partial class SettingsForm : Form
  {
    public int ZoomLevel { get; set; }
    public string AssetsPath { get; set; }
    public string CssFileName { get; set; }
    public string CssDarkModeFileName { get; set; }
    public bool ShowToolbar { get; set; }
    public bool ShowStatusbar { get; set; }

    public SettingsForm(Settings settings)
    {
      AssetsPath = settings.AssetsPath;
      ZoomLevel = settings.ZoomLevel;
      CssFileName = settings.CssFileName;
      CssDarkModeFileName = settings.CssDarkModeFileName;
      ShowToolbar = settings.ShowToolbar;
      ShowStatusbar = settings.ShowStatusbar;

      if (string.IsNullOrEmpty(CssFileName)) {
        CssFileName = Settings.DefaultCssFile;
      }
      if (string.IsNullOrEmpty(CssDarkModeFileName)) {
        CssDarkModeFileName = Settings.DefaultDarkModeCssFile;
      }

      InitializeComponent();

      tbAssetsPath.Text = AssetsPath;
      trackBar1.Value = ZoomLevel;
      lblZoomValue.Text = $"{ZoomLevel}%";
      tbCssFile.Text = CssFileName;
      tbDarkmodeCssFile.Text = CssDarkModeFileName;
      cbShowToolbar.Checked = ShowToolbar;
      cbShowStatusbar.Checked = ShowStatusbar;
    }

    private void trackBar1_ValueChanged(object sender, EventArgs e)
    {
      ZoomLevel = trackBar1.Value;
      lblZoomValue.Text = $"{ZoomLevel}%";
    }

    private void tbCssFile_TextChanged(object sender, EventArgs e)
    {
      CssFileName = tbCssFile.Text;
    }

    private void tbDarkmodeCssFile_TextChanged(object sender, EventArgs e)
    {
      CssDarkModeFileName = tbDarkmodeCssFile.Text;
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
      if (string.IsNullOrEmpty(sblInvalidHtmlPath.Text)) {
        DialogResult = DialogResult.OK;
      }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
      DialogResult = DialogResult.Cancel;
    }

    private void btnChooseCss_Click(object sender, EventArgs e)
    {
      using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
        openFileDialog.Filter = "css files (*.css)|*.css|All files (*.*)|*.*";
        openFileDialog.RestoreDirectory = true;
        if (openFileDialog.ShowDialog() == DialogResult.OK) {
          if ((sender as Button).Name == "btnChooseCss") {
            CssFileName = openFileDialog.FileName;
            tbCssFile.Text = CssFileName;
          }
          else if ((sender as Button).Name == "btnChooseDarkmodeCss") {
            CssDarkModeFileName = openFileDialog.FileName;
            tbDarkmodeCssFile.Text = CssDarkModeFileName;
          }
        }
      }
    }

    private void button1_Click(object sender, EventArgs e)
    {
      tbCssFile.Text = "";
    }

    private void btnDefaultDarkmodeCss_Click(object sender, EventArgs e)
    {
      tbDarkmodeCssFile.Text = "";
    }

    #region Show Toolbar
    private void cbShowToolbar_Changed(object sender, EventArgs e)
    {
      ShowToolbar = cbShowToolbar.Checked;
    }
        #endregion

    private void cbShowStatusbar_CheckedChanged(object sender, EventArgs e)
    {
      ShowStatusbar = cbShowStatusbar.Checked;
    }

    private void btnDefaultAssetDir_Click(object sender, EventArgs e)
    {
      tbAssetsPath.Text = "";
      AssetsPath = "";
    }

    private void btnChooseAssetsDir_Click(object sender, EventArgs e)
    {
      using (var folderOpenDialog = new FolderBrowserDialog()) {
        folderOpenDialog.SelectedPath = !string.IsNullOrEmpty(AssetsPath) ? AssetsPath : Path.GetFullPath(Settings.DefaultAssetPath);

        if (folderOpenDialog.ShowDialog() == DialogResult.OK) {
          if (folderOpenDialog.SelectedPath.Replace("\\", "/") == Settings.DefaultAssetPath) {
            AssetsPath = string.Empty;
          }
          else {
            AssetsPath = folderOpenDialog.SelectedPath;
          }
          tbAssetsPath.Text = AssetsPath;
        }
      }
    }

    private void tbAssetsPath_Leave(object sender, EventArgs e)
    {
      if (tbAssetsPath.Text != "") {
        if (Directory.Exists(tbAssetsPath.Text)) {
          AssetsPath = tbAssetsPath.Text;
        }
      }
      else {
        AssetsPath = "";
      }
    }
  }
}
