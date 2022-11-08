using EnvDTE;
using EnvDTE80;
using Microsoft.SqlServer.Management.SqlStudio.Explorer;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Reflection;

namespace CPSSMSToolboxExtension
{
    internal sealed class CPSSMSToolboxExtensionCommand
    {
        private string _tableUrnPath = "Server/Database/Table";

        private string COMMAND_ExecuteSQL = "Query.Execute";
        private string COMMAND_NewQueryWindow = "File.NewQuery";

        public const int TableCSharpClassId = 0x1151;
        public const int TableSQLIUProcedureId = 0x1251;
        public const int TableSQLSelectProcedureId = 0x1252;
        public const int TableSQLLogTableTriggerId = 0x1253;
        public const int CreateWhoIsActiveSPId = 0x1254;

        public static readonly Guid CommandSet = new Guid("1651C494-8C78-4BF3-895B-6C1F84F7E1C8");

        private readonly Package package;
        private HierarchyObject _tableMenu;

        public static CPSSMSToolboxExtensionCommand Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        private IObjectExplorerService GetObjectExplorer()
        {
            return ServiceProvider.GetService(typeof(IObjectExplorerService)) as IObjectExplorerService;
        }

        private IObjectExplorerService ObjectExplorerService
        {
            get
            {
                return GetObjectExplorer();
            }
        }


        private CPSSMSToolboxExtensionCommand(Package package)
        {
            if (package == null)
                throw new ArgumentNullException("package");

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var cmd1 = new OleMenuCommand(this.CreateTableCSharpClass, new CommandID(CommandSet, TableCSharpClassId));
                cmd1.Enabled = cmd1.Supported = false;
                cmd1.BeforeQueryStatus += Cmd1_BeforeQueryStatus;
                commandService.AddCommand(cmd1);

                //var cmd2 = new OleMenuCommand(this.CreateTableCSharpDBSaveMethod, new CommandID(CommandSet, TableCSharpDBSaveMethodId));
                //cmd2.Enabled = cmd2.Supported = false;
                //cmd2.BeforeQueryStatus += Cmd1_BeforeQueryStatus;
                //commandService.AddCommand(cmd2);

                var cmd3 = new OleMenuCommand(this.TableSQLIUProcedure, new CommandID(CommandSet, TableSQLIUProcedureId));
                cmd3.Enabled = cmd3.Supported = false;
                cmd3.BeforeQueryStatus += Cmd1_BeforeQueryStatus;
                commandService.AddCommand(cmd3);

                var cmd4 = new OleMenuCommand(this.TableSQLSelectProcedure, new CommandID(CommandSet, TableSQLSelectProcedureId));
                cmd4.Enabled = cmd4.Supported = false;
                cmd4.BeforeQueryStatus += Cmd1_BeforeQueryStatus;
                commandService.AddCommand(cmd4);

                var cmd5 = new OleMenuCommand(this.TableSQLLogTableTrigger, new CommandID(CommandSet, TableSQLLogTableTriggerId));
                cmd5.Enabled = cmd5.Supported = false;
                cmd5.BeforeQueryStatus += Cmd1_BeforeQueryStatus;
                commandService.AddCommand(cmd5);

                var cmd6 = new OleMenuCommand(this.CreateWhoIsActiveSP, new CommandID(CommandSet, CreateWhoIsActiveSPId));
                cmd6.Enabled = cmd6.Supported = false;
                cmd6.BeforeQueryStatus += Cmd1_BeforeQueryStatus;
                commandService.AddCommand(cmd6);
            }


            SetObjectExplorerEventProvider();
        }

        public static void Initialize(Package package)
        {
            Instance = new CPSSMSToolboxExtensionCommand(package);
        }


        private ContextService GetContextService()
        {
            ContextService contextService = null;

            var objectExplorer = GetObjectExplorer();
            if (objectExplorer == null) return null;
            var t = Assembly.Load("Microsoft.SqlServer.Management.SqlStudio.Explorer").GetType("Microsoft.SqlServer.Management.SqlStudio.Explorer.ObjectExplorerService");

            var piContainer = t.GetProperty("Container", BindingFlags.Public | BindingFlags.Instance);
            var objectExplorerContainer = piContainer.GetValue(objectExplorer, null);
            var piContextService = objectExplorerContainer.GetType().GetProperty("Components", BindingFlags.Public | BindingFlags.Instance);
            var objectExplorerComponents = piContextService.GetValue(objectExplorerContainer, null) as ComponentCollection;

            if (objectExplorerComponents != null)
                foreach (Component component in objectExplorerComponents)
                {
                    if (component.GetType().FullName.Contains("ContextService"))
                    {
                        contextService = (ContextService)component;
                        break;
                    }
                }

            return contextService;
        }

        private void SetObjectExplorerEventProvider()
        {
            var mi = GetType().GetMethod("Provider_SelectionChanged", BindingFlags.NonPublic | BindingFlags.Instance);

            ContextService contextService = GetContextService();

            if (contextService == null)
                throw new NullReferenceException("Can't find ObjectExplorer ContextService.");

            var piObjectExplorerContext = contextService.GetType().GetProperty("ObjectExplorerContext", BindingFlags.Public | BindingFlags.Instance);
            var objectExplorerContext = piObjectExplorerContext.GetValue(contextService, null);
            var ei = objectExplorerContext.GetType().GetEvent("CurrentContextChanged", BindingFlags.Public | BindingFlags.Instance);
            if (ei == null) return;
            var del = Delegate.CreateDelegate(ei.EventHandlerType, this, mi);
            ei.AddEventHandler(objectExplorerContext, del);
        }

        TableMenuItem tableMenuItem = new TableMenuItem();
        private void Provider_SelectionChanged(object sender, NodesChangedEventArgs args)
        {
            if (_tableMenu != null) return;

            if (args.ChangedNodes.Count <= 0) return;
            var node = args.ChangedNodes[0];
            if (node == null) return;

            if (node != null && node.UrnPath == _tableUrnPath)
            {
                _tableMenu = (HierarchyObject)node.GetService(typeof(IMenuHandler));
                _tableMenu.AddChild(string.Empty, tableMenuItem);
            }
        }


        private void Cmd1_BeforeQueryStatus(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand)
            {
                OleMenuCommand menuCommand = (OleMenuCommand)sender;

                menuCommand.Enabled = false;
                menuCommand.Supported = false;

                var table = GetTable();

                if (table != null)
                {
                    menuCommand.Enabled = true;
                    menuCommand.Supported = true;
                    //menuCommand.Text = node.InvariantName;
                }
            }
        }


        public INodeInformation GetTable()
        {
            int count;
            INodeInformation[] nodes = null;
            INodeInformation node = null;

            try
            {
                ObjectExplorerService.GetSelectedNodes(out count, out nodes);
            }
            catch
            {

            }

            if (nodes != null && nodes.Length > 0 && nodes[0].UrnPath == _tableUrnPath)
                node = nodes[0];


            return node;
        }

        public void CreateWindow(string SQLQuery, bool afterExecute = false, string[] afterExecuteCommand = null)
        {
            DTE2 dte = (DTE2)ServiceProvider.GetService(typeof(DTE));
            dte.ExecuteCommand(COMMAND_NewQueryWindow);

            var doc = (TextDocument)dte.Application.ActiveDocument.Object(null);
            doc.EndPoint.CreateEditPoint().Insert(SQLQuery);

            if (afterExecute)
                dte.ExecuteCommand(COMMAND_ExecuteSQL);

            if (afterExecuteCommand != null && afterExecuteCommand.Length > 0)
                foreach (string cmd in afterExecuteCommand)
                    dte.ExecuteCommand(cmd);
        }


        public void CreateTableCSharpClass(object sender, EventArgs e)
        {
            INodeInformation table = GetTable();

            if (table != null)
            {
                string classs = "/*" + Environment.NewLine + Environment.NewLine + Creator.CSharpClass(table) + Environment.NewLine + Environment.NewLine + "*/";

                CreateWindow(classs);
            }
        }

        public void TableSQLIUProcedure(object sender, EventArgs e)
        {
            INodeInformation table = GetTable();

            if (table != null)
            {
                string tsql = Creator.CreateInsertUpdateSP(table);

                CreateWindow(tsql);
            }
        }

        public void TableSQLSelectProcedure(object sender, EventArgs e)
        {
            INodeInformation table = GetTable();

            if (table != null)
            {
                string tsql = Creator.CreateSelectSP(table);

                CreateWindow(tsql);
            }
        }

        public void TableSQLLogTableTrigger(object sender, EventArgs e)
        {
            INodeInformation table = GetTable();

            if (table != null)
            {
                string tsql = Creator.CreateSQLtableLog(table);

                CreateWindow(tsql);
            }
        }

        public void CreateWhoIsActiveSP(object sender, EventArgs e)
        {
            INodeInformation table = GetTable();

            if (table != null)
            {
                string tsql = Creator.CreateWhoIsActiveSP(table);

                CreateWindow(tsql);
            }
        }



        public void TableSP_SET(object sender, EventArgs e)
        {
            INodeInformation table = GetTable();

            if (table != null)
            {
                string tsql = Creator.CreateTableSP_SET(table);

                CreateWindow(tsql);
            }
        }

        public void TableSP_GET(object sender, EventArgs e)
        {
            INodeInformation table = GetTable();

            if (table != null)
            {
                string tsql = Creator.CreateTableSP_GET(table);

                CreateWindow(tsql);
            }
        }

        public void TableSP_DEL(object sender, EventArgs e)
        {
            INodeInformation table = GetTable();

            if (table != null)
            {
                string tsql = Creator.CreateTableSP_DEL(table);

                CreateWindow(tsql);
            }
        }
    }
}
