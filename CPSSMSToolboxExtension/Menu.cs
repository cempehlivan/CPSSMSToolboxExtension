using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CPSSMSToolboxExtension
{
    internal class TableMenuItem : ToolsMenuItemBase, IWinformsMenuHandler
    {
        protected override void Invoke()
        {
        }

        public ToolStripItem[] GetMenuItems()
        {
            Bitmap csharpIcon = Resource.csharp.ToBitmap();
            Bitmap sqlIcon = Resource.sql.ToBitmap();
            Bitmap logoIcon = Resource.logo.ToBitmap();

            var item = new ToolStripMenuItem("Toolbox", logoIcon);

            ToolStripMenuItem btn;

            btn = new ToolStripMenuItem("C# Class", csharpIcon);
            btn.Click += CPSSMSToolboxExtensionCommand.Instance.CreateTableCSharpClass;
            btn.Enabled = false;
            btn.OwnerChanged += Btn_OwnerChanged;
            item.DropDownItems.Add(btn);

            btn = new ToolStripMenuItem("SQL Insert/Update Procedure", sqlIcon);
            btn.Click += CPSSMSToolboxExtensionCommand.Instance.TableSQLIUProcedure;
            btn.Enabled = false;
            btn.OwnerChanged += Btn_OwnerChanged;
            item.DropDownItems.Add(btn);

            btn = new ToolStripMenuItem("SQL SET Procedure", sqlIcon);
            btn.Click += CPSSMSToolboxExtensionCommand.Instance.TableSP_SET;
            btn.Enabled = false;
            btn.OwnerChanged += Btn_OwnerChanged;
            item.DropDownItems.Add(btn);

            btn = new ToolStripMenuItem("SQL GET Procedure", sqlIcon);
            btn.Click += CPSSMSToolboxExtensionCommand.Instance.TableSP_GET;
            btn.Enabled = false;
            btn.OwnerChanged += Btn_OwnerChanged;
            item.DropDownItems.Add(btn);

            btn = new ToolStripMenuItem("SQL DEL Procedure", sqlIcon);
            btn.Click += CPSSMSToolboxExtensionCommand.Instance.TableSP_DEL;
            btn.Enabled = false;
            btn.OwnerChanged += Btn_OwnerChanged;
            item.DropDownItems.Add(btn);

            btn = new ToolStripMenuItem("SQL Log Table/Trigger", sqlIcon);
            btn.Click += CPSSMSToolboxExtensionCommand.Instance.TableSQLLogTableTrigger;
            btn.Enabled = false;
            btn.OwnerChanged += Btn_OwnerChanged;
            item.DropDownItems.Add(btn);


            return new ToolStripItem[] { item };
        }

        private void Btn_OwnerChanged(object sender, EventArgs e)
        {
            if (sender is ToolStripItem)
            {
                var s = (ToolStripItem)sender;

                var table = CPSSMSToolboxExtensionCommand.Instance.GetTable();
                s.Enabled = false;


                if (table != null)
                    s.Enabled = true;
            }
        }

        public override object Clone() => new TableMenuItem();
    }



    internal class ToolStripSeparatorMenuItem : ToolsMenuItemBase, IWinformsMenuHandler
    {
        protected override void Invoke()
        {
        }

        public override object Clone() => new ToolStripSeparatorMenuItem();

        public ToolStripItem[] GetMenuItems()
        {
            return new ToolStripItem[] { new ToolStripSeparator() };
        }
    }
}