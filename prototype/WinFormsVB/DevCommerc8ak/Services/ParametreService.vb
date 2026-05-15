Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class ParametreService
        Private ReadOnly _repo As ParametreRepository

        Public Sub New(repo As ParametreRepository)
            _repo = repo
        End Sub

        ' Assure la table et retourne les parametres.
        Public Function Charger() As ParametreDTO
            _repo.AssurerTable()
            Return _repo.Obtenir()
        End Function

        ' Met a jour les parametres.
        Public Sub Enregistrer(p As ParametreDTO)
            _repo.MettreAJour(p)
        End Sub
    End Class
End Namespace
