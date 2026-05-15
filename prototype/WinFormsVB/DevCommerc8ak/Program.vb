Imports System
Imports System.Windows.Forms
Imports System.Threading

Namespace DevCommerc8ak
    Public Module Program
        <STAThread>
        Public Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            Dim splash As New SplashForm()
            splash.Show()
            Application.DoEvents()
            Thread.Sleep(2000)
            splash.Close()
            Application.Run(New LoginForm())
        End Sub
    End Module
End Namespace
