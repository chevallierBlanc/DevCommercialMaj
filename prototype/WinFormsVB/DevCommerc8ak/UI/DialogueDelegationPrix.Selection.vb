Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Partial Public NotInheritable Class DialogueDelegationPrix
        ' La petite liste est un instantane local, pas une liaison differee au
        ' CurrencyManager : les Items existent meme avant le rattachement au Form.
        ' Brancher les evenements de saisie seulement APRES cette initialisation.
        Friend Shared Function InitialiserChoixTarif(Of T As Class)(choix As ComboBox, tarifs As IEnumerable(Of T), correspond As Predicate(Of T), editable As Boolean) As T
            If choix Is Nothing Then Throw New ArgumentNullException(NameOf(choix))
            If correspond Is Nothing Then Throw New ArgumentNullException(NameOf(correspond))
            Dim instantane As New List(Of T)
            If tarifs IsNot Nothing Then instantane.AddRange(tarifs)
            choix.BeginUpdate()
            Try
                choix.Enabled = False
                choix.DataSource = Nothing
                choix.Items.Clear()
                choix.DisplayMember = "TypeVente"
                Dim index As Integer = -1
                For Each tarif As T In instantane
                    If tarif Is Nothing Then Throw New ArgumentException("Tarif absent de la liste.", NameOf(tarifs))
                    Dim ajoute As Integer = choix.Items.Add(tarif)
                    If index < 0 AndAlso correspond(tarif) Then index = ajoute
                Next
                choix.SelectedIndex = If(index >= 0 AndAlso index < choix.Items.Count, index, -1)
                choix.Enabled = editable AndAlso choix.SelectedIndex >= 0
                Return TryCast(choix.SelectedItem, T)
            Finally
                choix.EndUpdate()
            End Try
        End Function
    End Class
End Namespace
