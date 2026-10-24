Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    ' Toutes les dimensions sont en centiemes de pouce, jamais en pixels/DPI.
    Public NotInheritable Class ThermiqueMiseEnPage
        Private Sub New()
        End Sub

        Public Shared Function LargeurUtile(papier As Single, debutImprimable As Single, largeurImprimable As Single, marge As Single) As Single
            Dim gauche As Single = Math.Max(0, debutImprimable) + marge
            Dim droite As Single = Math.Min(papier, debutImprimable + largeurImprimable) - marge
            If marge < 0 OrElse droite - gauche < 80 Then Throw New InvalidOperationException("La largeur imprimable est insuffisante pour un ticket 80 mm. Verifiez le pilote et le papier.")
            Return droite - gauche
        End Function

        Public Shared Function DoitReporter(hauteur As Single, disponible As Single, pageVide As Boolean) As Boolean
            If hauteur <= disponible Then Return False
            If pageVide Then Throw New InvalidOperationException("Un article ou un bloc est plus haut que la page du pilote. Choisissez une longueur de papier adaptee ; aucune ligne n'a ete tronquee.")
            Return True
        End Function
    End Class
End Namespace
