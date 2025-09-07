Imports System.IO
Imports System.Reflection
Imports System.Text
Imports GTA
Imports GTA.UI

''' <summary>
''' 提醒用户更新的脚本。
''' </summary>
Public Class UpdateNotifier
    Inherits Script
    Private seconds As Integer = 0
    Public Sub New()
        Interval = 1000
    End Sub

    Private Sub UpdateNotifier_Tick(sender As Object, e As EventArgs) Handles Me.Tick

        If seconds = 10 Then
            seconds += 1
            '提示用户更新
            Dim builder As StringBuilder = New StringBuilder()
            Dim dll_file As FileInfo = New FileInfo(Assembly.GetExecutingAssembly().Location)
            builder.AppendLine("~g~DERRA 似乎已加载成功")
            builder.AppendLine("~w~如果你玩腻了建议安装最新版本的模组，作者可能已经更新了新的玩法。")
            builder.AppendLine("~r~Hot-Reload test successful!")
            Notification.PostTicker(builder.ToString, True)
        ElseIf seconds < 60 Then
            seconds += 1
        End If
    End Sub
End Class
