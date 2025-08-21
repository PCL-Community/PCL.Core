using PCL.Core.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCL.Core.Minecraft.InstanceExport
{
    public class InstanceExportHelper
    {
        private InstanceExportRules _rules;

        public InstanceExportHelper(string instancePath)
        {
            _LoadRules();
        }

        private void _LoadRules()
        {
            _rules = FileService.WaitForResult(PredefinedFileItems.InstanceExportRules)?.Try<InstanceExportRules>();
        }

        public Task ExportAsync()
        {
            // 没写完
            return Task.CompletedTask;
        }
    }
}
