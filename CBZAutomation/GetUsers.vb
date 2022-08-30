Public Class GetUsers
    Public Property id As Integer
    Public Property username As String
    Public Property officeId As Integer
    Public Property officeName As String
    Public Property firstname As String
    Public Property lastname As String
    Public Property email As String
    Public Property passwordNeverExpires As Boolean
    Public Property selectedRoles As List(Of SelectedRole)
    Public Property staff As Staff
    Public Property isSelfServiceUser As Boolean
    Public Property isEnabled As Boolean
    Public Property renewPasswordOnNextLogin As Boolean
    Public Property accountNonLocked As Boolean
    Public Property systemDefined As Boolean

End Class
