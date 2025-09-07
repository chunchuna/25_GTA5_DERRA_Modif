Public Module MathFunctions
    Public Function Clamp(x As Single, min As Single, max As Single) As Single
        If x < min Then
            Return min
        ElseIf x > max Then
            Return max
        Else
            Return x
        End If
    End Function
    Public Function Clamp(x As Integer, min As Integer, max As Integer) As Integer
        If x < min Then
            Return min
        ElseIf x > max Then
            Return max
        Else
            Return x
        End If
    End Function
End Module
