Namespace Structs
    ''' <summary>
    ''' 用于寻找集合中最小值的算法。
    ''' </summary>
    ''' <typeparam name="T">类型。</typeparam>
    Public Class Minimize(Of T)
        Private m_hasSelectAny As Boolean
        Private m_selected As T
        Private m_minScore As IComparable
        Public ReadOnly Property HasSelectAny As Boolean
            Get
                Return m_hasSelectAny
            End Get
        End Property
        Public ReadOnly Property MinScore As IComparable
            Get
                Return m_minScore
            End Get
        End Property
        Public ReadOnly Property Selected As T
            Get
                Return m_selected
            End Get
        End Property
        ''' <summary>
        ''' 尝试选择下一个值。
        ''' </summary>
        ''' <param name="score">分数。如果分数大于<see cref="MinScore"/>则替换<see cref="MinScore"/>为<paramref name="score"/>，<see cref="Selected"/>为<paramref name="value"/></param>
        ''' <param name="value">值。</param>
        ''' <returns>是否替换了选中的值。</returns>
        Public Function TryNext(score As IComparable, value As T) As Boolean
            If Not m_hasSelectAny OrElse score.CompareTo(m_minScore) < 0 Then
                m_minScore = score
                m_selected = value
                m_hasSelectAny = True
                Return True
            Else
                Return False
            End If
        End Function
    End Class
End Namespace