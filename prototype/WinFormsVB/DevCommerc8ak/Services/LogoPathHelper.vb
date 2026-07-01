Option Strict On
Option Explicit On

Imports System
Imports System.IO

Namespace DevCommerc8ak
    Public NotInheritable Class LogoPathHelper
        Private Sub New()
        End Sub

        Public Shared Function GetConfigDirectory() As String
            Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config")
        End Function

        Public Shared Function GetResourcesDirectory() As String
            Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "images")
        End Function

        Public Shared Function GetInstalledDefaultLogoPath() As String
            Dim configLogo As String = Path.Combine(GetConfigDirectory(), "logo.bmp")
            If File.Exists(configLogo) Then
                Return configLogo
            End If

            Dim resourceLogo As String = Path.Combine(GetResourcesDirectory(), "logo.bmp")
            If File.Exists(resourceLogo) Then
                Return resourceLogo
            End If

            Return String.Empty
        End Function

        Public Shared Function GetLogoPath(param As ParametreDTO) As String
            If param IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(param.LogoPath) AndAlso File.Exists(param.LogoPath) Then
                Return param.LogoPath
            End If

            Return GetInstalledDefaultLogoPath()
        End Function

        Public Shared Function PreparerLogoSelectionne(sourcePath As String) As String
            If String.IsNullOrWhiteSpace(sourcePath) OrElse Not File.Exists(sourcePath) Then
                Return GetInstalledDefaultLogoPath()
            End If

            Dim configDir As String = GetConfigDirectory()
            Directory.CreateDirectory(configDir)

            Dim extension As String = Path.GetExtension(sourcePath)
            If String.IsNullOrWhiteSpace(extension) Then
                extension = ".bmp"
            End If

            Dim destinationPath As String = Path.Combine(configDir, "logo" & extension.ToLowerInvariant())
            If Not String.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase) Then
                File.Copy(sourcePath, destinationPath, True)
            End If
            Return destinationPath
        End Function
    End Class
End Namespace
