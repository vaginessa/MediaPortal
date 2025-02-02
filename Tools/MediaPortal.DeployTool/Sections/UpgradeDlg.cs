#region Copyright (C) 2005-2023 Team MediaPortal

// Copyright (C) 2005-2023 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using MediaPortal.DeployTool.InstallationChecks;
using Microsoft.Win32;

namespace MediaPortal.DeployTool.Sections
{
  public partial class UpgradeDlg : DeployDialog
  {
    public static bool rbFreshChecked;
    private bool rbReinstallChecked;
    private bool rbUpdateChecked;
    public static bool MySQL56 = false;
    public static bool MySQL51= false;
    public static bool reInstallForce = false;
    public static bool freshForce = true; // Set to true by default (needed for fresh installation)

    public UpgradeDlg()
    {
      InitializeComponent();
      type = DialogType.Upgrade;
      labelSectionHeader.Text = "";
      bFresh.Image = Images.Choose_button_on;
      rbFreshChecked = true;
      rbReinstallChecked = false;
      rbUpdateChecked = false;
      // Check if MySQL need to be upgraded
      IInstallationPackage package = new MySQLChecker();
      CheckResult resultMySQL56 = package.CheckStatus();
      CheckResult resultMySQL51 = MySQLChecker.CheckStatusMySQL51();
      MySQL51 = resultMySQL51.state == CheckState.NOT_INSTALLED;
      MySQL56 = resultMySQL56.state == CheckState.NOT_INSTALLED;
      UpdateUI();
    }

    #region IDeployDialog interface

    public override void UpdateUI()
    {
      //
      // Disable upgrade if installed version is different from previous or SVN is installed
      // es1.  1.0.1 is current, installed must be 1.0.0
      // es2.  1.0 SVN XXXXX is installed
      //

      // MediaPortal
      int major = 0;
      int minor = 0;
      int revision = 0;
      RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MediaPortal");

      string MpBuild = "0";
      string MpDisplayVer = string.Empty;
      if (key != null)
      {
        MpBuild = key.GetValue("VersionBuild").ToString();
        major = (int)key.GetValue("VersionMajor", 0);
        minor = (int)key.GetValue("VersionMinor", 0);
        revision = (int)key.GetValue("VersionRevision", 0);
        MpDisplayVer = key.GetValue("DisplayVersion").ToString().Replace(" for TESTING ONLY", string.Empty);
        string InstallPath = key.GetValue("InstallPath").ToString();
        key.Close();

        if (!string.IsNullOrEmpty(InstallPath))
        {
          InstallationProperties.Instance.Set("MPDir", InstallPath);
        }
      }
      Version MpVer = new Version(major, minor, revision);

      // TV-Server
      major = 0;
      minor = 0;
      revision = 0;
      key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MediaPortal TV Server");
      string Tv3Build = "0";
      string Tv3DisplayVer = string.Empty;
      if (key != null)
      {
        Tv3Build = key.GetValue("VersionBuild").ToString();
        major = (int)key.GetValue("VersionMajor", 0);
        minor = (int)key.GetValue("VersionMinor", 0);
        revision = (int)key.GetValue("VersionRevision", 0);
        Tv3DisplayVer = key.GetValue("DisplayVersion").ToString().Replace(" for TESTING ONLY", string.Empty);
        string InstallPath = key.GetValue("InstallPath").ToString();
        key.Close();
        if (!string.IsNullOrEmpty(InstallPath))
        {
          InstallationProperties.Instance.Set("TVServerDir", InstallPath);
        }
      }
      Version Tv3Ver = new Version(major, minor, revision);

      rbUpdate.Enabled = false;
      bUpdate.Enabled = false;
      bReinstall.Enabled = false;
      rbReinstall.Enabled = false;

      if ((Utils.IsPackageUpdatabled(MpVer) || Utils.IsPackageUpdatabled(Tv3Ver)) &&
          (Utils.IsOfficialBuild(MpBuild) && Utils.IsOfficialBuild(Tv3Build)))
      {
        rbUpdate.Enabled = true;
        bUpdate.Enabled = true;

        // Set the default option to upgrade, if possible
        SelectUpdate();
      }

      if ((Utils.IsCurrentPackageUpdatabled(MpVer) || Utils.IsCurrentPackageUpdatabled(Tv3Ver)) &&
          (Utils.IsOfficialBuild(MpBuild) && Utils.IsOfficialBuild(Tv3Build)))
      {
        rbReinstall.Enabled = true;
        bReinstall.Enabled = true;

        // Set the default option to upgrade, if possible
        SelectReinstall();
      }

      var strMPDisplayVer = MpDisplayVer;
      if (MpVer.Major == 1 && MpVer.Minor == 1)
      {
        switch (MpVer.Build)
        {
          case 6:
            strMPDisplayVer = "1.2.0 Alpha";
            break;
          case 7:
            strMPDisplayVer = "1.2.0 Beta";
            break;
          case 8:
            strMPDisplayVer = "1.2.0 RC";
            break;
        }
      }

      var strTVDisplayVer = Tv3DisplayVer;
      if (Tv3Ver.Major == 1 && Tv3Ver.Minor == 1)
      {
        switch (Tv3Ver.Build)
        {
          case 6:
            strTVDisplayVer = "1.2.0 Alpha";
            break;
          case 7:
            strTVDisplayVer = "1.2.0 Beta";
            break;
          case 8:
            strTVDisplayVer = "1.2.0 RC";
            break;
        }
      }

      if (!String.IsNullOrEmpty(MpBuild))
      {
        labelSectionHeader.Text = !Utils.IsOfficialBuild(MpBuild)
                                    ? String.Format(Localizer.GetBestTranslation("Upgrade_labelSectionHeader_GIT"),
                                                    strMPDisplayVer, MpBuild)
                                    : String.Format(Localizer.GetBestTranslation("Upgrade_labelSectionHeader"),
                                                    strMPDisplayVer);
      }
      else
      {
        labelSectionHeader.Text = !Utils.IsOfficialBuild(Tv3Build)
                                    ? String.Format(Localizer.GetBestTranslation("Upgrade_labelSectionHeader_GIT"),
                                                    strTVDisplayVer, Tv3Build)
                                    : String.Format(Localizer.GetBestTranslation("Upgrade_labelSectionHeader"),
                                                    strTVDisplayVer);
      }

      rbUpdate.Text = String.Format(Localizer.GetBestTranslation("Upgrade_yes"), Utils.GetDisplayVersion());
      rbFresh.Text = Localizer.GetBestTranslation("Upgrade_no");
      rbReinstall.Text = String.Format(Localizer.GetBestTranslation("Reinstall"), Utils.GetDisplayVersion());
      labelNote.Text = Localizer.GetBestTranslation("Upgrade_note");
    }

    public override DeployDialog GetNextDialog()
    {
      if (MySQL56 && !MySQL51 && rbUpdate.Enabled && rbUpdateChecked &&
          (InstallationProperties.Instance["InstallType"] == "tvserver_master" ||
           InstallationProperties.Instance["InstallType"] == "singleseat"))
      {
        return DialogFlowHandler.Instance.GetDialogInstance(DialogType.MysqlUpgrade);
      }
      if (rbFreshChecked)
      {
        // Normal deploy...
        return DialogFlowHandler.Instance.GetDialogInstance(DialogType.WatchTV);
      }
      // Direct to upgrade
      if (InstallationProperties.Instance.Get("InstallType") != "tvserver_master")
      {
        // install includes MP so check skin choice
        return DialogFlowHandler.Instance.GetDialogInstance(DialogType.SkinChoice);
      }
      // tv server only install so no need for skin choice
      return DialogFlowHandler.Instance.GetDialogInstance(DialogType.Installation);
    }

    public override bool SettingsValid()
    {
      return true;
    }

    #endregion

    private void bUpdate_Click(object sender, EventArgs e)
    {
      SelectUpdate();
    }

    private void bFresh_Click(object sender, EventArgs e)
    {
      SelectFresh();
    }

    private void SelectUpdate()
    {
      bUpdate.Image = Images.Choose_button_on;
      bFresh.Image = Images.Choose_button_off;
      bReinstall.Image = Images.Choose_button_off;
      rbFreshChecked = false;
      rbUpdateChecked = true;
      reInstallForce = false;
      freshForce = false;
      InstallationProperties.Instance.Set("UpdateMode", "yes");

      CheckResult resultTvServer = Utils.CheckNSISUninstallString("MediaPortal TV Server", "MementoSection_SecServer");
      CheckResult resultTvClient = Utils.CheckNSISUninstallString("Mediaportal Tv Server", "MementoSection_SecClient");

      bool TvServer = resultTvServer.state != CheckState.NOT_INSTALLED;
      bool TvClient = resultTvClient.state != CheckState.NOT_INSTALLED;

      if (!TvServer && !TvClient) InstallationProperties.Instance.Set("InstallType", "mp_only");
      if (!TvServer && TvClient) InstallationProperties.Instance.Set("InstallType", "client");
      if (TvServer && !TvClient) InstallationProperties.Instance.Set("InstallType", "tvserver_master");
      if (TvServer && TvClient) InstallationProperties.Instance.Set("InstallType", "singleseat");
    }

    private void SelectReinstall()
    {
      bUpdate.Image = Images.Choose_button_off;
      bFresh.Image = Images.Choose_button_off;
      bReinstall.Image = Images.Choose_button_on;
      rbFreshChecked = false;
      rbUpdateChecked = false;
      rbReinstallChecked = true;
      reInstallForce = true;
      freshForce = false;
      InstallationProperties.Instance.Set("UpdateMode", "yes");

      CheckResult resultTvServer = Utils.CheckNSISUninstallString("MediaPortal TV Server", "MementoSection_SecServer");
      CheckResult resultTvClient = Utils.CheckNSISUninstallString("Mediaportal Tv Server", "MementoSection_SecClient");

      bool TvServer = resultTvServer.state != CheckState.NOT_INSTALLED;
      bool TvClient = resultTvClient.state != CheckState.NOT_INSTALLED;

      if (!TvServer && !TvClient) InstallationProperties.Instance.Set("InstallType", "mp_only");
      if (!TvServer && TvClient) InstallationProperties.Instance.Set("InstallType", "client");
      if (TvServer && !TvClient) InstallationProperties.Instance.Set("InstallType", "tvserver_master");
      if (TvServer && TvClient) InstallationProperties.Instance.Set("InstallType", "singleseat");
    }

    private void SelectFresh()
    {
      bUpdate.Image = Images.Choose_button_off;
      bFresh.Image = Images.Choose_button_on;
      bReinstall.Image = Images.Choose_button_off;
      rbFreshChecked = true;
      rbUpdateChecked = false;
      rbReinstallChecked = false;
      reInstallForce = false;
      freshForce = true;
      InstallationProperties.Instance.Set("UpdateMode", "no");
    }

    private void bReinstall_Click(object sender, EventArgs e)
    {
      SelectReinstall();
    }
  }
}