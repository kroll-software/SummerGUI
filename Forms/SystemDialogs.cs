using System;

namespace SummerGUI
{
    public enum DialogResult
    {
        None,
        OK,
        Cancel,			
    }
        
    public class OpenFileDialog {
        public string FileName { get; private set; }
        
        public DialogResult ShowDialog(IGUIContext ctx, 
                string caption = "Open File",
                string filter = "", 
                int filterIndex = 1, 
                string initialDirectory = "",
                bool restoreDirectory = false) 
        {
            switch (PlatformExtensions.CurrentOS) {
                case PlatformExtensions.OS.Windows:
                    var windowsDlg = new SystemSpecific.Windows.SystemDialogs();
                    FileName = windowsDlg.OpenFileDialog(ctx, caption, filter, filterIndex, initialDirectory, restoreDirectory);
                    return string.IsNullOrEmpty(FileName) ? DialogResult.Cancel : DialogResult.OK;						
                case PlatformExtensions.OS.Linux:
                    var linuxDlg = new SystemSpecific.Linux.SystemDialogs();
                    FileName = linuxDlg.OpenFileDialog(ctx, caption, filter, filterIndex, initialDirectory, restoreDirectory);
                    return string.IsNullOrEmpty(FileName) ? DialogResult.Cancel : DialogResult.OK;
                case PlatformExtensions.OS.Mac:
                    var macDlg = new SystemSpecific.Mac.SystemDialogs();
                    FileName = macDlg.OpenFileDialog(ctx, caption, filter, filterIndex, initialDirectory, restoreDirectory);
                    return string.IsNullOrEmpty(FileName) ? DialogResult.Cancel : DialogResult.OK;
            }
            return DialogResult.None;
        }
    }

    public class SaveFileDialog {
        public string FileName { get; private set; }
        
        public DialogResult ShowDialog(IGUIContext ctx, 
            string caption = "Save File",
            string defaultFileName = "", // Neu
            string filter = "", 
            int filterIndex = 1, 
            string initialDirectory = "",
            bool restoreDirectory = false)
        {
            switch (PlatformExtensions.CurrentOS) {
                case PlatformExtensions.OS.Windows:
                    var windowsDlg = new SystemSpecific.Windows.SystemDialogs();
                    FileName = windowsDlg.SaveFileDialog(ctx, caption, defaultFileName, filter, filterIndex, initialDirectory, restoreDirectory);
                    return string.IsNullOrEmpty(FileName) ? DialogResult.Cancel : DialogResult.OK;						
                case PlatformExtensions.OS.Linux:
                    var linuxDlg = new SystemSpecific.Linux.SystemDialogs();
                    FileName = linuxDlg.SaveFileDialog(ctx, caption, filter, defaultFileName, filterIndex, initialDirectory, restoreDirectory);
                    return string.IsNullOrEmpty(FileName) ? DialogResult.Cancel : DialogResult.OK;
                case PlatformExtensions.OS.Mac:
                    var macDlg = new SystemSpecific.Mac.SystemDialogs();
                    FileName = macDlg.SaveFileDialog(ctx, caption, filter, defaultFileName, filterIndex, initialDirectory, restoreDirectory);
                    return string.IsNullOrEmpty(FileName) ? DialogResult.Cancel : DialogResult.OK;
            }
            return DialogResult.None;
        }
    }

    public class SelectFolderDialog {
        public string FilePath { get; private set; }
        
        public DialogResult ShowDialog(IGUIContext ctx,
            string caption = "Select Folder",            
            string initialDirectory = "",
            bool restoreDirectory = false)
        {
            switch (PlatformExtensions.CurrentOS) {
                case PlatformExtensions.OS.Windows:
                    var windowsDlg = new SystemSpecific.Windows.SystemDialogs();
                    FilePath = windowsDlg.SelectFolderDialog(ctx, caption, initialDirectory, restoreDirectory);
                    return string.IsNullOrEmpty(FilePath) ? DialogResult.Cancel : DialogResult.OK;						
                case PlatformExtensions.OS.Linux:
                    var linuxDlg = new SystemSpecific.Linux.SystemDialogs();
                    FilePath = linuxDlg.SelectFolderDialog(ctx, caption, initialDirectory, restoreDirectory);
                    return string.IsNullOrEmpty(FilePath) ? DialogResult.Cancel : DialogResult.OK;
                case PlatformExtensions.OS.Mac:
                    var macDlg = new SystemSpecific.Mac.SystemDialogs();
                    FilePath = macDlg.SelectFolderDialog(ctx, caption, initialDirectory, restoreDirectory);
                    return string.IsNullOrEmpty(FilePath) ? DialogResult.Cancel : DialogResult.OK;
            }
            return DialogResult.None;
        }
    }
}