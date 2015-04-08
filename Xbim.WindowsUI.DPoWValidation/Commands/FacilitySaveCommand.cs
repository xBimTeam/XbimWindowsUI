using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using Xbim.CobieLiteUK.Validation.Reporting;
using Xbim.WindowsUI.DPoWValidation.Extensions;

namespace Xbim.WindowsUI.DPoWValidation.Commands
{
    class FacilitySaveCommand : ICommand
    {

        public event EventHandler CanExecuteChanged;
        private COBieLiteUK.Facility _facility;

        public bool CanExecute(object parameter)
        {
            return _facility != null;
        }

        public void Execute(object parameter)
        {
            var dlg = new SaveFileDialog();
            var filters = new List<string>();

            if (_facility.IsValidationResult())
            {
                filters.Add("Validation report|*.xlsx");
                filters.Add("Validation report|*.xls");
            }
            else
            {
                filters.Add("Cobie|*.xlsx");
                filters.Add("Cobie|*.xls");
            }
            filters.Add(@"Automation format|*.json");
            filters.Add(@"Automation format|*.xml");

            dlg.Filter = string.Join("|", filters.ToArray());
            // dlg.FileOk += dlg_FileOk;
            
            // _facility.ExportFacility(new FileInfo(@"C:\Data\dev\XbimTeam\XbimExchange\Tests\ValidationFiles\a3.xlsx"));
            if (dlg.ShowDialog() == true)
            {
                //var dlg = (sender as SaveFileDialog);
                //if (dlg == null)
                //    return;

                var fInfo = new FileInfo(dlg.FileName);
                // ExportFacility(new FileInfo(@"C:\Data\dev\XbimTeam\XbimExchange\Tests\ValidationFiles\a3.xlsx"));

                _facility.ExportFacility(fInfo);
            }
        }

        void dlg_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var dlg = (sender as SaveFileDialog);
            if (dlg == null)
                return;
            
            var fInfo = new FileInfo(dlg.FileName);
            // ExportFacility(new FileInfo(@"C:\Data\dev\XbimTeam\XbimExchange\Tests\ValidationFiles\a3.xlsx"));

            _facility.ExportFacility(fInfo);
            
        }

        

        public void ChangesHappened()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged.Invoke(this, new EventArgs());
            }
        }

        public FacilitySaveCommand(COBieLiteUK.Facility facility)
        {
            _facility = facility;
        }
    }
}
