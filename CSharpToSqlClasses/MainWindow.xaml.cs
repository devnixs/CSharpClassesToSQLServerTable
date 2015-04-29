using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.CSharp;
using ObjectScriptingExtensions;

namespace CSharpToSqlClasses
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            SqlServerTextBox.Clear();
            GenerateSQL(CSharpTextBox.Text);
        }

        public void GenerateSQL(string expression)
        {

            string code = expression;
            CompilerResults compilerResults = CompileCode(code);

            if (compilerResults.Errors.HasErrors)
            {
                MessageBox.Show("Expression has a syntax error.");
                return;
            }

            Assembly assembly = compilerResults.CompiledAssembly;

            foreach (TypeInfo definedType in assembly.DefinedTypes)
            {
                object instance = Activator.CreateInstance(definedType);

               
                 string   IdName = definedType.Name + "Id";

                string schema = instance.CreateSchema(new GenerateProperties(idName:IdName));
                SqlServerTextBox.AppendText(schema);
            }

        }
        
        public CompilerResults CompileCode(string source)
        {
            CompilerParameters parms = new CompilerParameters();

            parms.GenerateExecutable = false;
            parms.GenerateInMemory = true;
            parms.IncludeDebugInformation = false;
            parms.ReferencedAssemblies.Add(@"C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.ComponentModel.DataAnnotations.dll");
            parms.ReferencedAssemblies.Add(@"System.dll");
            parms.ReferencedAssemblies.Add(@"System.Core.dll");

            CodeDomProvider compiler = CSharpCodeProvider.CreateProvider("CSharp",new Dictionary<String, String>{{ "CompilerVersion","v3.5" }});

            return compiler.CompileAssemblyFromSource(parms, source);
        } 
    }
}
